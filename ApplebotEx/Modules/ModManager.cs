using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
using Newtonsoft.Json;

namespace ApplebotEx.Modules
{
    public class ModManager : ResourceDependentService<List<ModManager.SettingInstance>>, IBotPermissions
    {
        public class SettingInstance
        {
            [JsonProperty("channel")]
            [JsonRequired]
            public string Channel = null;

            [JsonProperty("moderators")]
            [JsonRequired]
            public string[] Moderators = null;
        };

        public ModManager() : base("Resources/ModManager.json")
        { }

        public override void ServiceAdd(IService service)
        {
            if (service is TwitchBackend)
                (service as TwitchBackend).ReceiveMessage += _HandleMessage;
        }

        private void _HandleMessage(IChatMessageHost host, object metadata, string user, string message)
        {
            var twitchHost = host as TwitchBackend;
            var twitchMeta = metadata as TwitchBackend.Metadata;

            if (twitchHost == null || twitchMeta == null)
                return;

            if (!twitchHost.HasOwnerAccount(twitchMeta.Channel))
                return;

            var setting = Resource.FirstOrDefault(x => x.Channel == twitchMeta.Channel);
            if (setting == null)
                return;

            if (!setting.Moderators.Contains(user))
                return;

            if (Regex.Match(message, @"^!up\b", RegexOptions.IgnoreCase).Success)
            {
                twitchMeta.OwnerPermissions = true;
                twitchHost.SendMessage(twitchMeta, $".mod {user}");
            }
            else if (Regex.Match(message, @"^!down\b", RegexOptions.IgnoreCase).Success)
            {
                twitchMeta.OwnerPermissions = true;
                twitchHost.SendMessage(twitchMeta, $".unmod {user}");
            }
        }

        public bool HasBotPermissions(object metadata)
        {
            var m = metadata as TwitchBackend.Metadata;
            if (m == null)
                return false;

            var setting = Resource.FirstOrDefault(x => x.Channel == m.Channel);
            if (setting == null)
                return false;

            if (setting.Moderators.Contains(m.Username))
                return true;

            return false;
        }
    }
}