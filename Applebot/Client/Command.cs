using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Client
{
    public struct MessageArgs
    {
        public string User { get; private set; }
        public string Content { get; private set; }

        public static MessageArgs GenerateArgs(string user, string content)
        {
            return new MessageArgs { User = user, Content = content };
        }
    }

    public abstract class Command
    {
        public string Name { get; private set; }
        public List<Regex> Expressions { get; private set; }

        protected BotCore _core;
        protected BotSettings _settings;
        protected UserManager _manager;

        public Command(string name, BotCore core, BotSettings settings, UserManager manager)
        {
            Name = name;

            _core = core;
            _settings = settings;
            _manager = manager;

            Expressions = new List<Regex>();
        }

        public abstract void Execute(MessageArgs message);
    }
}
