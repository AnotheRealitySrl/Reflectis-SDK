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
        url = ToWebSocketScheme(url);

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

    /// <summary>
    /// The same address expressed as a WebSocket URL: <c>http</c> becomes <c>ws</c>,
    /// <c>https</c> becomes <c>wss</c>, an address with no scheme at all gets one from
    /// <see cref="secureConnection"/>, and a <c>ws</c>/<c>wss</c> URL is already right.
    /// </summary>
    /// <remarks>
    /// This used to prepend a scheme whenever the URL did not already carry <c>ws</c> or
    /// <c>wss</c> — which quietly required every caller to hand over a *schemeless* host,
    /// since anything else produced <c>wss://https://host/path</c>. That parses: the
    /// authority ends at the next slash, so the host becomes literally <c>https</c>, DNS
    /// fails to resolve it, and the only symptom is "Unable to connect to the remote
    /// server" — an error that says nothing about the address being malformed.
    /// <para>
    /// It held together because the requirement was met upstream, in the platform data:
    /// <c>ctn_config.realtimeApiUrl</c> and <c>aiApiUrl</c> are stored as bare hostnames
    /// while the other three URLs in the same object carry <c>https://</c> — and those two
    /// are exactly the APIs reached over a WebSocket. So the convention was real, just
    /// undocumented and enforced nowhere.
    /// </para>
    /// <para>
    /// <c>config.apis.cpi_baseurls</c>, which feeds endpoint discovery and the generated
    /// endpoint asset (ADR 0024 / 0025), stores the same two addresses canonically with a
    /// scheme. The two sources therefore disagree on format, and a client switching from
    /// one to the other trips this. Handling both here is what makes that switch safe, and
    /// what lets the tenant-config side be normalised later without breaking clients still
    /// reading it.
    /// </para>
    /// </remarks>
    private string ToWebSocketScheme(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri parsedUri))
        {
            return $"ws{(secureConnection ? "s" : string.Empty)}://{url}";
        }

        switch (parsedUri.Scheme)
        {
            case "ws":
            case "wss":
                return url;

            case "http":
            case "https":
                string rest = url.Substring(url.IndexOf("://", StringComparison.Ordinal) + 3);
                return $"ws{(parsedUri.Scheme == "https" ? "s" : string.Empty)}://{rest}";

            default:
                Debug.LogWarning($"[{name}] '{url}' carries the scheme '{parsedUri.Scheme}', which is neither " +
                                 "a WebSocket nor an HTTP one. Connecting to it as-is; if that fails, the " +
                                 "address is coming from somewhere that should be reporting http/https.");
                return url;
        }
    }

}
