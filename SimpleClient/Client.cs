using System.Net.Sockets;

using static Configurations.NetworkConfigurations;

namespace SimpleClient;

public class Client
{
    static void Main(string[] args)
    {
        /*
            EN: Creates an anonymous instance of the Client class and immediately starts its main logic.
            BG: Създава анонимен обект от тип Client и веднага стартира основната му логика.
        */
        new Client().Connect();
    }

    private void Connect()
    {
        try
        {
            Console.Write("Enter your name: ");
            string myName = Console.ReadLine()!;

            /*
                EN: Establishes a TCP connection with the server. 'using' ensures proper socket disposal at the end.
                BG: Установява TCP връзка със сървъра. 'using' гарантира правилното затваряне на връзката със сървъра накрая.
            */
            using TcpClient serverConnection = new(ServerHost, ServerPort);

            /*
                EN: Creates read and write streams. AutoFlush = true - data is sent immediately without waiting for the buffer to fill up.
                BG: Създава потоци за четене и писане. AutoFlush = true - данните се изпращат веднага, без да се чака буферът да се напълни.
            */
            using StreamReader reader = new(serverConnection.GetStream());
            using StreamWriter writer = new(serverConnection.GetStream())
            {
                AutoFlush = true,
            };

            /*
                EN: Creates a thread solely responsible for reading from the keyboard and sending to the server.
                BG: Създава нишка, която отговаря единствено за четене от клавиатурата и пращане към сървъра.
            */
            Thread writerThread = new(() =>
            {
                try
                {
                    string input;
                    while ((input = Console.ReadLine()!) != null)
                    {
                        /*
                            EN: If the user types "quit", we send the exact command and break the loop.
                            BG: Ако потребителят въведе "quit", изпращаме точната команда и прекъсваме цикъла.
                        */
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

            /*
                EN: Creates a thread that listens for incoming messages from the server and prints them to the screen.
                BG: Създава нишка, която слуша за входящи съобщения от сървъра и ги отпечатва на екрана.
            */
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
                    /*
                        EN: Intentionally empty catch. We ignore the exception during the expected socket closure.
                        BG: Умишлено празен catch. Игнорираме грешката при очакваното затваряне на връзката със сървъра.
                    */
                }
            });

            /*
                EN: Start both threads simultaneously.
                BG: Стартираме и двете нишки едновременно.
            */
            writerThread.Start();
            readerThread.Start();

            /*
                EN: Blocks the main program and makes it wait until the writer thread finishes (on "quit").
                BG: Блокира главната програма и я кара да чака, докато нишката за писане не приключи (при "quit").
            */
            writerThread.Join();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connection error: {ex.Message}");
        }
    }
}
