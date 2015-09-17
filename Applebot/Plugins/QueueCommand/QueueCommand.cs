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

        public List<string[]> Queue;
        public bool isOpen;

        public QueueCommand() : base("QueueCommand")
        {
            Expressions.Add(new Regex("^!join\\b"));
            Expressions.Add(new Regex("^!next\\b"));
            Expressions.Add(new Regex("^!leave\\b"));
            Expressions.Add(new Regex("^!queue\\b"));

            Queue = new List<string[]> { };
            isOpen = false;
        }

        public override void HandleMessage<T1, T2>(T1 message, T2 platform)
        {
            string[] parts = message.Content.Split(' ');
            string username = message.Sender;
            bool asOwner = platform.CheckElevatedStatus(message.Sender);

            if (parts[0] == "!next")
                if (asOwner)
                {
                    if (Queue.Count == 0)
                    {
                        platform.Send(new SendData("No one is in queue.", false));
                        return;
                    }

                    string[] next = Queue[0];
                    Queue.RemoveAt(0);
                    platform.Send(new SendData("You're up, " + next[0] + " (" + next[2] + ")! Please join the room.", false));
                    return;
                }
                else
                {
                    platform.Send(new SendData("The next waiting player is " + Queue.First()[0] + " (" + Queue.First()[2] + ").", false));
                    return;
                }

            if (parts[0] == "!join")
            {
                if (isOpen == false)
                {
                    platform.Send(new SendData("The queue is closed. Please wait for the broadcaster to open it!", false));
                    return;
                }
                if (parts.Length == 1)
                {
                    platform.Send(new SendData("Please specify an in-game name to join, like this: '!join QueenOfNasods'", false));
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
                        platform.Send(new SendData("You're already in the queue.", false));
                        return;
                    }

                    string response = message.Content.Substring(parts[0].Length + 1);

                    string[] temp = { username };
                    Queue.Add(temp.Concat(parts).ToArray());
                    platform.Send(new SendData("Added " + username + " to queue as " + response + ". You are at position " + Queue.Count() + ".", false));
                    return;
                }
            }

            if (parts[0] == "!leave")
            {


                if (Queue.Count == 0)
                {
                    platform.Send(new SendData("You're not in the queue.", false));
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
                    platform.Send(new SendData("You've been removed from the queue.", false));
                    return;
                }
                else
                {
                    platform.Send(new SendData("You're not in the queue.", false));
                    return;
                }
            }

            if (parts[0] == "!queue")
            {
                if ((parts.Length > 1) && (asOwner == true))
                {
                    if (parts[1] == "remove" && parts.Length > 2)
                    {
                        bool plsgo = false;

                        string target = parts[2].ToLower();

                        foreach (string[] item in Queue.ToList())
                        {
                            if (item[0] == target)
                            {
                                Queue.Remove(item);
                                plsgo = true;
                            }
                        }

                        if (plsgo)
                        {
                            platform.Send(new SendData("User was removed from queue.", false));
                            return;
                        }
                        else
                        {
                            platform.Send(new SendData("User is not in queue.", false));
                            return;
                        }
                    }


                    if (parts[1] == "open")
                    {
                        if (isOpen == true)
                        {
                            platform.Send(new SendData("The queue is already open.", false));
                            return;
                        }
                        else
                        {
                            isOpen = true;
                            platform.Send(new SendData("The queue is now open! Join with \"!join [name]\".", false));
                            return;
                        }
                    }
                    if (parts[1] == "close")
                    {
                        if (isOpen == false)
                        {
                            platform.Send(new SendData("The queue is already closed.", false));
                            return;
                        }
                        else
                        {
                            isOpen = true;
                            platform.Send(new SendData("The queue is now closed.", false));
                            return;
                        }
                    }
                    if (parts[1] == "clear")
                    {
                        Queue.Clear();
                        platform.Send(new SendData("Queue cleared. All players have been removed.", false));
                        return;
                    }
                }


                if (Queue.Count == 0)
                {
                    platform.Send(new SendData("No one is in queue.", false));
                    return;
                }


                string finaloutput = "Currently waiting: ";
                foreach (string[] item in Queue.ToList())
                {
                    string response = string.Join(" ", item).Substring(item[0].Length + item[1].Length + 2);
                    finaloutput += item[0] + " (" + response + "), ";
                }



                finaloutput = finaloutput.Remove(finaloutput.Length - 2);
                platform.Send(new SendData(finaloutput, false));
                return;
            }

        }
    }
}
