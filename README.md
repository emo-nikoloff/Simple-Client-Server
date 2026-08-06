# Project workflow / Работен процес на проекта

> This document contains an explanation of the architecture and the workflow of the project. It is divided into two sections – English and Bulgarian.
>
> Този документ съдържа обяснение на архитектурата и работния процес на проекта. Разделен е на две части – английска и българска.

---

# EN: Architecture and Workflow

### 1. Server

*   **State Management:** Using `readonly` for the client list (`private readonly List<StreamWriter> clients = new();`) is an excellent practice. It indicates that the variable can only be initialized once when the object is created. This ensures that nowhere else in the code can you accidentally create a new list and delete all already connected users, thus keeping the instance safe.
*   **The Accept Loop:** The `while (true) { TcpClient client = listener.AcceptTcpClient(); ... }` block is the heart of the server. The main thread (started by the `Main` method) does nothing but loop and wait at the port's "door". When someone knocks, it lets them in, delegates the work to a new thread, and immediately returns to wait for the next one.
*   **Thread Safety:** Since there is one main thread and potentially hundreds of client threads, they all share the same client resource list. To protect the program from the chaos of concurrent access, `lock (mutex)` is used. The `lock` works like a room key – when one thread enters, the others wait outside. This prevents two main problems:
    *   *Race Condition:* If two clients connect simultaneously, the threads might attempt to write data to the same array position, which could overwrite data or crash the program.
    *   *Modification during read:* In C#, it is strictly forbidden to modify a collection (e.g., removing a client) while iterating over it with `foreach`. Without locking, this would throw an `InvalidOperationException` and crash the server.
*   **The Finally Block:** The `finally { lock (mutex) { clients.Remove(writer); } }` block is the lifesaver of network applications. Regardless of whether the exit is normal or the internet drops suddenly, this code is guaranteed to execute. If the client is not removed from the list, the server would try to send a message to a broken connection and completely crash.
*   **NetworkStream Handling:** The server uses `client.GetStream()` to access the two-way pipe (or cable) connecting the applications. The `client` object represents the specific user accepted by the server. When the server writes to this stream, the message goes solely to the person whose connection was just accepted.

> **NOTE (`AutoFlush = true`):** By default, `StreamWriter` uses an internal buffer and does not send data immediately. It waits until the buffer is full before sending. Setting `AutoFlush = true` overrides this behavior, ensuring that every broadcasted message is pushed (flushed) over the network instantly. This prevents messages from getting stuck in the server's memory.

### 2. Client

*   **Full-Duplex Communication:** Communication is handled via two threads (`writerThread` and `readerThread`). Without them, the program would block at each step, and you couldn't receive a message from another person until you pressed Enter. By splitting the process, one part constantly listens and prints, while the other waits for you to type, you enable simultaneous sending and receiving.
*   **Why writerThread.Join() is needed:** Without it, the `Connect()` method would continue downwards and reach its end, triggering all `using` blocks. This would destroy the connection, the reader, and the writer immediately. Calling `Join()` tells the main thread to stop and not proceed until `writerThread` finishes its work (upon sending "quit").
*   **The empty catch block:** When you type "quit", the client exits the loop and the main program closes the resources. At this exact moment, the `readerThread` is likely still hanging blocked, waiting for network messages. Because the main program literally "pulls the plug" and closes the connection right under its feet, the reading method throws an error (e.g., `IOException`). Since we closed the connection ourselves, the error is silently ignored so as not to scare the user.
*   **NetworkStream Handling:** For the local network connection (e.g., `serverConnection`), calling `GetStream()` opens one end of the two-way pipe pointing to the server. Using a `StreamWriter` sends messages, while a `StreamReader` reads the replies. This end of the pipe and the end on the server represent the exact same TCP connection.

> **NOTE (`AutoFlush = true`):** By default, `StreamWriter` uses an internal buffer and does not send data immediately. It waits until the buffer is full before sending. Setting `AutoFlush = true` overrides this behavior, ensuring that every message is pushed (flushed) over the network instantly. This is essential for real-time chat, as it prevents messages from getting stuck in memory and eliminates the need to manually call `.Flush()` after each write.

---

# BG: Архитектура и Работен процес

### 1. Сървър (Server)

