using Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace QueueCommand
{
    public class QueueCommand : Command
    {

        public List<string[]> Queue;
        public bool isOpen;

        public QueueCommand(CommandData data) : base("Queue Command", TimeSpan.FromSeconds(5), data)
        {
            Expressions.Add(new Regex("^!join\\b"));
            Expressions.Add(new Regex("^!next\\b"));
            Expressions.Add(new Regex("^!leave\\b"));
            Expressions.Add(new Regex("^!queue\\b"));

            Queue = new List<string[]> { };
            isOpen = false;
        }

        public override void Execute(MessageArgs message)
        {
            string[] parts = message.Content.Split(' ');
            string owner = _data.Settings["channel"].ToString().Substring(1);
            string username = message.User;
            bool asOwner = false;
            if (message.User == owner) { asOwner = true; }

            if (parts[0] == "!next")
                if (asOwner)
                {
                    if (Queue.Count == 0)
                    {
                        _data.Core.WriteChatMessage("No one is in queue.", false);
                        return;
                    }

                    string[] next = Queue[0];
                    Queue.RemoveAt(0);
                    _data.Core.WriteChatMessage("You're up, " + next[0] + " (" + next[2] + ")! Please join the room.", false);
                    return;
                }
                else
                {
                    _data.Core.WriteChatMessage("The next waiting player is " + Queue.First()[0] + " (" + Queue.First()[2] + ").", false);
                    return;
                }

            if (parts[0] == "!join")
            {
                if (isOpen == false)
                {
                    _data.Core.WriteChatMessage("The queue is closed. Please wait for the broadcaster to open it!", false);
                    return;
                }
                if (parts.Length == 1)
                {
                    _data.Core.WriteChatMessage("Please specify an in-game name to join, like this: '!join QueenOfNasods'", false);
                    return;
                }
                else
                {
                    bool duplicate = false;

                    foreach (string[] item in Queue.ToList())
                    {
                        if (item[0] == username)
                        {
                            duplicate = true;
                        }
                    }
                    if (duplicate)
                    {
                        _data.Core.WriteChatMessage("You're already in the queue.", false);
                        return;
                    }

                    string[] temp = { username };
                    Queue.Add(temp.Concat(parts).ToArray());
                    _data.Core.WriteChatMessage("Added " + username + " to queue as " + parts[1] + ". You are at position " + Queue.Count() + ".", false);
                    return;
                }
            }

            if (parts[0] == "!leave")
            {

                if (asOwner && (parts.Length > 1))
                {
                    bool plsgo = false;

                    foreach (string[] item in Queue.ToList())
                    {
                        if (item[0] == parts[1])
                        {
                            Queue.Remove(item);
                            plsgo = true;
                        }
                    }

                    if (plsgo)
                    {
                        _data.Core.WriteChatMessage("User was removed from queue.", false);
                        return;
                    }
                    else
                    {
                        _data.Core.WriteChatMessage("User is not in queue.", false);
                        return;
                    }

                }


                if (Queue.Count == 0)
                {
                    _data.Core.WriteChatMessage("You're not in the queue.", false);
                    return;
                }

                bool peaceout = false;

                foreach (string[] item in Queue.ToList())
                {
                    if (item[0] == username)
                    {
                        Queue.Remove(item);
                        peaceout = true;
                    }
                }
                if (peaceout)
                {
                    _data.Core.WriteChatMessage("You've been removed from the queue.", false);
                    return;
                }
                else
                {
                    _data.Core.WriteChatMessage("You're not in the queue.", false);
                    return;
                }
            }

            if (parts[0] == "!queue")
            {
                if ((parts.Length > 1) && (asOwner = true))
                {
                    if (parts[1] == "open")
                    {
                        if (isOpen == true)
                        {
                            _data.Core.WriteChatMessage("The queue is already open.", false);
                            return;
                        }
                        else
                        {
                            isOpen = true;
                            _data.Core.WriteChatMessage("The queue is now open!", false);
                            return;
                        }
                    }
                    if (parts[1] == "close")
                    {
                        if (isOpen == false)
                        {
                            _data.Core.WriteChatMessage("The queue is already closed.", false);
                            return;
                        }
                        else
                        {
                            isOpen = true;
                            _data.Core.WriteChatMessage("The queue is now closed.", false);
                            return;
                        }
                    }
                    if (parts[1] == "clear")
                    {
                        Queue.Clear();
                        _data.Core.WriteChatMessage("Queue cleared. All players have been removed.", false);
                        return;
                    }
                }


                if (Queue.Count == 0)
                {
                    _data.Core.WriteChatMessage("No one is in queue.", false);
                    return;
                }


                string finaloutput = "Currently waiting: ";
                foreach (string[] item in Queue.ToList())
                {
                    finaloutput += item[0] + " (" + item[2] + "), ";
                }

                finaloutput = finaloutput.Remove(finaloutput.Length - 2);
                _data.Core.WriteChatMessage(finaloutput, false);
                return;
            }

        }
    }
}
