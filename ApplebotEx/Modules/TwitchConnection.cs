using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ApplebotEx.Modules
{
    public class TwitchConnection : Service
    {
        private enum MessagePriority
        {
            High,
            Medium,
            Low
        }

        private struct IRCMessage
        {
            public string Type;
            public string Source;
            public string Body;

            public static IRCMessage Parse(string message)
            {
                var result = new IRCMessage();

                var parts = message.Split(' ');
                if (parts[0].StartsWith(":"))
                {
                    result.Source = parts[0].Substring(1);
                    result.Type = parts[1];
                    result.Body = message.Substring(parts[0].Length + 1 + parts[1].Length + 1);
                }
                else
                {
                    result.Type = parts[0];
                    result.Body = message.Substring(parts[0].Length + 1);
                }

                return result;
            }
        }

        public delegate void ReveiceMessageDelegate(string channel, string user, string message);
        public event ReveiceMessageDelegate ReceiveMessage;

        public TwitchBackend.Settings.AccountInfo Account { get; private set; }
        public string[] Channels { get; private set; }

        private TcpClient _Client;
        private StreamWriter _Writer;
        private StreamReader _Reader;

        private Queue<DateTime> _FloodQueue = new Queue<DateTime>();
        private object _FloodLock = new object();
        private int _FloodMax = 15;
        private TimeSpan _FloodSpan = TimeSpan.FromSeconds(30);

        private List<Tuple<string, MessagePriority>> _MessageQueue = new List<Tuple<string, MessagePriority>>();
        private object _MessageLock = new object();

        public TwitchConnection(TwitchBackend.Settings.AccountInfo account, string[] channels)
        {
            Account = account;
            Channels = channels;
        }

        public override bool Initialize()
        {
            Logger.Log($"Connecting to Twitch IRC -> {Account.Username}");

            _Client = new TcpClient();
            _Client.ConnectAsync("irc.twitch.tv", 6667).Wait();

            _Writer = new StreamWriter(_Client.GetStream());
            _Reader = new StreamReader(_Client.GetStream());

            Logger.Log("Sending login credentials");

            _SendStringRaw($"PASS {Account.OAuth}");
            _SendStringRaw($"NICK {Account.Username}");

            var response = IRCMessage.Parse(_Reader.ReadLine());
            if (response.Type != "001")
            {
                Logger.Log("Server did not return expected login message");
                return false;
            }

            // runners
            new Thread(_SendRunner).Start();
            new Thread(_ReceiveRunner).Start();

            Logger.Log("Connecting to channels");
            foreach (var c in Channels)
                _SendString($"JOIN #{c}", MessagePriority.Medium);

            return true;
        }

        public void SendMessage(string channel, string message)
        {
            _SendString($"PRIVMSG #{channel} :{message}", MessagePriority.Low);
        }

        private void _SendString(string message, MessagePriority priority)
        {
            var payload = Tuple.Create(message, priority);

            lock (_MessageLock)
            {
                switch(priority)
                {
                    case MessagePriority.Low:
                        _MessageQueue.Add(payload);
                        break;
                    case MessagePriority.Medium:
                        {
                            var medIndex = _MessageQueue.FindIndex(x => x.Item2 == MessagePriority.Medium);
                            if (medIndex != -1)
                                _MessageQueue.Insert(medIndex + 1, payload);
                            else
                            {
                                var highIndex = _MessageQueue.FindIndex(x => x.Item2 == MessagePriority.High);
                                if (highIndex != -1)
                                    _MessageQueue.Insert(highIndex + 1, payload);
                                else
                                    _MessageQueue.Insert(0, payload);
                            }
                        }
                        break;
                    case MessagePriority.High:
                        {
                            var highIndex = _MessageQueue.FindIndex(x => x.Item2 == MessagePriority.High);
                            if (highIndex != -1)
                                _MessageQueue.Insert(highIndex + 1, payload);
                            else
                                _MessageQueue.Insert(0, payload);
                        }
                        break;
                }
                Monitor.Pulse(_MessageLock);
            }
        }

        private void _SendStringRaw(string message)
        {
            lock (_FloodLock)
            {
                _Writer.WriteLine(message);
                _Writer.Flush();
                _FloodQueue.Enqueue(DateTime.Now);
            }
        }

        private void _SendRunner()
        {
            lock (_FloodLock)
                while (true)
                {
                    // remove timed out entries
                    while (_FloodQueue.Any() && DateTime.Now - _FloodQueue.First() > _FloodSpan)
                        _FloodQueue.Dequeue();

                    // if the max is reached, wait a second and try again
                    if (_FloodQueue.Count >= _FloodMax)
                    {
                        Monitor.Exit(_FloodLock);
                        Thread.Sleep(TimeSpan.FromSeconds(1));
                        Monitor.Enter(_FloodLock);
                        continue;
                    }

                    // send a message if there are any or wait for lock pulse
                    Monitor.Exit(_FloodLock);
                    lock (_MessageLock)
                    {
                        if (_MessageQueue.Any())
                        {
                            _SendStringRaw(_MessageQueue.First().Item1);
                            _MessageQueue.Remove(_MessageQueue.First());
                        }
                        else
                            Monitor.Wait(_MessageLock);
                    }
                    Monitor.Enter(_FloodLock);
                }
        }

        private void _ReceiveRunner()
        {
            string line;
            while ((line = _Reader.ReadLine()) != null)
            {
                var message = IRCMessage.Parse(line);
                switch (message.Type)
                {
                    case "PING":
                        _SendString("PONG :ApplebotEx", MessagePriority.High);
                        break;
                    case "JOIN":
                        Logger.Log($"Joined channel {message.Body}");
                        break;
                    case "PRIVMSG":
                        {
                            var parts = message.Body.Split(new char[] { ' ' }, 2);
                            var user = message.Source.Split('!')[0];
                            _HandleMessage(parts[0].Substring(1), user, parts[1].Substring(1));
                        }
                        break;
                }
            }
        }

        private void _HandleMessage(string channel, string user, string message)
        {
            ReceiveMessage?.Invoke(channel, user, message);
        }
    }
}
