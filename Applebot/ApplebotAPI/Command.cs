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
        public bool Droppable { get; private set; }

        private static readonly TimeSpan OverflowDefault = TimeSpan.FromSeconds(5);
        private static readonly bool DroppableDefault = false;

        public Command(string name, TimeSpan overflow, bool droppable)
        {
            Name = name;
            Overflow = overflow;
            Droppable = droppable;
        }

        public Command(string name, TimeSpan overflow) : this(name, OverflowDefault, DroppableDefault) { }

        public Command(string name) : this(name, OverflowDefault) { }

        public abstract void HandleMessage<T1, T2>(T1 message, T2 sender)
            where T1 : Message
            where T2 : Platform;
    }

}
