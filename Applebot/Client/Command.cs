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

        public static MessageArgs Generate(string user, string content)
        {
            return new MessageArgs { User = user, Content = content };
        }
    }

    public struct CommandData
    {
        public BotCore Core { get; private set; }
        public BotSettings Settings { get; private set; }
        public UserManager Manager { get; private set; }

        public static CommandData Generate(BotCore core, BotSettings settings, UserManager manager)
        {
            return new CommandData { Core = core, Settings = settings, Manager = manager };
        }
    }

    public abstract class Command
    {
        public string Name { get; private set; }
        public List<Regex> Expressions { get; private set; }
        public TimeSpan Overflow { get; private set; }

        protected CommandData _data { get; private set; }

        public Command(string name, TimeSpan overflow, CommandData data)
        {
            Name = name;
            Overflow = overflow;

            _data = data;

            Expressions = new List<Regex>();
        }

        public Command(string name, CommandData data) : this(name, TimeSpan.Zero, data)
        { }

        public abstract void Execute(MessageArgs message);
    }
}
