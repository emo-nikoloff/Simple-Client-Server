# Project workflow

[Български](README-bg.md) | [📖 Code Walkthrough](CODE_WALKTHROUGH.md)

> This project is created for educational purposes.

---

## How to Run

1. In **`Configurations\NetworkConfigurations.cs`**, change ServerHost so that it contains the local IP address of the machine running the server.
2. Start the **Server** application.
3. Start as many **Client** instances as needed.
4. Each client must enter a name when prompted.
5. Once connected, clients can communicate with each other through the server.

## 1. Server

- **The Accept Loop:** The `while (true) { TcpClient tcpClient = tcpListener.AcceptTcpClient(); ... }` block is the heart of the server. The main thread (started by the `Main` method) continuously waits for incoming connections on the specified port. When a client connects, it accepts the connection, starts a new thread to handle that client, and returns to waiting for the next connection.
- **Thread Safety:** Since there is one main thread and potentially many client threads, they all access the shared client list. To protect the program from concurrent access, `lock (mutex)` is used. The `lock` ensures that only one thread can access the shared client list at a time. This prevents two main problems:
    - *Race Condition:* If multiple client threads try to access the shared client list at the same time, the operations can interfere with each other and lead to unexpected behavior.
    - *Modification during read:* A collection should not be modified while it is being iterated with `foreach`. Without locking, this could throw an `InvalidOperationException`.
- **Client Identification:** When a client connects, the server reads the username sent by the client. If no valid name is provided, `"Guest"` is used. The server then creates a `ConnectedClient` object containing the client's TCP connection, writer, and username.
- **Client Notifications:** When a client joins or leaves, the server notifies the other connected clients. The client that joined or left does not receive its own notification.
- **Message Broadcasting:** When a client sends a message, the server identifies the sender through `currentClient` and sends the message to every other connected client. The sender is excluded, so they do not receive their own message back.
- **The Finally Block:** The `finally` block removes the disconnected client from the shared list and notifies the remaining clients. This cleanup is performed even when the connection ends because of an exception.
- **NetworkStream Handling:** The server uses `GetStream()` to access the two-way communication stream of each TCP connection. A `StreamReader` is used to receive data from the client, while a `StreamWriter` is used to send data to that client.

> **NOTE (`AutoFlush = true`):** By default, `StreamWriter` uses an internal buffer and does not send data immediately. Setting `AutoFlush = true` ensures that written data is sent immediately instead of waiting for the buffer to fill up.

## 2. Client

- **Full-Duplex Communication:** Communication is handled via two threads (`writerThread` and `readerThread`). The writer thread waits for keyboard input and sends messages to the server, while the reader thread continuously listens for incoming messages. This allows sending and receiving to happen independently.
- **Sending the Username:** After establishing the TCP connection, the client sends the entered username to the server. The server uses this name to identify the client and display the sender's name in messages.
- **Ignoring Empty Messages:** The writer thread ignores empty or whitespace-only input, so these messages are not sent to the server.
- **Why `writerThread.Join()` is needed:** `Join()` makes the main thread wait until `writerThread` finishes. Without it, `Connect()` would reach its end immediately after starting the worker threads, causing the `using` blocks to dispose of the connection and streams while the threads are still running.
- **The empty catch block:** When the client exits, the connection and streams are disposed. At that moment, the `readerThread` may still be blocked waiting for data from the server. Closing the connection can cause an exception in that thread, which is intentionally ignored because the connection is being closed normally.
- **NetworkStream Handling:** For the connection to the server, `GetStream()` provides the two-way communication stream. `StreamWriter` sends messages to the server, while `StreamReader` receives messages from it.

> **NOTE (`AutoFlush = true`):** By default, `StreamWriter` uses an internal buffer and does not send data immediately. Setting `AutoFlush = true` ensures that every message is sent immediately instead of remaining in the buffer.
