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
        private List<Regex> _expressions = new List<Regex>();

        public PingCommand()
        {
            _expressions.Add(new Regex("^!ping\\b"));
        }

        public List<Regex> Expressions
        {
            get
            {
                return _expressions;
            }
        }

        public string Name
        {
            get
            {
                return "Ping Command";
            }
        }

        public void Execute(string user, string message, BotCore sender, BotSettings settings, UserManager manager)
        {
            sender.WriteChatMessage("Pong!", false);
        }
    }
}
