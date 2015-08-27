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
        public string Sender { get; private set; }
        public string Content { get; private set; }

        public Message(string sender, string content)
        {
            Sender = sender;
            Content = content;
        }
    }

    public abstract class Command
    {
        public string Name { get; private set; }
        public List<Regex> Expressions { get; private set; } = new List<Regex>();
        public TimeSpan Overflow { get; private set; } = TimeSpan.FromSeconds(5); // Default settings
        public bool Droppable { get; private set; } = false; //

        public Command(string name)
        {
            Name = name;
        }

        public Command(string name, TimeSpan overflow) : this(name)
        {
            Overflow = overflow;
        }

        public Command(string name, TimeSpan overflow, bool droppable) : this(name, overflow)
        {
            Droppable = droppable;
        }

        public abstract void HandleMessage<T1, T2>(T1 message, T2 platform)
            where T1 : Message
            where T2 : Platform;
    }

}
