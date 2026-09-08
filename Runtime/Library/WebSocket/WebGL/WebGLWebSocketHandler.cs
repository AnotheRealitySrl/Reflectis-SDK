#if UNITY_WEBGL && !UNITY_EDITOR
using Virtuademy.SDK.Core.WebSocket;

using System.Collections.Generic;
using System.Threading.Tasks;

public class WebGLWebSocketHandler : IWebSocketHandler
{

    private List<IWebSocketListener> listeners = new List<IWebSocketListener>();

    public List<IWebSocketListener> Listeners => listeners;

    public IWebSocketHandler.EWebSocketState ConnectionState { get; set; }

    public async Task ConnectAsync(string url)
    {
        ConnectionState = IWebSocketHandler.EWebSocketState.Connecting;
        await WebSocketMessagesHandler.Instance.Connect(url, this);
        ConnectionState = IWebSocketHandler.EWebSocketState.Open;
    }


    public Task SendMessage(string message)
    {
        WebSocketMessagesHandler.Instance.SendMessage(this, message);
        return Task.CompletedTask;
    }

    public Task SendBuffer(byte[] buffer)
    {
        WebSocketMessagesHandler.Instance.SendBuffer(this, buffer);
        return Task.CompletedTask;
    }

    public void OnSocketClose()
    {
        foreach (IWebSocketListener listener in listeners)
        {
            listener.OnWebSocketClose();
        }
        listeners.Clear();
        ConnectionState = IWebSocketHandler.EWebSocketState.Closed;
    }

    public async Task Disconnect()
    {
        ConnectionState = IWebSocketHandler.EWebSocketState.Closing;
        await WebSocketMessagesHandler.Instance.CloseSocket(this);
        OnSocketClose();
    }

    public void RemoveListener(IWebSocketListener listener)
    {
        listeners.Remove(listener);
    }

    internal void OnMessageReceived(string message)
    {
        foreach (IWebSocketListener listener in listeners)
        {
            if (listener != null)
            {
                listener.OnWebSocketMessageReceived(message);
            }
        }
    }

    internal void OnBinaryMessageReceived(byte[] buffer)
    {
        foreach (IWebSocketListener listener in listeners)
        {
            if (listener != null)
            {
                listener.OnWebSocketBinaryMessageReceived(buffer);
            }
        }
    }

    internal void OnSocketError()
    {
        foreach (IWebSocketListener listener in listeners)
        {
            if (listener != null)
            {
                listener.OnWebSocketError("WebGl socket error.");
            }
        }
    }
}
#endif
