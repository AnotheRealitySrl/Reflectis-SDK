using System;

namespace Virtuademy.SDK.Core.WebSocket
{
    public class BaseWebSocketListener : IWebSocketListener
    {
        public Action onClose;

        public Action<string> onMessageReceived;

        public Action<byte[]> onBinaryMessageReceived;

        public Action<string> onError;

        public void OnWebSocketClose()
        {
            onClose?.Invoke();
        }

        public void OnWebSocketError(string error)
        {
            onError?.Invoke(error);
        }

        public void OnWebSocketMessageReceived(string data)
        {
            onMessageReceived?.Invoke(data);
        }

        public void OnWebSocketBinaryMessageReceived(byte[] buffer)
        {
            onBinaryMessageReceived?.Invoke(buffer);
        }
    }
}
