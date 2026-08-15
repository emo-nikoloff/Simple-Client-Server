using System.Net;
using System.Net.Sockets;

using static Configurations.NetworkConfigurations;

namespace SimpleServer;

public class Server
{
    private readonly List<StreamWriter> clients = new();

    static void Main(string[] agrs)
    {
        new Server().Start();
    }

    private void Start()
    {
        try
        {
            using TcpListener listener = new(IPAddress.Any, ServerPort);
            listener.Start();
            Console.WriteLine($"Server started on port: {ServerPort}");

            object mutex = new();

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                Console.WriteLine("Client connected.");

                Thread clientThread = new(() =>
                {
                    using (client)
                    {
                        using StreamReader reader = new(client.GetStream());
                        using StreamWriter writer = new(client.GetStream())
                        {
                            AutoFlush = true,
                        };

                        try
                        {
                            lock (mutex)
                            {
                                clients.Add(writer);
                            }

                            string? input;
                            while ((input = reader.ReadLine()) != null)
                            {
                                if (input.Equals("quit", StringComparison.CurrentCultureIgnoreCase))
                                {
                                    break;
                                }

                                lock (mutex)
                                {
                                    foreach (StreamWriter client in clients)
                                    {
                                        client.WriteLine(input);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Client connection error: {ex.Message}");
                        }
                        finally
                        {
                            lock (mutex)
                            {
                                clients.Remove(writer);
                                Console.WriteLine("Client disconnected.");
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
