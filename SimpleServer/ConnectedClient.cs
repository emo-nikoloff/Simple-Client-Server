using System.Net.Sockets;

namespace SimpleServer;

public class ConnectedClient
{
    public TcpClient Tcp { get; private set; }

    public StreamWriter Writer { get; private set; }

    public string Username { get; private set; }

    public ConnectedClient(TcpClient tcp, StreamWriter writer, string username)
    {
        Tcp = tcp;
        Writer = writer;
        Username = username;
    }

    public override string ToString() => $"{Username} [{Tcp.Client.RemoteEndPoint?.ToString() ?? "Unknown IP"}]";
}
