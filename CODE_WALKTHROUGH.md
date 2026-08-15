# Code Walkthrough | Преглед на кода

[English README](README.md) | [Български README](README-bg.md)

> **EN:** This document provides an explanation of the core logic behind the Server and Client applications. It is designed to help you understand how TCP connections, multithreading, and stream handling work in C#.
>
> **BG:** Този документ предоставя обяснение на основната логика зад Server и Client приложенията. Създаден е да ви помогне да разберете как работят TCP връзките, работата с множество нишки и работата с потоци в C#.

---

## Server:
```csharp
public class Server
{
    /*
        EN: Stores the connections (write streams) for all active clients. 'readonly' ensures that the list instance itself cannot be accidentally overwritten.
        BG: Съхранява връзките (потоците за писане) към всички активни клиенти. 'readonly' гарантира, че самият списък не може да бъде презаписан по погрешка.
    */
    private readonly List<StreamWriter> clients = new();

    static void Main(string[] agrs)
    {
        /*
            EN: Creates an anonymous instance of the Server class and immediately starts its main logic.
            BG: Създава анонимен обект от тип Server и веднага стартира основната му логика.
        */
        new Server().Start();
    }

    private void Start()
    {
        try
        {
            /*
                EN: Initializes the server to listen on all network interfaces on the specified port. 'using' ensures that the listener is properly disposed of in case of a critical error.
                BG: Инициализира сървъра да слуша на всички мрежови интерфейси на дадения порт. 'using' гарантира, че слушателят ще бъде затворен при критична грешка.
            */
            using TcpListener listener = new(IPAddress.Any, ServerPort);
            listener.Start();
            Console.WriteLine($"Server started on port: {ServerPort}");

            /*
                EN: An object that serves as a synchronization "key" (lock) for accessing the client list.
                BG: Обект, който служи като "ключ" за синхронизация на достъпа до списъка с клиенти.
            */
            object mutex = new();

            /*
                EN: An infinite loop that keeps the server awake and ready to accept new connections.
                BG: Безкраен цикъл, който държи сървъра буден и готов да приема нови връзки.
            */
            while (true)
            {
                /*
                    EN: The program stops here and waits. When a client connects, it creates a TcpClient for them.
                    BG: Програмата спира тук и чака. Когато някой се свърже, създава TcpClient за него.
                */
                TcpClient client = listener.AcceptTcpClient();
                Console.WriteLine("Client connected.");

                /*
                    EN: Each new connection is handled in a separate thread so it doesn't block the main loop.
                    BG: Всяка нова връзка се изнася в отделна нишка, за да не блокира основния цикъл.
                */
                Thread clientThread = new(() =>
                {
                    /*
                        EN: Ensures the client connection and streams are closed when the thread finishes.
                        BG: Гарантираме затварянето на клиентската връзка и потоците, когато нишката приключи.
                    */
                    using (client)
                    {
                        /*
                            EN: Creates read and write streams. AutoFlush = - data is sent immediately without waiting for the buffer to fill up.
                            BG: Създава потоци за четене и писане. AutoFlush = true - данните се изпращат веднага, без да се чака буферът да се напълни.
                        */
                        using StreamReader reader = new(client.GetStream());
                        using StreamWriter writer = new(client.GetStream())
                        {
                            AutoFlush = true,
                        };

                        try
                        {
                            /*
                                EN: Locks the list to safely add the new client.
                                BG: Заключваме списъка, за да добавим новия клиент безопасно.
                            */
                            lock (mutex)
                            {
                                clients.Add(writer);
                            }

                            /*
                                EN: Reads messages from the network. If it returns null, the client has disconnected.
                                BG: Четем съобщения от мрежата. Ако върне null, значи клиентът е прекъснал връзката.
                            */
                            string? input;
                            while ((input = reader.ReadLine()) != null)
                            {
                                /*
                                    EN: If we receive the "quit" command from the client, we break the loop, which will trigger the connection closure.
                                    BG: Ако получим командата "quit" от клиента, прекъсваме цикъла, което ще задейства затварянето на връзката с него.
                                */
                                if (input.Equals("quit", StringComparison.CurrentCultureIgnoreCase))
                                {
                                    break;
                                }

                                /*
                                    EN: Broadcasts the received message to all other connected clients.
                                    BG: Разпращаме полученото съобщение до всички останали клиенти.
                                */
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

                /*
                    EN: Starts the thread to begin serving this specific client.
                    BG: Стартираме нишката, за да започне обслужването на този конкретен клиент.
                */
                clientThread.Start();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Server starting error: {ex.Message}");
        }
    }
}
```

## Client:
```csharp
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
```