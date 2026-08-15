using System.Net;
using System.Net.Sockets;

using static Configurations.NetworkConfigurations;

namespace SimpleServer;

public class Server
{
    private readonly List<ConnectedClient> clients = new();

    static void Main(string[] args)
    {
        new Server().Start();
    }

    private void Start()
    {
        try
        {
            using TcpListener tcpListener = new(IPAddress.Any, ServerPort);
            tcpListener.Start();
            Console.WriteLine($"Server started on port: {ServerPort}");

            object mutex = new();

            while (true)
            {
                TcpClient tcpClient = tcpListener.AcceptTcpClient();

                string clientEndPoint = tcpClient.Client.RemoteEndPoint?.ToString() ?? "Unknown IP";
                Console.WriteLine($"[{clientEndPoint}] Client connecting...");

                Thread clientThread = new(() =>
                {
                    using (tcpClient)
                    {
                        using StreamReader reader = new(tcpClient.GetStream());
                        using StreamWriter writer = new(tcpClient.GetStream())
                        {
                            AutoFlush = true,
                        };

                        ConnectedClient? currentClient = null;

                        try
                        {
                            string? username = reader.ReadLine();
                            if (string.IsNullOrWhiteSpace(username))
                            {
                                username = "Guest";
                            }

                            currentClient = new ConnectedClient(tcpClient, writer, username);

                            lock (mutex)
                            {
                                clients.Add(currentClient);

                                Console.WriteLine($"{currentClient} connected. Total clients: {clients.Count}");

                                foreach (ConnectedClient client in clients)
                                {
                                    if (client != currentClient)
                                    {
                                        client.Writer.WriteLine($"--- {currentClient.Username} joined ---");
                                    }
                                }
                            }

                            string? input;
                            while ((input = reader.ReadLine()) != null)
                            {
                                if (input.Equals("quit", StringComparison.CurrentCultureIgnoreCase))
                                {
                                    break;
                                }

                                Console.WriteLine($"{currentClient} says: {input}");

                                lock (mutex)
                                {
                                    foreach (ConnectedClient client in clients)
                                    {
                                        if (client != currentClient)
                                        {
                                            client.Writer.WriteLine($"{currentClient.Username}: {input}");
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[{clientEndPoint}] connection error: {ex.Message}");
                        }
                        finally
                        {
                            if (currentClient != null)
                            {
                                lock (mutex)
                                {
                                    clients.Remove(currentClient);
                                    Console.WriteLine($"{currentClient} left. Total: {clients.Count}");

                                    foreach (ConnectedClient client in clients)
                                    {
                                        client.Writer.WriteLine($"--- {currentClient.Username} left ---");
                                    }
                                }
                            }
                        }
                    }
                });

                clientThread.Start();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Server starting error: {ex.Message}");
        }
    }
}
