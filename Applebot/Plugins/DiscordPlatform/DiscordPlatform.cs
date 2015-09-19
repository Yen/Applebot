using ApplebotAPI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace DiscordPlatform
{
    public class DiscordPlatform : Platform
    {

        ClientWebSocket _socket;
        NameValueCollection _loginData;

        public DiscordPlatform()
        {
            if (!File.Exists("Settings/discordsettings.xml"))
            {
                Logger.Log(Logger.Level.ERROR, "Settings file \"{0}\" does not exist", "discordsettings.xml");
                State = PlatformState.Unready;
                return;
            }
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load("Settings/discordsettings.xml");
            }
            catch
            {
                Logger.Log(Logger.Level.ERROR, "Error reading settings file \"{0}\"", "Settings/discordsettings.xml");
                State = PlatformState.Unready;
                return;
            }

            var emailBuf = doc.SelectSingleNode("settings/email");
            var passBuf = doc.SelectSingleNode("settings/pass");

            if ((emailBuf == null) || (passBuf == null))
            {
                Logger.Log(Logger.Level.ERROR, "Settings file \"{0}\" is missing required values", "Settings/discordsettings.xml");
                State = PlatformState.Unready;
                return;
            }

            string email = emailBuf.InnerXml;
            string pass = passBuf.InnerXml;

            _loginData = new NameValueCollection();
            _loginData.Add("email", email);
            _loginData.Add("password", pass);
        }

        private string GetLoginToken()
        {
            Logger.Log(Logger.Level.PLATFORM, "Attempting to recieve auth token from Discord login server");

            try
            {
                using (WebClient client = new WebClient())
                {
                    byte[] buffer = client.UploadValues("https://discordapp.com/api/auth/login", _loginData);

                    JToken json = JToken.Parse(Encoding.UTF8.GetString(buffer));
                    return json["token"].ToString();
                }
            }
            catch
            {
                Logger.Log(Logger.Level.ERROR, "Error getting Discord auth token, retrying");
                Thread.Sleep(TimeSpan.FromSeconds(1)); //Just stops spam if the server is offline or something
                return GetLoginToken();
            }
        }

        private string CreateConnectionData(string token)
        {
            JObject result = new JObject();

            result.Add("op", 2);

            JObject d = new JObject();
            d.Add("token", token);
            d.Add("v", 2);

            JObject prop = new JObject();
            prop.Add("os", Environment.OSVersion.ToString());
            prop.Add("browser", "");
            prop.Add("device", "");
            prop.Add("referrer", "");
            prop.Add("referring_domain", "");

            d.Add("properties", prop);

            result.Add("d", d);

            return result.ToString();
        }

        private JObject RecieveDiscord()
        {
            List<byte> recieved = new List<byte>();
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[1024]);

            bool completed = false;
            while (!completed)
            {
                var result = _socket.ReceiveAsync(buffer, CancellationToken.None).Result;

                recieved.AddRange(buffer.Take(result.Count));

                if (result.EndOfMessage)
                    completed = true;
            }

            string decoded = Encoding.UTF8.GetString(recieved.ToArray());

            try
            {
                return JObject.Parse(decoded);
            }
            catch
            {
                return null;
            }
        }

        public override void Run()
        {
            string token = GetLoginToken();

            _socket = new ClientWebSocket();

            Logger.Log(Logger.Level.PLATFORM, "Attempting connection to Discord websocket hub server");

            _socket.ConnectAsync(new Uri("wss://discordapp.com/hub"), CancellationToken.None).Wait();

            string connectionData = CreateConnectionData(token);

            SendData(connectionData);

            while (true)
            {
                JObject data = RecieveDiscord();

                if (data == null)
                {
                    Logger.Log(Logger.Level.WARNING, "Error parsing packet recieved from Discord, skipping");
                    continue;
                }

                var type = data["t"];
                if (type == null)
                {
                    Logger.Log(Logger.Level.WARNING, "Packet recieved from Discord does not contain a defined type");
                    continue;
                }


                string value = type.ToString();
                switch (value)
                {
                    case "READY":
                        {
                            Logger.Log(Logger.Level.PLATFORM, "Ready packet revieved from Discord, starting stayalive loop");
                            Task.Run(() =>
                            {
                                while (true)
                                {
                                    DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

                                    long date = (long)(DateTime.UtcNow - origin).TotalMilliseconds;

                                    JObject datePacket = new JObject();
                                    datePacket.Add("op", 1);
                                    datePacket.Add("d", date);

                                    SendData(datePacket.ToString());

                                    Logger.Log(Logger.Level.DEBUG, "Sending stayalive packet");

                                    Thread.Sleep(int.Parse(data["d"]["heartbeat_interval"].ToString()) - 5000);
                                }
                            });
                            break;
                        }
                    default:
                        {
                            Logger.Log(Logger.Level.WARNING, "Discord platform recieved unknown message type \"{0}\"", value);
                            break;
                        }
                }
            }
        }

        private Task SendData(string data)
        {
            ArraySegment<byte> buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(data));
            return _socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public override void Send<T1>(T1 data)
        {

        }
    }
}
