# Code Walkthrough | Преглед на кода

> **EN:** This document explains the core logic behind the Server and Client applications. It is designed to help you understand how TCP connections, working with multiple threads, and stream handling work in C#.
>
> **BG:** Този документ обяснява основната логика зад Server и Client приложенията. Създаден е да ви помогне да разберете как работят TCP връзките, работата с множество нишки и работата с потоци в C#.

[English README](README.md) | [Български README](README-bg.md)

---

## Server:

```csharp
public class Server
{
    /*
        EN: Stores information about all currently connected clients. ConnectedClient 
        keeps the client's connection, writer, and username together.
        BG: Съхранява информация за всички текущо свързани клиенти. ConnectedClient 
        обединява връзката на клиента, writer-а и потребителското му име.
    */
    private readonly List<ConnectedClient> clients = new();
    
    static void Main(string[] args)
    {
        /*
            EN: Creates an anonymous instance of the Server class and 
            immediately starts its main logic.
            BG: Създава анонимен обект от тип Server и веднага стартира 
            основната му логика.
        */
        new Server().Start();
    }

    private void Start()
    {
        try
        {
            /*
                EN: Initializes the server to listen on all network interfaces 
                on the specified port.
                BG: Инициализира сървъра да слуша на всички мрежови интерфейси 
                на зададения порт.
            */
            using TcpListener tcpListener = new(IPAddress.Any, ServerPort);
            tcpListener.Start();
            Console.WriteLine($"Server started on port: {ServerPort}");

            /*
                EN: An object used as a synchronization key for safely accessing 
                the shared client list.
                BG: Обект, който се използва като ключ за синхронизация при 
                безопасен достъп до споделения списък с клиенти.
            */
            object mutex = new();

            /*
                EN: Keeps the server running and ready to accept new client connections.
                BG: Поддържа сървъра работещ и готов да приема нови клиентски връзки.
            */
            while (true)
            {
                /*
                    EN: The program waits here until a client connects. Once a 
                    connection is established, TcpClient represents that client.
                    BG: Програмата чака тук, докато клиент се свърже. След установяване 
                    на връзката TcpClient представлява конкретния клиент.
                */
                TcpClient tcpClient = tcpListener.AcceptTcpClient();

                /*
                    EN: Gets the client's remote endpoint so the server can 
                    identify its IP address and port.
                    BG: Получава отдалечената крайна точка на клиента, за да може 
                    сървърът да идентифицира неговия IP адрес и порт.
                */
                string clientEndPoint = tcpClient.Client.RemoteEndPoint?.ToString() ?? "Unknown IP";
                Console.WriteLine($"[{clientEndPoint}] Client connecting...");

                /*
                    EN: Each client connection is handled in a separate thread 
                    so multiple clients can be served concurrently.
                    BG: Всяка клиентска връзка се обработва в отделна нишка, за да 
                    могат няколко клиента да бъдат обслужвани едновременно.
                */
                Thread clientThread = new(() =>
                {
                    /*
                        EN: Ensures that the TCP connection is disposed when this 
                        client's thread finishes.
                        BG: Гарантира, че TCP връзката ще бъде освободена, когато 
                        нишката на този клиент приключи.
                    */
                    using (tcpClient)
                    {
                        /*
                            EN: Creates the streams used to receive data from and send 
                            data to the client. AutoFlush = true sends written data 
                            immediately without waiting for the buffer to fill.
                            BG: Създава потоците, използвани за получаване и изпращане 
                            на данни към клиента. AutoFlush = true изпраща записаните 
                            данни веднага, без да изчаква буферът да се запълни.
                        */
                        using StreamReader reader = new(tcpClient.GetStream());
                        using StreamWriter writer = new(tcpClient.GetStream())
                        {
                            AutoFlush = true,
                        };

                        /*
                            EN: Stores the ConnectedClient instance for the current 
                            connection. It starts as null because the username has 
                            not been read yet.
                            BG: Съхранява ConnectedClient обекта за текущата връзка. В 
                            началото е null, защото потребителското име все още не е прочетено.
                        */
                        ConnectedClient? currentClient = null;

                        try
                        {
                            /*
                                EN: Reads the username sent by the client when the connection 
                                is established. If the value is missing or contains only 
                                whitespace, the client is assigned the default name "Guest".
                                BG: Прочита потребителското име, изпратено от клиента при 
                                установяване на връзката. Ако стойността липсва или съдържа 
                                само празни символи, на клиента се задава името "Guest".
                            */
                            string? username = reader.ReadLine();
                            if (string.IsNullOrWhiteSpace(username))
                            {
                                username = "Guest";
                            }

                            /*
                                EN: Creates a ConnectedClient object containing the client's 
                                connection, writer, and username.
                                BG: Създава ConnectedClient обект, който съдържа връзката 
                                на клиента, writer-а и потребителското му име.
                            */
                            currentClient = new ConnectedClient(tcpClient, writer, username);

                            /*
                                EN: Adds the client to the shared list while preventing other 
                                threads from modifying or reading the list at the same time.
                                BG: Добавя клиента към споделения списък, като не позволява 
                                на други нишки да променят или четат списъка едновременно.
                            */
                            lock (mutex)
                            {
                                clients.Add(currentClient);

                                Console.WriteLine($"{currentClient} connected. Total clients: {clients.Count}");

                                /*
                                    EN: Notifies every other connected client that the new 
                                    client has joined. The newly connected client does not 
                                    receive its own join notification.
                                    BG: Уведомява всички останали свързани клиенти, че новият 
                                    клиент се е присъединил. Новоприсъединеният клиент не 
                                    получава собственото си съобщение за присъединяване.
                                */
                                foreach (ConnectedClient client in clients)
                                {
                                    if (client != currentClient)
                                    {
                                        client.Writer.WriteLine($"--- {currentClient.Username} joined ---");
                                    }
                                }
                            }

                            /*
                                EN: Continuously reads messages sent by the current client. 
                                The loop ends when the client disconnects or sends the "quit" command.
                                BG: Непрекъснато прочита съобщенията, изпратени от текущия клиент. 
                                Цикълът приключва, когато клиентът прекъсне връзката или 
                                изпрати командата "quit".
                            */
                            string? input;
                            while ((input = reader.ReadLine()) != null)
                            {
                                /*
                                    EN: Stops processing messages when the client requests 
                                    to leave the session.
                                    BG: Спира обработката на съобщенията, когато клиентът 
                                    поиска да напусне сесията.
                                */
                                if (input.Equals("quit", StringComparison.CurrentCultureIgnoreCase))
                                {
                                    break;
                                }

                                /*
                                    EN: Logs which connected client sent the message.
                                    BG: Записва кой свързан клиент е изпратил съобщението.
                                */
                                Console.WriteLine($"{currentClient} says: {input}");

                                /*
                                    EN: Sends the message to every other connected client. The 
                                    sender is excluded so they do not receive their own message back.
                                    BG: Изпраща съобщението до всички останали свързани клиенти. 
                                    Подателят е пропуснат, за да не получава собственото си 
                                    съобщение обратно.
                                */
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
                            /*
                                EN: currentClient can be null if the connection failed 
                                before the ConnectedClient object was created.
                                BG: currentClient може да бъде null, ако връзката е прекъснала 
                                преди създаването на ConnectedClient обекта.
                            */
                            if (currentClient != null)
                            {
                                /*
                                    EN: Removes the disconnected client from the shared 
                                    list and notifies the remaining clients.
                                    BG: Премахва прекъсналия клиент от споделения списък 
                                    и уведомява останалите клиенти.
                                */
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

                /*
                    EN: Starts the thread responsible for serving this specific client.
                    BG: Стартира нишката, отговорна за обслужването на конкретния клиент.
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

---

## Client:

```csharp
public class Client
{
    static void Main(string[] args)
    {
        /*
            EN: Creates an anonymous instance of the Client class and 
            immediately starts its main logic.
            BG: Създава анонимен обект от тип Client и веднага стартира 
            основната му логика.
        */
        new Client().Connect();
    }

