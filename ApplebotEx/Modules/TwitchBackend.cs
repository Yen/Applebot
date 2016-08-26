using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ApplebotEx.Modules
{
    public class TwitchBackend : ResourceDependentService<TwitchBackend.Settings>, IChatMessageHost, IBotPermissions
    {
        public class Metadata
        {
            public string Username;
            public string Channel;
            public bool OwnerPermissions;
        }

        public class ChattersAPIResponse
        {
            [JsonProperty("chatters_count")]
            public int ChattersCount;

            public class ChattersStructure
            {
                [JsonProperty("moderators")]
                public string[] Moderators;

                [JsonProperty("staff")]
                public string[] Staff;

                [JsonProperty("admins")]
                public string[] Admins;

                [JsonProperty("global_mods")]
                public string[] GlobalMods;

                // could be large and is not used at the moment
                //[JsonProperty("viewers")]
                //public string[] Viewers;
            }

            [JsonProperty("chatters")]
            public ChattersStructure Chatters;
        }

        private class ChattersInfo
        {
            public ChattersAPIResponse APIResponse = new ChattersAPIResponse
            {
                Chatters = new ChattersAPIResponse.ChattersStructure
                {
                    Moderators = new string[0],
                    Staff = new string[0],
                    Admins = new string[0],
                    GlobalMods = new string[0]
                }
            };
            public DateTime LastChattersUpdate = DateTime.MinValue;
            public static TimeSpan ChattersUpdateTimeout = TimeSpan.FromSeconds(15);
        }

        public class Settings
        {
            public class AccountInfo
            {
                [JsonRequired]
                [JsonProperty("username")]
                public string Username = null;

                [JsonRequired]
                [JsonProperty("oauth")]
                public string OAuth = null;
            }

            [JsonRequired]
            [JsonProperty("bot_account")]
            public AccountInfo BotAccount = null;

            [JsonRequired]
            [JsonProperty("owner_accounts")]
            public AccountInfo[] OwnerAccounts = null;

            [JsonRequired]
            [JsonProperty("channels")]
            public string[] Channels = null;
        }

        public event ChatMessageHostReceiveDelegate ReceiveMessage;

        private TwitchConnectionNew _BotConnection;

        private List<Tuple<TwitchConnectionNew, string>> _OwnerConnections = new List<Tuple<TwitchConnectionNew, string>>();

        private Dictionary<string, ChattersInfo> _ChattersInfos = new Dictionary<string, ChattersInfo>();
        private object _ChattersInfosLock = new object();

        private ModManager _ModManager;
        private object _ModManagerLock = new object();

        public TwitchBackend() : base("Resources/TwitchBackend.json")
        { }

        protected override bool Bootstrap()
        {
            _BotConnection = new TwitchConnectionNew(new TwitchConnectionNew.AccountInfo
            {
                Username = Resource.BotAccount.Username,
                OAuth = Resource.BotAccount.OAuth
            }, Resource.Channels);
            if (!_InitializeConnection(_BotConnection))
                return false;

            foreach (var c in Resource.Channels)
            {
                var acc = Resource.OwnerAccounts.FirstOrDefault(x => x.Username.Equals(c, StringComparison.CurrentCultureIgnoreCase));
                if (acc != null)
                {
                    var channel = acc.Username;
                    var connection = new TwitchConnectionNew(new TwitchConnectionNew.AccountInfo
                    {
                        Username = acc.Username,
                        OAuth = acc.OAuth
                    }, new string[] { channel });

                    if (!_InitializeConnection(connection))
                        return false;

                    _OwnerConnections.Add(Tuple.Create(connection, channel));
                }
                _ChattersInfos.Add(c, new ChattersInfo());
            }

            _BotConnection.ReceiveMessage += _HandleMessage;

            return true;
        }

        public override void ServiceAdd(IService service)
        {
            if (service is ModManager)
                lock (_ModManagerLock)
                {
                    if (_ModManager != null)
                        Logger.Log($"Second {nameof(ModManager)} instance registered, ignoring");
                    else
                    {
                        Logger.Log($"{nameof(ModManager)} registered");
                        _ModManager = service as ModManager;
                    }
                }
        }

        private bool _InitializeConnection(TwitchConnectionNew connection)
        {
            try
            {
                connection.InitializeInternals(Logger, ServiceInfos);
                if (!connection.Initialize())
                {
                    Logger.Log($"Failed to initialize connection -> {connection.Account.Username}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Exception initializing connection -> {connection.Account.Username} -> {ex.Message}");
                return false;
            }

            return true;
        }

        private void _HandleMessage(IChatMessageHost host, object metadata, string user, string message)
        {
            var m = metadata as TwitchConnectionNew.Metadata;
            if (m == null)
                return;

            var invokers = ReceiveMessage.GetInvocationList().Select(x => x as ChatMessageHostReceiveDelegate);
            foreach (var i in invokers)
                new Thread(() =>
                {
                    i(this, new Metadata { Channel = m.Channel, OwnerPermissions = false, Username = user }, user, message);
                }).Start();
        }

        public bool SendMessage(object m, string message)
        {
            var metadata = (Metadata)m;
            var sendMetadata = new TwitchConnectionNew.Metadata { Channel = metadata.Channel };

            if (metadata.OwnerPermissions)
            {
                var connection = _OwnerConnections.FirstOrDefault(x => x.Item2 == metadata.Channel);
                if (connection == null)
                    return false;

                connection.Item1.SendMessage(sendMetadata, message);
                return true;
            }

            _BotConnection.SendMessage(sendMetadata, message);
            return true;
        }

        public bool HasOwnerAccount(string channel)
        {
            return _OwnerConnections.Any(x => x.Item2 == channel);
        }

        public bool HasBotPermissions(object metadata)
        {
            var m = metadata as Metadata;
            if (m == null)
                return false;

            if (m.Username == m.Channel)
                return true;

            lock (_ModManagerLock)
                if (_ModManager != null && _ModManager.HasBotPermissions(metadata))
                    return true;

            _UpdateChatters(m.Channel);

            ChattersInfo info;
            lock (_ChattersInfosLock)
                info = _ChattersInfos[m.Channel];

            if (info.APIResponse.Chatters.Admins.Contains(m.Username))
                return true;
            if (info.APIResponse.Chatters.GlobalMods.Contains(m.Username))
                return true;
            if (info.APIResponse.Chatters.Staff.Contains(m.Username))
                return true;
            if (info.APIResponse.Chatters.Moderators.Contains(m.Username))
                return true;

            return false;
        }

        private void _UpdateChatters(string channel)
        {
            lock (_ChattersInfosLock)
            {
                if (!_ChattersInfos.ContainsKey(channel))
                    return;

                var info = _ChattersInfos[channel];
                if (DateTime.UtcNow - info.LastChattersUpdate < ChattersInfo.ChattersUpdateTimeout)
                    return;

                try
                {
                    Logger.Log($"Updating chatters -> {channel}");
                    var request = WebRequest.CreateHttp($"https://tmi.twitch.tv/group/user/{channel}/chatters");
                    using (var response = request.GetResponseAsync().Result)
                    using (var reader = new StreamReader(response.GetResponseStream()))
                        _ChattersInfos[channel].APIResponse = JsonConvert.DeserializeObject<ChattersAPIResponse>(reader.ReadToEnd());
                    _ChattersInfos[channel].LastChattersUpdate = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error updating Twitch chatters -> {ex.Message}");
                }
            }
        }
    }
}
