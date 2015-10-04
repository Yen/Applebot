using ApplebotAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace QueueCommand
{
    public class QueueCommand : Command
    {
        public List<Tuple<string, string>> Queue = new List<Tuple<string, string>>();
        public bool isOpen = false;

        public QueueCommand() : base("QueueCommand")
        {
            Expressions.Add(new Regex("^!join\\b"));
            Expressions.Add(new Regex("^!next\\b"));
            Expressions.Add(new Regex("^!leave\\b"));
            Expressions.Add(new Regex("^!queue\\b"));
        }

        public override void HandleMessage<T1, T2>(T1 message, T2 platform)
        {
            string[] parts = message.Content.Split(' ');
            string username = message.Sender;
            bool asOwner = platform.CheckElevatedStatus(message);

            if (parts[0] == "!join")
            {
                if (!isOpen)
                {
                    platform.Send(new SendData("The queue is closed. Please wait for the broadcaster to open it.", false, message));
                    return;
                }

                if (parts.Length < 2)
                {
                    platform.Send(new SendData("Please specify a name to join with, like this: \"!join QueenOfNasods\".", false, message));
                    return;
                }

                var search = Queue.FirstOrDefault(x => x.Item1 == username);
                if (search != null)
                {
                    platform.Send(new SendData(String.Format("You're already in queue (position {0}). To leave, use \"!leave\".", Queue.IndexOf(search) + 1), false, message));
                    return;
                }
                else
                {
                    Queue.Add(new Tuple<string, string>(username, parts[1]));
                    platform.Send(new SendData(String.Format("Added {0} ({1}) to queue at position {2}.", username, parts[1], Queue.Count), false, message));
                    return;
                }
            }

            if (parts[0] == "!leave")
            {
                var search = Queue.FirstOrDefault(x => x.Item1 == username);
                if (search == null)
                {
                    platform.Send(new SendData("You're not in queue. To join, use \"!join [name]\".", false, message));
                    return;
                }
                else
                {
                    Queue.Remove(search);
                    platform.Send(new SendData("You were removed from the queue.", false, message));
                }
            }

            if (parts[0] == "!next" && asOwner)
            {
                if (Queue.Count == 0)
                {
                    platform.Send(new SendData("The queue is empty.", false, message));
                    return;
                }

                platform.Send(new SendData(String.Format("You're up, {0} ({1})! Please join the game.", Queue[0].Item1, Queue[0].Item2), false, message));
                Queue.RemoveAt(0);
            }

            if (parts[0] == "!queue")
            {
                if (parts.Length == 1)
                {
                    if (Queue.Count == 0)
                    {
                        platform.Send(new SendData("The queue is empty.", false, message));
                        return;
                    }

                    string result = "Currently waiting: ";
                    foreach (Tuple<string, string> t in Queue)
                    {
                        result += String.Format("{0} ({1}), ", t.Item1, t.Item2);
                    }
                    result = result.Remove(result.Length - 2);

                    platform.Send(new SendData(result, false, message));
                    return;
                }
               
                if (parts[1] == "open" && asOwner)
                {
                    platform.Send(new SendData(String.Format("The queue is {0} open.", isOpen ? "already" : "now"), false, message));
                    isOpen = true;
                    return;
                }

                if (parts[1] == "close" && asOwner)
                {
                    platform.Send(new SendData(String.Format("The queue is {0} closed.", isOpen ? "now" : "already"), false, message));
                    isOpen = false;
                    return;
                }

                if (parts[1] == "remove" && parts.Length == 3 && asOwner)
                {
                    var search = Queue.FirstOrDefault(x => x.Item1 == parts[2].ToLower());
                    if (search == null)
                    {
                        platform.Send(new SendData(String.Format("No such user in queue.", Queue.IndexOf(search) + 1), false, message));
                        return;
                    }
                    else
                    {
                        platform.Send(new SendData(String.Format("{0} ({1}) was removed from the queue.", search.Item1, search.Item2), false, message));
                        Queue.Remove(search);
                    }
                }
            }
        }
    }
}
