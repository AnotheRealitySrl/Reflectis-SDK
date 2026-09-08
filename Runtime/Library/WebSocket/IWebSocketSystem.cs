using Virtuademy.SDK.Core.SystemFramework;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Virtuademy.SDK.Core.WebSocket
{
    public interface IWebSocketSystem : ISystem
    {
        /// <summary>
        /// Connect to a webSocket, returns false if the connection failed
        /// To make sure that you do not lose the first message received by the socket, add any listener before connecting to the socket
        /// </summary>
        /// <param name="url"></param>
        /// <param name="queryParams"></param>
        /// <param name="onWebSocketOpenError">if there was an error while opening the websocket call the action with the error message param</param>
        /// <returns></returns>
        Task<bool> ConnectAsync(string url, Dictionary<string, string> queryParams, Action<string> onWebSocketOpenError = null);

        /// <summary>
        /// Add listener to the webSocket
        /// To make sure that you do not lose the first message received by the socket, add the listener before connecting to the socket.
        /// Every listener will be removed at the first socket disconnection.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="webSocketListener"></param>
        void AddListener(string url, IWebSocketListener webSocketListener);

        /// <summary>
        /// Disconnect this listener from the socket, this will NOT call the listener onClose
        /// </summary>
        /// <param name="url"></param>
        /// <param name="webSocketListener"></param>
        /// <returns></returns>
        void RemoveListener(string url, IWebSocketListener webSocketListener);

        /// <summary>
        /// Hard disconnect from the socket
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        Task DisconnectAsync(string url);

        /// <summary>
        /// Send message to a websocket we are connected to. 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public Task SendMessageAsync(string url, string message);

        /// <summary>
        /// Send buffer to a websocket we are connected to. 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public Task SendBufferMessageAsync(string url, byte[] buffer);
    }
}
