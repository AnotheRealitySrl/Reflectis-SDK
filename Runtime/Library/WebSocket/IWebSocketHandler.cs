using Virtuademy.SDK.Core.WebSocket;

using System.Collections.Generic;
using System.Threading.Tasks;

public interface IWebSocketHandler
{
    public enum EWebSocketState
    {
        Closed = 0,
        Connecting,
        Open,
        Closing
    }

    public EWebSocketState ConnectionState { get; }

    public abstract List<IWebSocketListener> Listeners { get; }

    public abstract Task ConnectAsync(string url);

    public abstract Task SendMessage(string message);

    public abstract Task SendBuffer(byte[] buffer);

    public abstract Task Disconnect();


}
