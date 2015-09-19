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
    public class DiscordMessage : Message
    {
        public string UserID { get; private set; }
        public string ChannelID { get; private set; }
        public string ID { get; private set; }

        public DiscordMessage(string sender, string content, string userID, string channelID, string id) : base(sender, content)
        {
            UserID = userID;
            ChannelID = channelID;
            ID = id;
        }
    }

    public class DiscordPlatform : Platform
    {

        ClientWebSocket _socket;
        NameValueCollection _loginData;
        string _token;

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
            _token = GetLoginToken();

            _socket = new ClientWebSocket();

            Logger.Log(Logger.Level.PLATFORM, "Attempting connection to Discord websocket hub server");

            _socket.ConnectAsync(new Uri("wss://discordapp.com/hub"), CancellationToken.None).Wait();

            string connectionData = CreateConnectionData(_token);

            SendString(connectionData);

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

                                    SendString(datePacket.ToString());

                                    Thread.Sleep(int.Parse(data["d"]["heartbeat_interval"].ToString()) - 5000);
                                }
                            });
                            break;
                        }
                    case "MESSAGE_CREATE":
                        {
                            string user = data["d"]["author"]["username"].ToString();
                            string content = data["d"]["content"].ToString();
                            string userID = data["d"]["author"]["id"].ToString();
                            string channelID = data["d"]["channel_id"].ToString();
                            string id = data["d"]["id"].ToString();
                            ProcessMessage(this, new DiscordMessage(user, content, userID, channelID, id));
                            break;
                        }
                }
            }
        }

        private Task SendString(string data)
        {
            ArraySegment<byte> buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(data));
            return _socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public override void Send<T1>(T1 data)
        {
            if (!(data.Origin is DiscordMessage))
            {
                Logger.Log(Logger.Level.ERROR, "Discord platform recieved a SendData derived class whose origin is not that of DiscordMessage, send request is being ignored");
                return;
            }

            JObject content = new JObject();
            content.Add("content", data.Content);

            HttpWebRequest request = WebRequest.CreateHttp(string.Format(@"https://discordapp.com/api/channels/{0}/messages", (data.Origin as DiscordMessage).ChannelID));
            request.Method = "POST";
            request.Headers.Add("authorization", _token);
            request.ContentType = "application/json";

            StreamWriter requestWriter = new StreamWriter(request.GetRequestStream());
            requestWriter.Write(content.ToString());
            requestWriter.Flush();
            requestWriter.Close();

            var response = request.GetResponse();
        }

    }
}
