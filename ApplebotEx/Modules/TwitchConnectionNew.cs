using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ApplebotEx.Modules
{
    public class TwitchConnectionNew : Service, IChatMessageHost
    {
        public class AccountInfo
        {
            [JsonProperty("username")]
            [JsonRequired]
            public string Username;

            [JsonProperty("oauth")]
            [JsonRequired]
            public string OAuth;
        }

        public class Metadata
        {
            public string Channel;
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

        private enum MessagePriority
        {
            High,
            Medium,
            Low
        }

        public event ChatMessageHostReceiveDelegate ReceiveMessage;

        public AccountInfo Account { get; private set; }
        public string[] Channels { get; private set; }

        private TcpClient _Client;
        private StreamReader _Reader;
        private StreamWriter _Writer;

        private Queue<DateTime> _FloodQueue = new Queue<DateTime>();
        private object _FloodLock = new object();
        private int _FloodMax = 15;
        private TimeSpan _FloodSpan = TimeSpan.FromSeconds(30);

        private List<Tuple<string, MessagePriority>> _MessageQueue = new List<Tuple<string, MessagePriority>>();
        private object _MessageLock = new object();

        private Thread _SendRunnerThread;
        private bool _SendRunnerCancelRequest;

        public TwitchConnectionNew(AccountInfo account, string[] channels)
        {
            Account = account;
            Channels = channels;
        }

        public override bool Initialize()
        {
            // restart runner
            new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        _Reconnect();
                        _StartRunners();
                        _ReceiveRunner();
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Unhandled exception, restarting connection -> {ex.Message}", LoggerColor.Red);
                    }
                    finally
                    {
                        _StopRunners();
                        Thread.Sleep(TimeSpan.FromSeconds(5));
                    }
                }
            }).Start();

            return true;
        }

        private void _Reconnect()
        {
            Logger.Log($"Connecting to Twitch IRC -> {Account.Username}");

            if (_Client != null)
                _Client.Dispose();
            _Client = new TcpClient();

            _Client.ConnectAsync("irc.twitch.tv", 6667).Wait();

            if (_Reader != null)
                _Reader.Dispose();
            _Reader = new StreamReader(_Client.GetStream());

            if (_Writer != null)
                _Writer.Dispose();
            _Writer = new StreamWriter(_Client.GetStream());

            Logger.Log("Sending login credentials");

            _SendStringRaw($"PASS {Account.OAuth}");
            _SendStringRaw($"NICK {Account.Username}");

            var response = IRCMessage.Parse(_Reader.ReadLine());
            if (response.Type != "001")
                throw new Exception("Server did not return expected login message, login failed?");

            Logger.Log("Connecting to channels");
            foreach (var c in Channels)
                _SendString($"JOIN #{c}", MessagePriority.Medium);
        }

        private void _StopRunners()
        {
            _SendRunnerCancelRequest = true;
            lock (_MessageLock)
                Monitor.Pulse(_MessageLock);

            Logger.Log($"Waiting for {nameof(_SendRunnerThread)} to end");
            _SendRunnerThread.Join();
        }

        private void _StartRunners()
        {
            _SendRunnerCancelRequest = false;
            _SendRunnerThread = new Thread(_SendRunner);

            Logger.Log($"Starting {nameof(_SendRunnerThread)}");
            _SendRunnerThread.Start();
        }

        private void _SendString(string message, MessagePriority priority)
        {
            var payload = Tuple.Create(message, priority);

            lock (_MessageLock)
            {
                switch (priority)
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
                _FloodQueue.Enqueue(DateTime.UtcNow);
            }
        }

        private void _SendRunner()
        {
            lock (_FloodLock)
                while (true)
                {
                    // stop if cancel requested
                    if (_SendRunnerCancelRequest)
                        break;

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
                            var channel = parts[0].Substring(1);
                            var user = message.Source.Split('!')[0];
                            ReceiveMessage?.Invoke(this, new Metadata { Channel = channel }, user, parts[1].Substring(1));
                        }
                        break;
                }
            }
        }

        public bool SendMessage(object metadata, string message)
        {
            var m = metadata as Metadata;
            if (m == null)
                return false;

            _SendString($"PRIVMSG #{m.Channel} :{message}", MessagePriority.Low);
            return true;
        }
    }
}
