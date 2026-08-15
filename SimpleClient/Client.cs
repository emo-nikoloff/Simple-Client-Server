using System.Net.Sockets;

using static Configurations.NetworkConfigurations;

namespace SimpleClient;

public class Client
{
    static void Main(string[] args)
    {
        new Client().Connect();
    }

    private void Connect()
    {
        try
        {
            Console.Write("Enter your name: ");
            string myName = Console.ReadLine()!;

            using TcpClient serverConnection = new(ServerHost, ServerPort);

            using StreamReader reader = new(serverConnection.GetStream());
            using StreamWriter writer = new(serverConnection.GetStream())
            {
                AutoFlush = true,
            };

            Thread writerThread = new(() =>
            {
                try
                {
                    string input;
                    while ((input = Console.ReadLine()!) != null)
                    {
                        if (input.Equals("quit", StringComparison.CurrentCultureIgnoreCase))
                        {
                            writer.WriteLine("quit");
                            break;
                        }

                        writer.WriteLine($"{myName}: {input}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Message sending error: {ex.Message}");
                }
            });

            Thread readerThread = new(() =>
            {
                try
                {
                    string? response;
                    while ((response = reader.ReadLine()) != null)
                    {
                        Console.WriteLine(response);
                    }
                }
                catch (Exception)
                {
                }
            });

            writerThread.Start();
            readerThread.Start();

            writerThread.Join();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connection error: {ex.Message}");
        }
    }
}
