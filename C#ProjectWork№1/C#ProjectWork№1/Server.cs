using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;

class Server
{
    private static ConcurrentDictionary<string, Clientinfo> clients = new();
    
    class Clientinfo
    {
        public string ComputerName { get; set; }
        public string UserName { get; set; }
        public IPAddress IP {  get; set; }
        public DateTime ConnectTime { get; set; }
    }
    static void Main()
    {
        IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
        int port = 8888;

        TcpListener server = new TcpListener(ipAddress, port);
        server.Start();

        Console.WriteLine("Сервер запущен");

        try
        {
            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClient));
                clientThread.Start(client);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
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
        try
        {
            using (NetworkStream stream = tcpClient.GetStream())
            {
                byte[] buffer = new byte[1024];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                var clientData = Encoding.UTF8.GetString(buffer, 0, bytesRead).Split("|");

                var clientInfo = new Clientinfo
                {
                    ComputerName = clientData[0],
                    UserName = clientData[1],
                    IP = clientEndPoint.Address,
                    ConnectTime = DateTime.Now,
                };

                clients.TryAdd(clientdId, clientInfo);
                PrintClientStatus(clientInfo, "Подключен");

                while (true)
                {
                    if (tcpClient.Connected && stream.DataAvailable)
                    {
                        bytesRead = stream.Read(buffer, 0, buffer.Length);
                    }
                    else if (!tcpClient.Connected)
                    {
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка с клиентом {clientEndPoint}: {ex.Message}");
        }
        finally
        {
            if (clients.TryRemove(clientdId, out Clientinfo removedClient))
            {
                PrintClientStatus(removedClient, "Отключен");
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
}