mergeInto(LibraryManager.library, {
  sockets: [], 
  handler: "",

  WebSocketInit: function (handler) {
    handler = UTF8ToString(handler);
    this.handler = handler;

    // Inizializza l'array sockets se non è già stato fatto
    if (!this.sockets) {
      this.sockets = [];
    }

    console.log("Handler: " + this.handler);
  },

  WebSocketOpen: function (url) {
    url = UTF8ToString(url);

    console.log(url);

    const socket = new WebSocket(url);

    // Impostiamo esplicitamente il tipo binario su arraybuffer.
    // Senza questo, i dati binari arriverebbero come 'Blob'.
    socket.binaryType = "arraybuffer"; 

    const socketIndex = this.sockets.length;

    this.sockets.push(socket);

    socket.addEventListener("open", (event) => {
      SendMessage(this.handler, "SocketOpenEventHandler", socketIndex);
    });

    socket.addEventListener("close", (event) => {
      SendMessage(this.handler, "SocketCloseEventHandler", socketIndex);
    });

    socket.addEventListener("message", (event) => {
      // binary frame: convert to string
      if (event.data instanceof ArrayBuffer) {
        const bufView = new Uint8Array(event.data); // Usa `event.data` anziché `buffer`

        // 1. Converti i byte in una stringa binaria grezza
        // (Attenzione allo stack overflow per file grandi, meglio un loop se >60kb)
        let binaryString = "";
        const len = bufView.byteLength;
        for (let i = 0; i < len; i++) {
          binaryString += String.fromCharCode(bufView[i]);
        }
        
        // 2. CODIFICA IN BASE64
        const base64String = btoa(binaryString);

        SendMessage(
          this.handler,
          "SocketBinaryMessageEventHandler",
          socketIndex + "|" + base64String
        );
      }
      // text frame
      else {
        SendMessage(
          this.handler,
          "SocketMessageEventHandler",
          socketIndex + "|" + event.data
        );
      }
    });

    socket.addEventListener("error", (event) => {
      SendMessage(this.handler, "SocketErrorEventHandler", socketIndex);
    });

    return socketIndex;
  },

  WebSocketSendMessage: function (socketIndex, message) {
    message = UTF8ToString(message);

    if (socketIndex < 0 || socketIndex >= this.sockets.length) {
      console.error("Invalid socket index: ", socketIndex);
      return;
    }

    try {
      const socket = this.sockets[socketIndex];
      if (socket) {
        socket.send(message);
      }
    } catch (error) {
      console.error(error);
    }
  },

  WebSocketSendBuffer: function (socketIndex, bufferPtr, length) {
    if (socketIndex < 0 || socketIndex >= this.sockets.length) {
      console.error("Invalid socket index: ", socketIndex);
      return;
    }

    try {
      const socket = this.sockets[socketIndex];
      if (socket) {
        // ACCESSO ALLA MEMORIA DI UNITY
        // HEAPU8 è l'array globale che rappresenta la RAM del gioco.
        // Creiamo una subarray che punta esattamente ai dati passati da C#.
        // bufferPtr = inizio, bufferPtr + length = fine.
        const dataToSend = HEAPU8.subarray(bufferPtr, bufferPtr + length);

        // socket.send accetta Uint8Array, quindi invierà binary frame.
        socket.send(dataToSend);
      }
    } catch (error) {
      console.error(error);
    }
  },

  WebSocketClose: function (socketIndex) {

    if (socketIndex < 0 || socketIndex >= this.sockets.length) {
      console.error("Invalid socket index: ", socketIndex);
      return;
    }

    const socket = this.sockets[socketIndex];
    if (socket) {
      socket.close();
    } else {
      console.error("Socket not found");
    }
  },
});
