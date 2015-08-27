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
        /// <summary>
        /// A string representing the entity that sent the message
        /// </summary>
        public string Sender { get; private set; }
        /// <summary>
        /// A string representing the content of the message
        /// </summary>
        public string Content { get; private set; }

        /// <param name="sender">The name of the entity that sent the message, this can be set to null if no specific entity sent it</param>
        /// <param name="content">The formatted string that represents the message content</param>
        public Message(string sender, string content)
        {
            Sender = sender;
            Content = content;
        }
    }

    /// <summary>
    /// Base class for all command type plugins
    /// </summary>
    public abstract class Command
    {
        /// <summary>
        /// A unique name for the command
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// A list of regex expressions used to decide if the command should be activated for a message
        /// </summary>
        public List<Regex> Expressions { get; private set; } = new List<Regex>();
        /// <summary>
        /// The time the command cannot be activated (except by an elevated user) after it was last activated
        /// </summary>
        public TimeSpan Overflow { get; private set; } = TimeSpan.FromSeconds(5);

        /// <param name="name">The unique name of the command</param>
        public Command(string name)
        {
            Name = name;
        }

        /// <param name="name">The unique name of the command</param>
        /// <param name="overflow">The downtime between message activations</param>
        public Command(string name, TimeSpan overflow) : this(name)
        {
            Overflow = overflow;
        }

        public abstract void HandleMessage<T1, T2>(T1 message, T2 platform)
            where T1 : Message
            where T2 : Platform;
    }

}
