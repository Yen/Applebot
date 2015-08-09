using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    class BotCore
    {

        private BotSettings _settings;
        private TcpClient _client;

        private TextReader _reader;
        private TextWriter _writer;
        private readonly object _writerLock = new object();
        private readonly object _elevatedWriterLock = new object();

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

            if (_settings["loggingMessages"] == null)
                _settings["loggingMessages"] = false;

            ConnectToServer();
            Logger.Log(Logger.Level.LOG, "Connected to chat server");

            WriteMessage("PASS {0}", true, _settings["pass"]);
            WriteMessage("NICK {0}", true, _settings["nick"]);

            WriteMessage("JOIN {0}", true, _settings["channel"]);

            string line;
            while ((line = _reader.ReadLine()) != null)
            {
                string[] parts = line.Split(' ');
                if (parts[1].Equals("PRIVMSG") && parts[2].Equals(_settings["channel"]))
                {
                    string user = parts[0].Split('!')[0].Substring(1);
                    string message = line.Substring(parts[0].Length + parts[1].Length + parts[2].Length + 4);

                    if ((bool)_settings["loggingMessages"])
                        Logger.Log(Logger.Level.MESSAGE, "{0}: {1}", user, message);
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
        }

        public void WriteMessage(string message, bool elevated, params object[] keys)
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
            }
            else
            {
                Logger.Log(Logger.Level.WARNING, "Message was to be written to server but server was disconnected");
            }
        }

    }
}
