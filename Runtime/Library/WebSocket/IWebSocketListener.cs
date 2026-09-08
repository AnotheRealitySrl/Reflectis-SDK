namespace Virtuademy.SDK.Core.WebSocket
{
    public interface IWebSocketListener
    {
        public void OnWebSocketError(string error);
        public void OnWebSocketMessageReceived(string data);
        public void OnWebSocketBinaryMessageReceived(byte[] buffer);
        public void OnWebSocketClose();
    }
}
