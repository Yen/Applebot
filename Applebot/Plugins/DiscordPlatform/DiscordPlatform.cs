using ApplebotAPI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
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

        private const long BitwiseElevatedPermission = 1 << 13;

        public class Role
        {
            public string ID;
            public string Name;
            public long Permissions;
        }

        public class Member
        {
            public string User;
            public string ID;
            public List<string> RoleIDs = new List<string>();
        }

        public class Channel
        {
            public class PermissionOverwrites
            {
                public string Type;
                public string ID;
                public int Allow;
                public int Deny;
            }

            public string ID;
            public string Name;
            public string Type;
            public int Position;
            public List<PermissionOverwrites> Overwrites = new List<PermissionOverwrites>();
        }

        public class Guild
        {
            public string OwnerID;
            public string ID;
            public string Name;
            public List<Role> Roles = new List<Role>();
            public List<Member> Members = new List<Member>();
            public List<Channel> Channels = new List<Channel>();
        }

        ClientWebSocket _socket;
        NameValueCollection _loginData;
        string _token;
        object _connectionLock = new object();

        SynchronizedCollection<Guild> _guilds = new SynchronizedCollection<Guild>();
        int _taskID = 0;

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
            lock (_connectionLock)
            {
                List<byte> recieved = new List<byte>();
                ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[1024]);

                bool completed = false;
                while (!completed)
                {
                    try
                    {
                        var result = _socket.ReceiveAsync(buffer, CancellationToken.None).Result;

                        if (result.Count == 0)
                        {
                            Reconnect();
                            return RecieveDiscord();
                        }


                        recieved.AddRange(buffer.Take(result.Count));

                        if (result.EndOfMessage)
                            completed = true;
                    }
                    catch
                    {
                        Reconnect();
                        return RecieveDiscord();
                    }
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
        }

        private void Reconnect()
        {
            lock (_connectionLock)
            {
                _guilds.Clear();

                _token = GetLoginToken();

                if (_socket != null)
                {
                    if (_socket.State != WebSocketState.Closed)
                        _socket.CloseAsync(WebSocketCloseStatus.Empty, null, CancellationToken.None);
                    _socket.Dispose();
                }
                _socket = new ClientWebSocket();
                _socket.Options.KeepAliveInterval = TimeSpan.Zero;

                Logger.Log(Logger.Level.PLATFORM, "Attempting connection to Discord websocket hub server");

                _socket.ConnectAsync(new Uri("wss://discordapp.com/hub"), CancellationToken.None).Wait();

                string connectionData = CreateConnectionData(_token);

                SendString(connectionData);
            }
        }

        private JToken GetJsonObject(JToken data, params string[] args)
        {
            JToken result = data;
            foreach (var arg in args)
            {
                result = result[arg];
                if (result == null)
                    return null;
            }
            return result;
        }

        private void HandlePacket(JToken data)
        {
            var type = GetJsonObject(data, "t");
            if (type == null) return;

            string value = type.ToString();
            switch (value)
            {
                case "READY":
                    {
                        Logger.Log(Logger.Level.PLATFORM, "Ready packet revieved from Discord");
                        _taskID++;
                        Task.Run(() =>
                        {
                            int id = _taskID;
                            Logger.Log(Logger.Level.PLATFORM, $"Starting new Discord keep alive task, id ({id})");
                            bool running = true;
                            int interval = int.Parse(data["d"]["heartbeat_interval"].ToString());
                            while (running)
                            {
                                if (id != _taskID)
                                {
                                    running = false;
                                    continue;
                                }

                                DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

                                long date = (long)(DateTime.UtcNow - origin).TotalMilliseconds;

                                JObject datePacket = new JObject();
                                datePacket.Add("op", 1);
                                datePacket.Add("d", date);

                                SendString(datePacket.ToString());

                                Thread.Sleep(TimeSpan.FromMilliseconds(interval - 5000));
                            }
                            Logger.Log(Logger.Level.PLATFORM, $"Discord keep alive task, id ({id}), terminated");
                        });

                        foreach (var guild_ in data["d"]["guilds"])
                        {
                            Guild guild = new Guild();
                            guild.Name = guild_["name"].ToString();
                            guild.OwnerID = guild_["owner_id"].ToString();
                            guild.ID = guild_["id"].ToString();

                            foreach (var role_ in guild_["roles"])
                            {
                                Role role = new Role();
                                role.Permissions = long.Parse(role_["permissions"].ToString());
                                role.Name = role_["name"].ToString();
                                role.ID = role_["id"].ToString();

                                guild.Roles.Add(role);
                            }

                            foreach (var member_ in guild_["members"])
                            {
                                Member member = new Member();
                                member.User = member_["user"]["username"].ToString();
                                member.ID = member_["user"]["id"].ToString();

                                foreach (var role_ in member_["roles"])
                                {
                                    member.RoleIDs.Add(role_.ToString());
                                }

                                guild.Members.Add(member);
                            }

                            foreach (var channel_ in guild_["channels"])
                            {
                                Channel channel = new Channel();
                                channel.Type = channel_["type"].ToString();
                                channel.Position = int.Parse(channel_["position"].ToString());
                                channel.Name = channel_["name"].ToString();
                                channel.ID = channel_["id"].ToString();

                                foreach (var overwrite_ in channel_["permission_overwrites"])
                                {
                                    var overwrite = new Channel.PermissionOverwrites();
                                    overwrite.Type = overwrite_["type"].ToString();
                                    overwrite.ID = overwrite_["id"].ToString();
                                    overwrite.Deny = int.Parse(overwrite_["deny"].ToString());
                                    overwrite.Allow = int.Parse(overwrite_["allow"].ToString());

                                    channel.Overwrites.Add(overwrite);
                                }

                                guild.Channels.Add(channel);
                            }

                            _guilds.Add(guild);
                        }
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
                case "GUILD_MEMBER_UPDATE":
                    {
                        var guilds = _guilds.Where(a => a.ID == data["d"]["guild_id"].ToString());

                        if (guilds.Count() == 1)
                        {
                            var members = guilds.First().Members.Where(a => a.ID == data["d"]["user"]["id"].ToString());

                            if (members.Count() == 1)
                            {
                                members.First().RoleIDs.Clear();
                                foreach (var role in data["d"]["roles"])
                                {
                                    members.First().RoleIDs.Add(role.ToString());
                                }
                            }
                        }

                        break;
                    }
                case "GUILD_ROLE_UPDATE":
                    {
                        var guilds = _guilds.Where(a => a.ID == data["d"]["guild_id"].ToString());

                        if (guilds.Count() == 1)
                        {
                            var roles = guilds.First().Roles.Where(a => a.ID == data["d"]["role"]["id"].ToString());

                            if (roles.Count() == 1)
                            {
                                roles.First().Permissions = long.Parse(data["d"]["role"]["permissions"].ToString());
                                roles.First().Name = data["d"]["role"]["name"].ToString();
                            }
                        }

                        break;
                    }
                case "GUILD_ROLE_DELETE":
                    {
                        var guilds = _guilds.Where(a => a.ID == data["d"]["guild_id"].ToString());

                        if (guilds.Count() == 1)
                        {
                            var roles = guilds.First().Roles.Where(a => a.ID == data["d"]["role_id"].ToString());

                            if (roles.Count() == 1)
                            {
                                guilds.First().Roles.Remove(roles.First());
                            }
                        }

                        break;
                    }
                case "GUILD_ROLE_CREATE":
                    {
                        var guilds = _guilds.Where(a => a.ID == data["d"]["guild_id"].ToString());

                        if (guilds.Count() == 1)
                        {
                            Role role = new Role();
                            role.Permissions = long.Parse(data["d"]["role"]["permissions"].ToString());
                            role.Name = data["d"]["role"]["name"].ToString();
                            role.ID = data["d"]["role"]["id"].ToString();

                            guilds.First().Roles.Add(role);
                        }

                        break;
                    }
                case "PRESENCE_UPDATE":
                    {
                        var guilds = _guilds.Where(a => a.ID == data["d"]["guild_id"].ToString());

                        if (guilds.Count() == 1)
                        {
                            var members = guilds.First().Members.Where(a => a.ID == data["d"]["user"]["id"].ToString());

                            if (members.Count() == 0)
                            {
                                Member member = new Member();
                                member.ID = data["d"]["user"]["id"].ToString();
                                member.User = data["d"]["user"]["username"].ToString();

                                guilds.First().Members.Add(member);
                            }
                            else if (members.Count() == 1)
                            {
                                members.First().User = data["d"]["user"]["username"].ToString();
                            }
                        }

                        break;
                    }
                case "TYPING_START":
                case "MESSAGE_ACK": // possibly something to do with the unread messages prompts in the client, not needed
                case "VOICE_STATE_UPDATE":
                case "MESSAGE_UPDATE": // seems to be information on embeds once the server has looked over it
                    {
                        // ignored messages
                        break;
                    }
                default:
                    {
                        Logger.Log(Logger.Level.WARNING, $"Unknown type {value}");
                        break;
                    }
            }
        }

        public override void Run()
        {
            Reconnect();

            while (true)
            {
                JObject data = RecieveDiscord();

                if (data == null)
                {
                    Logger.Log(Logger.Level.WARNING, "Error parsing packet recieved from Discord, skipping");
                    continue;
                }

                HandlePacket(data);
            }
        }

        private Task SendString(string data)
        {
            try
            {
                ArraySegment<byte> buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(data));
                return _socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch
            {
                Reconnect();
                return SendString(data);
            }
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
            request.ContentType = "application/json";
            request.Headers.Add("authorization", _token);
            StreamWriter requestWriter = new StreamWriter(request.GetRequestStream());
            requestWriter.Write(content.ToString());
            requestWriter.Flush();
            requestWriter.Close();

            try
            {
                request.GetResponse().Close();
            }
            catch
            {
                Reconnect();
                Send(data);
            }
        }

        public override bool CheckElevatedStatus(Message message)
        {
            if (!(message is DiscordMessage))
            {
                Logger.Log(Logger.Level.ERROR, "Discord platform recieved a Message derived class that of DiscordMessage in check elevated status");
                return false;
            }

            var userID = (message as DiscordMessage).UserID;
            var channelID = (message as DiscordMessage).ChannelID;

            var guild = _guilds.Where(a => a.Channels.Any(b => b.ID == channelID));

            if (guild.Count() != 1)
                return false;

            if (guild.First().OwnerID == userID)
            {
                return true;
            }

            var member = guild.First().Members.Where(a => a.ID == userID);

            if (member.Count() != 1)
                return false;

            List<Role> roles = new List<Role>();

            foreach (var role in guild.First().Roles)
            {
                foreach (var roleID in member.First().RoleIDs)
                {
                    if (roleID == role.ID)
                    {
                        roles.Add(role);
                    }
                }
            }

            if (roles.Count == 0)
                return false;

            foreach (var role in roles)
            {
                if ((role.Permissions & BitwiseElevatedPermission) == BitwiseElevatedPermission)
                    return true;
            }

            return false;
        }

    }
}
