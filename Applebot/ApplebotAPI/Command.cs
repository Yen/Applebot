using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ApplebotAPI
{
    public class Message
    {
        public string Content { get; private set; }

        public Message(string content)
        {
            Content = content;
        }
    }

    public abstract class Command
    {
        public string Name { get; private set; }
        public IEnumerable<Regex> Expressions { get; private set; } = new List<Regex>();
        public TimeSpan Overflow { get; private set; }

        public static readonly TimeSpan OverflowDefault = TimeSpan.FromSeconds(5);

        public Command(string name, TimeSpan overflow)
        {
            Name = name;
            Overflow = overflow;
        }

        public Command(string name) : this(name, OverflowDefault) { }

        public abstract void HandleMessage<T1>(T1 message) where T1 : Message;
    }
}
