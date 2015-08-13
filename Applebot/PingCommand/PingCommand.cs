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
        public PingCommand(CommandData data) : base("Ping Command", data)
        {
            Expressions.Add(new Regex("^!ping\\b"));
        }

        public override void Execute(MessageArgs message)
        {
            _data.Core.WriteChatMessage("Pong!", false);
        }
    }
}
