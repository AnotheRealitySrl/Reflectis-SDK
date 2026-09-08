using Virtuademy.SDK.Core.SystemFramework;
using Virtuademy.SDK.Core.WebSocket;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;

#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

[CreateAssetMenu(menuName = "Virtuademy/SDK-WebSocket/WebSocketSystem", fileName = "WebSocketSystem")]
public class WebSocketSystem : BaseSystem, IWebSocketSystem
{
    [SerializeField] private bool secureConnection = true;

    /// <summary>
    /// Match url with handler
    /// </summary>
    private Dictionary<string, IWebSocketHandler> webSocketHandlers = new();

    public override Task Init()
    {
        base.Init();

#if UNITY_WEBGL && !UNITY_EDITOR
        GameObject webGLWebSocketHandler = new GameObject();
        webGLWebSocketHandler.AddComponent<WebSocketMessagesHandler>();
        DontDestroyOnLoad(webGLWebSocketHandler);
#else

#endif

        return Task.CompletedTask;
    }

    public async Task<bool> ConnectAsync(string url, Dictionary<string, string> queryParams, Action<string> onWebSocketOpenError = null)
    {
        IWebSocketHandler webSocketHandler = GetWebSocketHandler(url);
        //The socket exist and is already tryin to connect
        while (webSocketHandlers[url].ConnectionState == IWebSocketHandler.EWebSocketState.Connecting)
        {
            await Task.Yield();
        }
        //we are connected to this socket
        if (webSocketHandlers[url].ConnectionState == IWebSocketHandler.EWebSocketState.Open)
        {
            return true;
        }
        //Someone was closing this socket
        if (webSocketHandlers[url].ConnectionState == IWebSocketHandler.EWebSocketState.Closing)
        {
            await Task.Yield();
        }

        //try to connect to this socket
        try
        {
            await webSocketHandler.ConnectAsync(GetCompleteUrl(url, queryParams));

            return true;
        }
        catch (Exception e)
        {
            onWebSocketOpenError?.Invoke(e.Message);
            return false;
        }
    }

    public void AddListener(string url, IWebSocketListener webSocketListener)
    {
        GetWebSocketHandler(url).Listeners.Add(webSocketListener);
    }

    public void RemoveListener(string url, IWebSocketListener webSocketListener)
    {

        if (webSocketHandlers.ContainsKey(url))
        {
            if (!webSocketHandlers[url].Listeners.Contains(webSocketListener))
            {
                Debug.LogWarning("Trying to disconnect an inactive listener!");
            }
            else
            {
                webSocketHandlers[url].Listeners.Remove(webSocketListener);
                webSocketListener.OnWebSocketClose();

            }
        }
        else
        {
            Debug.LogWarning("Trying to disconnect a listener on an inactive socket! Url: " + url);
        }
    }

    public async Task DisconnectAsync(string url)
    {
        IWebSocketHandler webSocketHandler = GetWebSocketHandler(url);
        //if we are connecting wait and then disconnect
        while (webSocketHandler.ConnectionState == IWebSocketHandler.EWebSocketState.Connecting)
        {
            await Task.Yield();
        }

        if (webSocketHandler.ConnectionState == IWebSocketHandler.EWebSocketState.Open)
        {
            await webSocketHandlers[url].Disconnect();
        }

    }

    public async Task SendMessageAsync(string url, string message)
    {
        IWebSocketHandler webSocketHandler = GetWebSocketHandler(url);

        while (webSocketHandler.ConnectionState == IWebSocketHandler.EWebSocketState.Connecting)
        {
            await Task.Yield();
        }

        if (webSocketHandler.ConnectionState == IWebSocketHandler.EWebSocketState.Open)
        {
            await webSocketHandlers[url].SendMessage(message);
        }
        else
        {
            Debug.LogError("Trying to send message on an inactive socket! Url: " + url
                + ". Open a new websocket to send a message on this Url.");
        }
    }

    public async Task SendBufferMessageAsync(string url, byte[] buffer)
    {
        IWebSocketHandler webSocketHandler = GetWebSocketHandler(url);

        while (webSocketHandler.ConnectionState == IWebSocketHandler.EWebSocketState.Connecting)
        {
            await Task.Yield();
        }

        if (webSocketHandler.ConnectionState == IWebSocketHandler.EWebSocketState.Open)
        {
            await webSocketHandlers[url].SendBuffer(buffer);
        }
        else
        {
            Debug.LogError("Trying to send buffer on an inactive socket! Url: " + url
                + ". Open a new websocket to send a message on this Url.");
        }
    }

    private IWebSocketHandler GetWebSocketHandler(string url)
    {
        if (!webSocketHandlers.ContainsKey(url))
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            IWebSocketHandler webSocketHandler = new WebGLWebSocketHandler();
#else
            IWebSocketHandler webSocketHandler = new WebSocketHandler();
#endif
            webSocketHandlers.Add(url, webSocketHandler);
        }

        return webSocketHandlers[url];
    }

    private string GetCompleteUrl(string url, Dictionary<string, string> queryParams)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri parsedUri) ||
            (parsedUri.Scheme != "ws" && parsedUri.Scheme != "wss"))
        {
            url = $"ws{(secureConnection ? "s" : string.Empty)}://{url}";
        }

        string queryString = string.Empty;
        if (queryParams != null)
        {
            bool isFirst = true;
            foreach (KeyValuePair<string, string> item in queryParams)
            {
                queryString += (isFirst ? "?" : "&") + item.Key + "=" + item.Value;
                isFirst = false;
            }
        }

        return url + queryString;
    }

}
