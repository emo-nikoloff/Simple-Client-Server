# Project workflow

English | [Български](README-bg.md)

> This project is created for educational purposes.

---

## How to Run

1. Start the **Server** application first.
2. Start as many **Client** instances as needed.
3. Each client must enter a name when prompted.
4. Once connected, clients can communicate with each other through the server.

## 1. Server

-   **State Management:** Using `readonly` for the client list (`private readonly List<StreamWriter> clients = new();`) is an excellent practice. It indicates that the variable can only be initialized once when the object is created. This ensures that nowhere else in the code can you accidentally create a new list and delete all already connected users, thus keeping the instance safe.
-   **The Accept Loop:** The `while (true) { TcpClient client = listener.AcceptTcpClient(); ... }` block is the heart of the server. The main thread (started by the `Main` method) does nothing but loop and wait at the port's "door". When someone knocks, it lets them in, delegates the work to a new thread, and immediately returns to wait for the next one.
-   **Thread Safety:** Since there is one main thread and potentially hundreds of client threads, they all share the same client resource list. To protect the program from the chaos of concurrent access, `lock (mutex)` is used. The `lock` works like a room key – when one thread enters, the others wait outside. This prevents two main problems:
    -   *Race Condition:* If two clients connect simultaneously, the threads might attempt to write data to the same array position, which could overwrite data or crash the program.
    -   *Modification during read:* In C#, it is strictly forbidden to modify a collection (e.g., removing a client) while iterating over it with `foreach`. Without locking, this would throw an `InvalidOperationException` and crash the server.
-   **The Finally Block:** The `finally { lock (mutex) { clients.Remove(writer); } }` block is the lifesaver of network applications. Regardless of whether the exit is normal or the internet drops suddenly, this code is guaranteed to execute. If the client is not removed from the list, the server would try to send a message to a broken connection and completely crash.
-   **NetworkStream Handling:** The server uses `client.GetStream()` to access the two-way pipe (or cable) connecting the applications. The `client` object represents the specific user accepted by the server. When the server writes to this stream, the message goes solely to the person whose connection was just accepted.

> **NOTE (`AutoFlush = true`):** By default, `StreamWriter` uses an internal buffer and does not send data immediately. It waits until the buffer is full before sending. Setting `AutoFlush = true` overrides this behavior, ensuring that every broadcasted message is pushed (flushed) over the network instantly. This prevents messages from getting stuck in the server's memory.

## 2. Client

-   **Full-Duplex Communication:** Communication is handled via two threads (`writerThread` and `readerThread`). Without them, the program would block at each step, and you couldn't receive a message from another person until you pressed Enter. By splitting the process, one part constantly listens and prints, while the other waits for you to type, you enable simultaneous sending and receiving.
-   **Why writerThread.Join() is needed:** Without it, the `Connect()` method would continue downwards and reach its end, triggering all `using` blocks. This would destroy the connection, the reader, and the writer immediately. Calling `Join()` tells the main thread to stop and not proceed until `writerThread` finishes its work (upon sending "quit").
-   **The empty catch block:** When you type "quit", the client exits the loop and the main program closes the resources. At this exact moment, the `readerThread` is likely still hanging blocked, waiting for network messages. Because the main program literally "pulls the plug" and closes the connection right under its feet, the reading method throws an error (e.g., `IOException`). Since we closed the connection ourselves, the error is silently ignored so as not to scare the user.
-   **NetworkStream Handling:** For the local network connection (e.g., `serverConnection`), calling `GetStream()` opens one end of the two-way pipe pointing to the server. Using a `StreamWriter` sends messages, while a `StreamReader` reads the replies. This end of the pipe and the end on the server represent the exact same TCP connection.

> **NOTE (`AutoFlush = true`):** By default, `StreamWriter` uses an internal buffer and does not send data immediately. It waits until the buffer is full before sending. Setting `AutoFlush = true` overrides this behavior, ensuring that every message is pushed (flushed) over the network instantly. This is essential for real-time chat, as it prevents messages from getting stuck in memory and eliminates the need to manually call `.Flush()` after each write.
