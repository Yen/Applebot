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

            ConnectToServer();

            _writer.WriteLine("PASS {0}", _settings["pass"]);
            _writer.WriteLine("NICK {0}", _settings["nick"]);
            _writer.Flush();

            _writer.WriteLine("JOIN {0}", _settings["channel"]);
            _writer.Flush();

            string line;
            while ((line = _reader.ReadLine()) != null)
            {
                Logger.Log(Logger.Level.LOG, line);
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

            _client = new TcpClient(_settings["host"], int.Parse(_settings["port"]));

            _reader = new StreamReader(_client.GetStream());
            _writer = new StreamWriter(_client.GetStream());
        }

    }
}
