using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace ApplebotEx.Modules
{
    public class PingCommand : Service
    {
        private TimeoutMessageHandler _Timeout;

        public PingCommand()
        {
            _Timeout = new TimeoutMessageHandler(TimeSpan.FromSeconds(5), (IChatMessageHost host, object metadata, string user, string message) =>
            {
                host.SendMessage(metadata, $"Pong! -> {DateTime.Now.ToLocalTime()} @ {TimeZoneInfo.Local}");
            });
        }

        public override void ServiceAdd(IService service)
        {
            if (service is IChatMessageHost)
                (service as IChatMessageHost).ReceiveMessage += _HandleMessage;
        }

        private void _HandleMessage(IChatMessageHost host, object metadata, string user, string message)
        {
            if (Regex.Match(message, @"^!ping\b", RegexOptions.IgnoreCase).Success)
                _Timeout.ReceiveHandler(host, metadata, user, message);
        }
    }
}