    private void Connect()
    {
        try
        {
            /*
                EN: Asks the user for a name that will be sent to the server 
                after the connection is established.
                BG: Изисква от потребителя име, което ще бъде изпратено към 
                сървъра след установяване на връзката.
            */
            Console.Write("Enter your name: ");
            string? myName = Console.ReadLine();

            /*
                EN: Establishes a TCP connection with the server.
                BG: Установява TCP връзка със сървъра.
            */
            using TcpClient serverConnection = new(ServerHost, ServerPort);

            /*
                EN: Creates the streams used to receive data from and send data 
                to the server. AutoFlush = true sends written data immediately 
                without waiting for the buffer to fill.
                BG: Създава потоците, използвани за получаване и изпращане на данни 
                към сървъра. AutoFlush = true изпраща записаните данни веднага, 
                без да изчаква буферът да се запълни.
            */
            using StreamReader reader = new(serverConnection.GetStream());
            using StreamWriter writer = new(serverConnection.GetStream())
            {
                AutoFlush = true,
            };

            /*
                EN: Sends the client's username to the server so it can 
                identify the connection.
                BG: Изпраща потребителското име към сървъра, за да може 
                той да идентифицира връзката.
            */
            writer.WriteLine(myName);

            /*
                EN: Creates a thread responsible for reading keyboard input 
                and sending messages to the server.
                BG: Създава нишка, отговорна за четенето от клавиатурата 
                и изпращането на съобщения към сървъра.
            */
            Thread writerThread = new(() =>
            {
                try
                {
                    string input;
                    while ((input = Console.ReadLine()!) != null)
                    {
                        /*
                            EN: Ignores empty or whitespace-only messages.
                            BG: Игнорира празни съобщения и съобщения, съдържащи 
                            само празни символи.
                        */
                        if (string.IsNullOrWhiteSpace(input))
                        {
                            continue;
                        }

                        /*
                            EN: Sends the "quit" command to the server and 
                            stops the input loop.
                            BG: Изпраща командата "quit" към сървъра и 
                            прекратява цикъла за въвеждане.
                        */
                        if (input.Equals("quit", StringComparison.CurrentCultureIgnoreCase))
                        {
                            writer.WriteLine("quit");
                            break;
                        }

                        /*
                            EN: Sends the user's message to the server. The server 
                            is responsible for adding the sender's username before 
                            broadcasting it.
                            BG: Изпраща съобщението на потребителя към сървъра. 
                            Сървърът е отговорен за добавянето на името на подателя 
                            преди разпространяването на съобщението.
                        */
                        writer.WriteLine(input);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Message sending error: {ex.Message}");
                }
            });

            /*
                EN: Creates a thread that listens for incoming messages from 
                the server and prints them to the console.
                BG: Създава нишка, която слуша за входящи съобщения от сървъра 
                и ги отпечатва в конзолата.
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
                        EN: The exception is intentionally ignored because the 
                        connection may be closing normally.
                        BG: Изключението умишлено се игнорира, защото връзката 
                        може да се затваря нормално.
                    */
                }
            });

            /*
                EN: Starts both threads so the client can send and receive 
                messages independently.
                BG: Стартира и двете нишки, за да може клиентът независимо 
                да изпраща и получава съобщения.
            */
            writerThread.Start();
            readerThread.Start();

            /*
                EN: Blocks the main thread until the writer thread finishes, 
                which happens after the user sends "quit".
                BG: Блокира главната нишка, докато нишката за писане приключи, 
                което се случва след изпращане на "quit".
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