*   **Управление на състоянието (State Management):** Използването на `readonly` за списъка с клиенти (`private readonly List<StreamWriter> clients = new();`) е отлична практика. То указва, че променливата може да бъде инициализирана само веднъж при създаването на обекта. Това означава, че никъде другаде в кода не можете случайно да създадете нов списък и да изтриете всички вече свързани потребители, като по този начин пазите инстанцията в безопасност.
*   **Главният цикъл (The Accept Loop):** Блокът `while (true) { TcpClient client = listener.AcceptTcpClient(); ... }` е сърцето на сървъра. Главната нишка (стартирана от `Main` метода) не прави нищо друго, освен да се върти в този цикъл и да чака на "вратата" на порта. Когато някой почука, тя го пуска вътре, делегира работата с него на нова нишка и веднага се връща да чака следващия.
*   **Безопасност при нишките (Thread Safety):** Тъй като имате една главна нишка и потенциално стотици клиентски нишки, всички те споделят един и същ ресурсен списък с клиенти. За да се предпази програмата от хаоса при едновременен достъп, се използва `lock (mutex)`. Блокът `lock` работи като ключ за стая – когато една нишка влезе, другите чакат отвън. Това предотвратява два основни проблема:
    *   *Счупване на списъка (Race Condition):* Ако двама клиенти се свържат едновременно, нишките могат да опитат да запишат данни на една и съща позиция в масива, което би презаписало данните или крашнало програмата.
    *   *Промяна по време на четене:* В C# е строго забранено колекция да се променя (напр. изтриване на клиент), докато се обхожда с `foreach`. Без заключване, това би хвърлило `InvalidOperationException` и сървърът би се счупил.
*   **Гарантирано почистване (The Finally Block):** Блокът `finally { lock (mutex) { clients.Remove(writer); } }` е спасителят на мрежовите приложения. Независимо дали излизането е нормално или интернетът е спрял внезапно, този код гарантирано се изпълнява. Ако клиентът не бъде премахнат от списъка, сървърът би се опитал да изпрати съобщение към прекъсната връзка и би се сринал тотално.
*   **Работа с мрежовия поток (NetworkStream):** Сървърът използва `client.GetStream()`, за да достъпи двупосочната тръба (или кабел), свързваща двете приложения. Обектът `client` представлява конкретния потребител, приет от сървъра. Когато сървърът пише в този поток, съобщението отива само и единствено до човека, чиято връзка току-що е била приета.

> **ВАЖНО (`AutoFlush = true`):** По подразбиране `StreamWriter` използва вътрешен буфер и не изпраща данните веднага. Той изчаква буферът да се напълни, преди да ги изпрати. Задаването на `AutoFlush = true` променя това поведение, като гарантира, че всяко разпратено съобщение се избутва (flush-ва) по мрежата на момента. Това предотвратява задържането на съобщенията в паметта на сървъра.

### 2. Клиент (Client)

*   **Пълно дуплексна комуникация (Full-Duplex):** Комуникацията се осъществява чрез две нишки (`writerThread` и `readerThread`). Без тях програмата би се блокирала на всяка стъпка и не бихте могли да получите съобщение от друг човек, докато не натиснете Enter. Разделяйки процеса, едната част постоянно слуша и печата, а другата чака да напишете нещо, осъществявате едновременно изпращане и получаване.
*   **Защо ни е нужен writerThread.Join()?:** Ако го нямаше, методът `Connect()` щеше да продължи надолу и да достигне своя край, задействайки всички `using` блокове. Това би унищожило връзката, четеца и писача веднага. Извикването на `Join()` казва на главната нишка да спре и да не продължава, докато `writerThread` не приключи работа (при изпращане на "quit").
*   **Празният catch блок:** Когато напишете "quit", клиентът излиза от цикъла и главната програма затваря ресурсите. В този точен момент `readerThread` най-вероятно все още виси блокиран, чакайки съобщения от мрежата. Тъй като главната програма буквално му "дърпа шалтера" и затваря връзката под краката му, методът за четене хвърля грешка (напр. `IOException`). Тъй като ние сами затваряме връзката, грешката мълчаливо се игнорира, за да не плаши потребителя.
*   **Работа с мрежовия поток (NetworkStream):** За локалната мрежова връзка (напр. `serverConnection`), извикването на `GetStream()` отваря единия край на двупосочната тръба, която сочи към сървъра. С помощта на `StreamWriter` изпращате съобщения, а със `StreamReader` четете отговорите. Този край на тръбата и краят в сървъра представляват една и съща TCP връзка.

> **ВАЖНО (`AutoFlush = true`):** По подразбиране `StreamWriter` използва вътрешен буфер и не изпраща данните веднага. Той изчаква буферът да се напълни, преди да ги изпрати. Задаването на `AutoFlush = true` променя това поведение, като гарантира, че всяко съобщение се избутва (flush-ва) по мрежата на момента. Това е ключово за чат в реално време, тъй като предотвратява задържането на съобщения в паметта и премахва нуждата от ръчно извикване на `.Flush()` след всяко писане.