using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using System.Data.SQLite;
using static Server;

class Server
{
    private static ConcurrentDictionary<string, Clientinfo> clients = new();
    
    public class Clientinfo
    {
        public string ComputerName { get; set; }
        public string UserName { get; set; }
        public IPAddress IP {  get; set; }
        public DateTime ConnectTime { get; set; }
    }
    static void Main()
    {
        DBManager.InitializeDatabase();

        //IPAddress ipAddress = IPAddress.Any;
        IPAddress ipAddress = IPAddress.Parse("192.168.137.1");
        int port = 8888;

        TcpListener server = new TcpListener(ipAddress, port);
        server.Start();

        Console.WriteLine("Сервер запущен. Ожидание подключений...");

        try
        {
            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                Thread clientThread = new Thread(HandleClient);
                clientThread.Start(client);
            }
        }
        finally
        {
            server.Stop();
        }
    }

    private static void HandleClient(object obj)
    {
        TcpClient tcpClient = (TcpClient)obj;
        IPEndPoint clientEndPoint = (IPEndPoint)tcpClient.Client.RemoteEndPoint;
        string clientdId = Guid.NewGuid().ToString();
        Clientinfo clientInfo = null;

        try
        {
            using (NetworkStream stream = tcpClient.GetStream())
            {
                byte[] buffer = new byte[1024];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                var clientData = Encoding.UTF8.GetString(buffer, 0, bytesRead).Split("|");

                clientInfo = new Clientinfo
                {
                    ComputerName = clientData[0],
                    UserName = clientData[1],
                    IP = clientEndPoint.Address,
                    ConnectTime = DateTime.Now,
                };

                DBManager.SaveClient(clientInfo, clientdId);

                PrintClientStatus(clientInfo, "ПОДКЛЮЧЕН");

                while (true)
                {
                    try
                    {
                        // Проверяем, активно ли соединение
                        if (!IsConnected(tcpClient)) break;

                        // Если есть данные, читаем их (можно пропустить, если не нужны)
                        if (stream.DataAvailable)
                        {
                            bytesRead = stream.Read(buffer, 0, buffer.Length);
                            string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        }
                        // Пауза для снижения нагрузки на CPU
                        Thread.Sleep(100);
                    }
                    catch (IOException)
                    {
                        // Соединение было разорвано
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка с клиентом: {ex.Message}");
        }
        finally
        {
            if (clientInfo != null)
            {
                // Обновляем время отключения
                DBManager.UpdateClientDisconnectTime(clientdId);
                PrintClientStatus(clientInfo, "ОТКЛЮЧЕН");
                clients.TryRemove(clientdId, out _);
            }
            tcpClient.Close();
        }
    }

    private static void PrintClientStatus(Clientinfo clientInfo, string status)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {status}");
        Console.WriteLine($"Компьютер: {clientInfo.ComputerName}");
        Console.WriteLine($"Пользователь: {clientInfo.UserName}");
        Console.WriteLine($"IP: {clientInfo.IP}");
        Console.WriteLine($"Время: {clientInfo.ConnectTime:HH:mm:ss}");
        Console.WriteLine(new string('-', 30));
    }
    // Метод для проверки активности соединения
    private static bool IsConnected(TcpClient client)
    {
        try
        {
            if (client.Client.Poll(0, SelectMode.SelectRead))
            {
                byte[] buff = new byte[1];
                return client.Client.Receive(buff, SocketFlags.Peek) != 0;
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static class DBManager
    {
        private static string connectionString = "Data Source=clients.db;Version=3;";

        public static void InitializeDatabase()
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                var command = new SQLiteCommand(
                    @"CREATE TABLE IF NOT EXISTS Clients (
                    Id TEXT PRIMARY KEY,
                    ComputerName TEXT NOT NULL,
                    UserName TEXT NOT NULL,
                    IP TEXT NOT NULL,
                    ConnectTime DATETIME NOT NULL,
                    DisconnectTime DATETIME
                )", connection);
                command.ExecuteNonQuery();
            }
        }

        public static void SaveClient(Clientinfo clientInfo, string clientdId)
        {
            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    var command = new SQLiteCommand(
                        "INSERT INTO Clients (Id, ComputerName, UserName, IP, ConnectTime) " +
                        "VALUES (@id, @computer, @user, @ip, @time)", connection);

                    command.Parameters.AddWithValue("@id", clientdId);
                    command.Parameters.AddWithValue("@computer", clientInfo.ComputerName);
                    command.Parameters.AddWithValue("@user", clientInfo.UserName);
                    command.Parameters.AddWithValue("@ip", clientInfo.IP.ToString());
                    command.Parameters.AddWithValue("@time", DateTime.Now);

                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения данных: {ex.Message}");
            }
        }

        public static void UpdateClientDisconnectTime(string clientdId)
        {
            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    var command = new SQLiteCommand(
                    "UPDATE Clients SET DisconnectTime = @disconnectTime " +
                    "WHERE Id = @id", connection);

                    command.Parameters.AddWithValue("@disconnectTime", DateTime.Now);
                    command.Parameters.AddWithValue("@id", clientdId);
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления данных: {ex.Message}");
            }
        }
    }
}