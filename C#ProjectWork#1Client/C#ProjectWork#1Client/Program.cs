
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

class Client
{
    static void Main()
    {
        string serverIP = "192.168.137.1";
        int port = 8888;

        try
        {
            using TcpClient client = new (serverIP, port);
            using NetworkStream stream = client.GetStream() ;

            string connectMessage = $"{Environment.MachineName}|{Environment.UserName}";
            byte[] data = Encoding.UTF8.GetBytes(connectMessage);
            stream.Write(data, 0, data.Length);

            Console.WriteLine("Успешно подключен к серверу");
            Console.ReadLine();

            client.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
        }
    }
}