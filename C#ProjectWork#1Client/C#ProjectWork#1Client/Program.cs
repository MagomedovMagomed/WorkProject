using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

class Client
{
    static void Main()
    {
        string serverIP = "127.0.0.1";
        int port = 8888;

        try
        {
            using TcpClient client = new (serverIP, port);
            using NetworkStream stream = client.GetStream() ;

            string computerName = Environment.MachineName;
            string UserName = Environment.UserName;
            string authData = "admin|123";
            
            string connectMessage = $"{computerName}|{UserName}";
            byte[] data = Encoding.UTF8.GetBytes(connectMessage);
            stream.Write(data, 0, data.Length);

            Console.WriteLine("Успешно подключен к серверу");
            Console.ReadLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
        }
    }
}