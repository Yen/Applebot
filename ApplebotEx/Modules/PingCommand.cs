using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace ApplebotEx.Modules
{
    public class PingCommand : Service
    {
        public override void ServiceAdd(IService service)
        {
            if (service is IChatMessageHost)
                (service as IChatMessageHost).ReceiveMessage += new TimeoutMessageHandler(TimeSpan.FromSeconds(5), _HandleMessage).ReceiveHandler;
        }

        private void _HandleMessage(IChatMessageHost host, object metadata, string user, string message)
        {
            if (Regex.Match(message, @"^!ping\b", RegexOptions.IgnoreCase).Success)
                host.SendMessage(metadata, $"Pong! -> {DateTime.Now.ToLocalTime()} @ {TimeZoneInfo.Local}");
        }
    }
}
