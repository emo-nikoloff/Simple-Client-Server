using System.Net;
using System.Net.Sockets;

using static Configurations.NetworkConfigurations;

namespace SimpleServer;

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
