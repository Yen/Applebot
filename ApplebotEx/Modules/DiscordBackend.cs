using System;
using Newtonsoft.Json;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace ApplebotEx.Modules
{
    public class DiscordBackend : ResourceDependentService<DiscordBackend.Settings>, IChatMessageHost
    {
        public class Settings
        {
            [JsonRequired]
            [JsonProperty("gateway")]
            public string Gateway = null;

            [JsonRequired]
            [JsonProperty("token")]
            public string Token = null;
        }

        public class Metadata
        {
            public DiscordPacket.User User;
            public DiscordPacket.Guild Guild;
            public string ChannelID;
        }

        public event ChatMessageHostReceiveDelegate ReceiveMessage;

        private ClientWebSocket _Socket;
        private object _SocketSendLock = new object();

        private int _LastMessageSequence = 0;

        private DiscordPacket.User _BotUser;
        private List<DiscordPacket.Guild> _Guilds = new List<DiscordPacket.Guild>();

        private Queue<Tuple<string, Metadata>> _MessageQueue = new Queue<Tuple<string, Metadata>>();
        private object _MessageQueueLock = new object();

        private Thread _SendRunnerThread;
        private bool _SendRunnerCancelRequest;

        private Thread _HeartbeatThread;
        private bool _HeartbeatCancelRequest;
        private object _HeartbeatLock = new object();

        public DiscordBackend() : base("Resources/DiscordBackend.json")
        { }

        protected override bool Bootstrap()
        {
            // restart runner
            new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        _Reconnect();
                        _StartRunners();
                        _ReceiveRunner();
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Unhandled exception -> {ex.Message}", LoggerColor.Red);
                    }
                    finally
                    {
                        _StopRunners();
                        Logger.Log("Reconnecting");
                        Thread.Sleep(TimeSpan.FromSeconds(5));
                    }
                }
            }).Start();

            return true;
        }

        private void _Reconnect()
        {
            _Guilds.Clear();

            Logger.Log($"Connecting to Discord gateway -> {Resource.Gateway}");

            if (_Socket != null)
                _Socket.Dispose();
            _Socket = new ClientWebSocket();

            _Socket.ConnectAsync(new Uri(Resource.Gateway), CancellationToken.None).Wait();

            _SendString(_CreateConnectionPacket(Resource.Token));
        }

        private void _StopRunners()
        {
            _SendRunnerCancelRequest = true;
            _HeartbeatCancelRequest = true;
            lock (_MessageQueueLock)
                Monitor.Pulse(_MessageQueueLock);

            if (_HeartbeatThread != null)
                lock (_HeartbeatLock)
                    Monitor.Pulse(_HeartbeatLock);

            Logger.Log($"Waiting for {nameof(_SendRunnerThread)} to end");
            _SendRunnerThread.Join();

            Logger.Log($"Waiting for {nameof(_HeartbeatThread)} to end");
            if (_HeartbeatThread != null)
            {
                _HeartbeatThread.Join();
                _HeartbeatThread = null;
            }
            _HeartbeatCancelRequest = false;
        }

        private void _StartRunners()
        {
            _SendRunnerCancelRequest = false;
            _SendRunnerThread = new Thread(_SendRunner);

            Logger.Log($"Starting {nameof(_SendRunnerThread)}");
            _SendRunnerThread.Start();
        }

        private void _ReceiveRunner()
        {
            string line;
            while ((line = _ReceivePacket()) != null)
            {
                var packet = _DecodePacket(line);
                if (packet == null)
                {
                    Logger.Log($"Could not decode packet, ignoring");
                    continue;
                }
                _LastMessageSequence = packet.SequenceNumber;
                switch (packet.Type)
                {
                    case "READY":
                        Logger.Log("Ready message received");
                        _HandleReadyPacket((DiscordPacket.Ready)packet.Data);
                        break;
                    case "MESSAGE_CREATE":
                        var message = (DiscordPacket.Message)packet.Data;
                        Logger.Log($"{message.Author.Username} @ {message.ChannelID} -> {message.Content}");
                        _HandleMessage(message);
                        break;
                    case "GUILD_CREATE":
                        _CreateGuild((DiscordPacket.Guild)packet.Data);
                        break;
                    case "GUILD_UPDATE":
                        _UpdateGuild((DiscordPacket.Guild)packet.Data);
                        break;
                    case "GUILD_DELETE":
                        _DeleteGuild((DiscordPacket.GuildDelete)packet.Data);
                        break;
                }
            }
        }

        private void _SendRunner()
        {
            while (true)
                lock (_MessageQueueLock)
                {
                    if (_SendRunnerCancelRequest)
                        break;

                    if (!_MessageQueue.Any())
                    {
                        Monitor.Wait(_MessageQueueLock);
                        continue;
                    }

                    var message = _MessageQueue.First();
                    var result = _SendMessageRaw(message.Item2.ChannelID, message.Item1);
                    switch (result.Item1)
                    {
                        case SendMessageResponse.RateLimited:
                            TimeSpan time;
                            if (result.Item2 == null)
                            {
                                Logger.Log("Discord rate limited but no retry info was sent, using default timeout");
                                time = TimeSpan.FromSeconds(5);
                            }
                            else
                                time = TimeSpan.FromMilliseconds(result.Item2.Value);

                            Logger.Log($"Discord rate limited for {time}");

                            Monitor.Exit(_MessageQueueLock);
                            Thread.Sleep(time + TimeSpan.FromMilliseconds(500));
                            Monitor.Enter(_MessageQueueLock);
                            break;
                        case SendMessageResponse.Failed:
                            // if it failed assume the message is invalid
                            _MessageQueue.Dequeue();

                            // crash the backend to force restart
                            Logger.Log("Send runner crashing to force restart");
                            try { _Socket.Abort(); } catch { }
                            return;
                        case SendMessageResponse.Success:
                            _MessageQueue.Dequeue();
                            break;
                    }
                }
        }

        private void _CreateGuild(DiscordPacket.Guild guild)
        {
            if (_Guilds.Any(x => x.ID == guild.ID))
            {
                Logger.Log("WARN: Guild create called for already existing guild, removing old");
                _Guilds.RemoveAll(x => x.ID == guild.ID);
            }
            _Guilds.Add(guild);
            Logger.Log($"Created guild -> {guild.ID} ({guild.Name})");
        }

        private void _UpdateGuild(DiscordPacket.Guild guild)
        {
            var index = _Guilds.FindIndex(x => x.ID == guild.ID);
            if (index == -1)
            {
                Logger.Log("WARN: Attempted to update a guild that did not exist, creating instead");
                _CreateGuild(guild);
                return;
            }
            _Guilds[index] = guild;
            Logger.Log($"Updated guild -> {guild.ID} ({guild.Name})");
        }

        private void _DeleteGuild(DiscordPacket.GuildDelete guildDelete)
        {
            if (!_Guilds.Any(x => x.ID == guildDelete.ID))
            {
                Logger.Log("WARN: Attempted to remove guild that did not exist");
                return;
            }
            _Guilds.RemoveAll(x => x.ID == guildDelete.ID);
            Logger.Log($"Deleted guild -> {guildDelete.ID}");
        }

        private DiscordPacket _DecodePacket(string packet)
        {
            var decoded = JsonConvert.DeserializeObject<DiscordPacket>(packet);
            if (decoded == null)
                return null;

            switch (decoded.Type)
            {
                case "READY":
                    decoded.Data = JsonConvert.DeserializeObject<DiscordPacket.Ready>(decoded.Data.ToString());
                    break;
                case "GUILD_CREATE":
                case "GUILD_UPDATE":
                    decoded.Data = JsonConvert.DeserializeObject<DiscordPacket.Guild>(decoded.Data.ToString());
                    break;
                case "GUILD_DELETE":
                    decoded.Data = JsonConvert.DeserializeObject<DiscordPacket.GuildDelete>(decoded.Data.ToString());
                    break;
                case "MESSAGE_CREATE":
                    decoded.Data = JsonConvert.DeserializeObject<DiscordPacket.Message>(decoded.Data.ToString());
                    break;
                default:
                    Logger.Log($"Unknown packet type -> {decoded.Type}");
                    decoded.Data = null;
                    break;
            }

            return decoded;
        }

        private void _HandleReadyPacket(DiscordPacket.Ready packet)
        {
            _BotUser = packet.User;

            // heartbeat runner
            _HeartbeatThread = new Thread(() =>
            {
                while (true)
                {
                    var heartbeat = new DiscordPacket();
                    heartbeat.OPCode = 1;
                    heartbeat.Data = _LastMessageSequence;

                    try
                    {
                        _SendString(JsonConvert.SerializeObject(heartbeat));
                    }
                    catch (Exception e)
                    {
                        Logger.Log($"Exception thrown while sending heartbeat -> {e.Message}", LoggerColor.Red);
                        // crash backend to force restart
                        try { _Socket.Abort(); } catch { }
                        return;
                    }

                    Logger.Log("Discord heartbeat sent");
                    lock (_HeartbeatLock)
                    {
                        if (_HeartbeatCancelRequest)
                            return;
                        Monitor.Wait(_HeartbeatLock, TimeSpan.FromMilliseconds(packet.HeartbeatInterval));
                    }
                }
            });
            _HeartbeatThread.Start();

            // initial status update
            _SendString(JsonConvert.SerializeObject(new DiscordPacket()
            {
                OPCode = 3,
                Data = new DiscordPacket.StatusUpdate()
                {
                    Game = new DiscordPacket.Game()
                    {
                        Name = "applebot.ix.je"
                    }
                }
            }));
        }

        private void _HandleMessage(DiscordPacket.Message message)
        {
            // ignore self messages
            if (message.Author.ID == _BotUser.ID)
                return;

            var metadata = new Metadata();
            metadata.User = message.Author;
            metadata.Guild = GetGuildFromID(message.ChannelID);
            metadata.ChannelID = message.ChannelID;

            ReceiveMessage?.Invoke(this, metadata, message.Author.Username, message.Content);
        }

        private void _SendString(string message)
        {
            _SendStringRaw(message);
        }

        private void _SendStringRaw(string message)
        {
            var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
            lock (_SocketSendLock)
                _Socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None).Wait();
        }

        private string _ReceivePacket()
        {
            var packet = new List<byte>();
            var buffer = new ArraySegment<byte>(new byte[512]);

            bool endOfMessage = false;
            while (!endOfMessage)
            {
                var result = _Socket.ReceiveAsync(buffer, CancellationToken.None).Result;

                packet.AddRange(buffer.Take(result.Count));

                if (result.EndOfMessage)
                    endOfMessage = true;
            }

            return Encoding.UTF8.GetString(packet.ToArray());
        }

        private string _CreateConnectionPacket(string token)
        {
            var properties = new JObject();
            properties["$os"] = RuntimeInformation.OSDescription;
            properties["$browser"] = "ApplebotEx";
            properties["$device"] = "ApplebotEx";
            properties["$referrer"] = string.Empty;
            properties["$referrer_domain"] = string.Empty;

            var data = new JObject();
            data["token"] = token;
            data["properties"] = properties;
            data["compress"] = false;

            var packet = new DiscordPacket();
            packet.OPCode = 2;
            packet.Data = data;

            return JsonConvert.SerializeObject(packet);
        }

        public bool SendMessage(object m, string message)
        {
            var metadata = m as Metadata;
            if (metadata == null)
                return false;

            lock (_MessageQueueLock)
            {
                _MessageQueue.Enqueue(Tuple.Create(message, metadata));
                Monitor.Pulse(_MessageQueueLock);
            }

            return true;
        }

        private enum SendMessageResponse
        {
            Success,
            Failed,
            RateLimited
        }

        private Tuple<SendMessageResponse, int?> _SendMessageRaw(string locationID, string message)
        {
            var json = new JObject();
            json["content"] = message;

            var content = new StringContent(json.ToString(Formatting.None), Encoding.UTF8);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            try
            {
                using (var http = new HttpClient())
                {
                    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", Resource.Token);
                    var response = http.PostAsync($"https://discordapp.com/api/channels/{locationID}/messages", content).Result;
                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.OK:
                            return Tuple.Create(SendMessageResponse.Success, null as int?);
                        case (HttpStatusCode)429 /* too many requests */:
                            // get "Retry-After" header and return it as the second value of the tuple
                            var headers = response.Headers.Where(x => x.Key.Equals("Retry-After", StringComparison.CurrentCultureIgnoreCase));
                            if (headers.Count() == 0)
                                return Tuple.Create(SendMessageResponse.RateLimited, null as int?);
                            var retry = int.Parse(headers.First().Value.First());
                            return Tuple.Create(SendMessageResponse.RateLimited, retry as int?);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Exception thrown while sending Discord message -> {ex.Message}");
            }

            return Tuple.Create(SendMessageResponse.Failed, null as int?);
        }

        public DiscordPacket.Guild GetGuildFromID(string id)
        {
            return _Guilds.FirstOrDefault(x => x.ID == id);
        }
    }

}