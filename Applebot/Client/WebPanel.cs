using ApplebotAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    class WebPanel
    {

        HttpListener _listener = new HttpListener();
        int _port = 6522; //Temp

        public WebPanel()
        {
            _listener.Prefixes.Add($"http://localhost:{_port}/");
        }

        public void Run()
        {
            _listener.Start();

            Logger.Log(Logger.Level.APPLICATION, $"WebPanel listening on port \"{_port}\"");

            while (true)
            {
                try
                {
                    var context = _listener.GetContext();
                    Task.Run(new Action(() =>
                    {
                        if (context.Request.IsWebSocketRequest)
                            HandleWsContext(context.AcceptWebSocketAsync(null).Result);
                        else
                            HandleHttpContext(context);
                    }));
                }
                catch (Exception)
                { }
            }
        }

        private void HandleHttpContext(HttpListenerContext context)
        {
            using (TextWriter writer = new StreamWriter(context.Response.OutputStream))
            {
                writer.WriteLine(Properties.Resources.index);
            }

            context.Response.OutputStream.Flush();
            context.Response.OutputStream.Close();
        }

        private async void HandleWsContext(WebSocketContext context)
        {
            WebSocket ws = context.WebSocket;

            string line;
            while ((line = ReadWebsocket(ws)) != null)
            {
                switch (line)
                {
                    case "OPEN":
                        await SendWebsocket(ws, "Websocket connected");
                        break;
                    case "BEEP":
                        await SendWebsocket(ws, "Boop");
                        break;
                }
            }

            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
        }

        private string ReadWebsocket(WebSocket ws)
        {
            List<byte> recieved = new List<byte>();
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[1024]);

            bool completed = false;
            while (!completed)
            {
                try
                {
                    WebSocketReceiveResult result = ws.ReceiveAsync(buffer, CancellationToken.None).Result;

                    recieved.AddRange(buffer.Take(result.Count));

                    if (result.EndOfMessage)
                        completed = true;
                }
                catch (Exception)
                { return null; }
            }

            return Encoding.UTF8.GetString(recieved.ToArray());
        }

        private Task SendWebsocket(WebSocket ws, string data)
        {
            ArraySegment<byte> buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(data));
            return ws.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }

    }
}
