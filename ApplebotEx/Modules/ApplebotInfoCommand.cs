using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace ApplebotEx.Modules
{
    public class ApplebotInfoCommand : Service
    {
        private DateTime _StartupTime;

        public override bool Initialize()
        {
            _StartupTime = DateTime.UtcNow;
            return true;
        }

        public override void ServiceAdd(IService service)
        {
            if (service is IChatMessageHost)
                (service as IChatMessageHost).ReceiveMessage += _HandleMessage;
        }

        private void _HandleMessage(IChatMessageHost host, object metadata, string user, string message)
        {
            if (Regex.Match(message, @"^!applebot_info\b", RegexOptions.IgnoreCase).Success)
            {
                var permissions = host as IBotPermissions;
                if (permissions == null || !permissions.HasBotPermissions(metadata))
                    return;
            
                if (host is DiscordBackend)
                {
                    var system = $"System:\n\t{RuntimeInformation.OSDescription}\n\t{RuntimeInformation.OSArchitecture}";
                    var uptime = $"Uptime:\n\t{DateTime.UtcNow - _StartupTime}\n\tSince {_StartupTime.ToLocalTime()} {TimeZoneInfo.Local.Id}";
                    var services = "Services:";
                    foreach (var s in ServiceInfos)
                        services += $"\n\t {(s.Running ? "✅" : "❎")} {s.Identifier}";

                    host.SendMessage(metadata, $"```\n{system}\n{uptime}\n{services}```");
                }
                else
                    host.SendMessage(metadata, $"System -> {RuntimeInformation.OSDescription} | Uptime -> {DateTime.UtcNow - _StartupTime}");
            }
        }
    }
}
