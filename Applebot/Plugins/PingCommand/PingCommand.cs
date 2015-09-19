using ApplebotAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PingCommand
{
    public class PingCommand : Command
    {
        public PingCommand() : base("PingCommand")
        {
            Expressions.Add(new Regex("(?i)^!ping\\b"));
        }

        public override void HandleMessage<T1, T2>(T1 message, T2 platform)
        {
            platform.Send(new SendData(string.Format("Pong! {0} ({1}) {2}", DateTime.Now.ToString(), TimeZoneInfo.Local.DisplayName, platform.CheckElevatedStatus(message)), false, message));
        }
    }
}
