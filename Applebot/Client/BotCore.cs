using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    public class BotCore
    {

        private BotSettings _settings;
        private TcpClient _client;

        private TextReader _reader;
        private TextWriter _writer;
        private readonly object _writerLock = new object();
        private readonly object _elevatedWriterLock = new object();

        private Queue<TimeSpan> _seconds = new Queue<TimeSpan>();
        private readonly int _messagesPer30 = 15;

        private DateTime _startingTime = DateTime.Now;

        private MessageHandler _handler;

        public BotCore(BotSettings settings)
        {
            _settings = settings;

            if ((_settings["nick"] == null) ||
                (_settings["pass"] == null) ||
                (_settings["host"] == null) ||
                (_settings["port"] == null) ||
                (_settings["channel"] == null))
            {
                throw new ManualException("Missing required settings for bot core to run");
            }

            _handler = new MessageHandler(_settings, this);

            if (_settings["loggingMessages"] == null)
                _settings["loggingMessages"] = false;
        }

        public void Run()
        {
                        while (true)
            {
                try
                {
                    Logger.Log(Logger.Level.LOG, "Attempting connection to chat server");

                    ConnectToServer();

                    Logger.Log(Logger.Level.LOG, "Connected to chat server");

                    string line;
                    while ((line = _reader.ReadLine()) != null)
                    {
                        if (line.StartsWith("PING"))
                        {
                            new Thread(() => { WriteMessage("PONG apple", true); }).Start();
                            continue;
                        }

                        string[] parts = line.Split(' ');

                        if (parts[1].Equals("001"))
                        {
                            Logger.Log(Logger.Level.LOG, "Logged into chat server");
                            continue;
                        }

                        if (parts[1].Equals("JOIN"))
                        {
                            Logger.Log(Logger.Level.LOG, "Joined channel {0}", parts[2]);
                            continue;
                        }

                        if (parts[1].Equals("PRIVMSG") && parts[2].Equals(_settings["channel"]))
                        {
                            string user = parts[0].Split('!')[0].Substring(1);
                            string message = line.Substring(parts[0].Length + parts[1].Length + parts[2].Length + 4);

                            _handler.Execute(user, message);
                        }
                    }

                    Logger.Log(Logger.Level.WARNING, "Null data was recieved from server (login error?), reconnecting");
                }
                catch (SocketException)
                {
                    Logger.Log(Logger.Level.ERROR, "Connection to server was dropped, reconnecting");
                }
            }
        }

        private void ConnectToServer()
        {
            if (_client != null)
            {
                if (_client.Connected)
                {
                    _client.Close();
                }
            }

            _client = new TcpClient((string)_settings["host"], int.Parse((string)_settings["port"]));

            _reader = new StreamReader(_client.GetStream());
            _writer = new StreamWriter(_client.GetStream());

            WriteMessage("PASS {0}", true, _settings["pass"]);
            WriteMessage("NICK {0}", true, _settings["nick"]);

            WriteMessage("JOIN {0}", true, _settings["channel"]);
        }

        public void WriteChatMessage(string message, bool elevated, params object[] keys)
        {
            string buffer = string.Format(message, keys);

            WriteMessage("PRIVMSG {0} :{1}", elevated, _settings["channel"], buffer);

            Logger.Log(Logger.Level.SENT, message);
        }


        private void WriteMessage(string message, bool elevated, params object[] keys)
        {
            string buffer = string.Format(message, keys);

            // Ensures that elevated message will only have to wait out a single non-elevated
            // message before it is sent

            if (elevated)
            {
                lock (_elevatedWriterLock)
                {
                    WriteMessageData(buffer);
                }
            }
            else
            {
                lock (_writerLock)
                {
                    lock (_elevatedWriterLock)
                    {
                        WriteMessageData(buffer);
                    }
                }
            }
        }

        private void WriteMessageData(string message)
        {
            if (_client.Connected)
            {
                _writer.WriteLine(message);
                _writer.Flush();

                _seconds.Enqueue(DateTime.Now - _startingTime);

                while (_seconds.Count > _messagesPer30)
                {
                    while (_seconds.First() < (DateTime.Now - _startingTime) - TimeSpan.FromSeconds(30))
                    {
                        _seconds.Dequeue();
                    }
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
            }
            else
            {
                Logger.Log(Logger.Level.WARNING, "Message was to be written to server but server was disconnected");
            }
        }

        public void ReloadCommands()
        {
            _handler.LoadCommands();
        }

    }
}
