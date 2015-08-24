using ApplebotAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TwitchPlatform
{
    public class TwitchPlatform : Platform
    {
        private TcpClient _client;
        private TextReader _reader;
        private TextWriter _writer;
        private object _streamLock = new object();

        private static readonly string _host = "irc.twitch.tv";
        private static readonly int _port = 6667;

        public override void Run()
        {
            try
            {
                Logger.Log(Logger.Level.PLATFORM, "Attempting connection to Twitch socket server");

                Reconnect();

                Logger.Log(Logger.Level.PLATFORM, "Connected to Twitch server");

                string line;
                while ((line = _reader.ReadLine()) != null)
                {
                    if (line.StartsWith("PING"))
                    {
                        SendString("PONG applebot");
                        continue;
                    }

                    string[] parts = line.Split(' ');

                    if (parts[1].Equals("001"))
                    {
                        Logger.Log(Logger.Level.PLATFORM, "Logged into Twitch chat server");
                        continue;
                    }

                    if (parts[1].Equals("JOIN"))
                    {
                        Logger.Log(Logger.Level.PLATFORM, "Joined Twitch channel {0}", parts[2]);
                        continue;
                    }

                    if (parts[1].Equals("PRIVMSG"))
                    {
                        string user = parts[0].Split('!')[0].Substring(1);
                        string message = line.Substring(parts[0].Length + parts[1].Length + parts[2].Length + 4);

                        ProcessMessage(this, new TwitchMessage(message, user));
                    }
                }

                Logger.Log(Logger.Level.WARNING, "Connection to Twitch server was dropped by remote host (login error?), reconnecting");
            }
            catch (SocketException)
            {
                Logger.Log(Logger.Level.ERROR, "Connection to Twitch server was dropped, reconnecting");
            }

            Run();
        }

        private void Reconnect()
        {
            if (_client != null && _client.Connected)
                _client.Close();

            _client = new TcpClient(_host, _port);

            _reader = new StreamReader(_client.GetStream());
            _writer = new StreamWriter(_client.GetStream());

            //TODO: Config loading
            
            SendString("PASS {0}", "oauth");
            SendString("NICK {0}", "zogzer");

            SendString("JOIN {0}", "#zogzer");
        }

        public override void Send<T1>(T1 data)
        {
            Logger.Log("Send: {0}", data.Content);
        }

        private void SendString(string format, params string[] args)
        {
            lock (_streamLock)
            {
                _writer.WriteLine(string.Format(format, args));
                _writer.Flush();
            }
        }
    }
}
