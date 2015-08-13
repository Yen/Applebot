using Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace PingCommand
{
    public class PingCommand : Command
    {
        public PingCommand(BotCore core, BotSettings settings, UserManager manager) : base("Ping Command", core, settings, manager)
        {
            Expressions.Add(new Regex("^!ping\\b"));
        }

        public override void Execute(MessageArgs message)
        {
            _core.WriteChatMessage("Pong!", false);
        }
    }
}
