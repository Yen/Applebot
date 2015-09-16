using ApplebotAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

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

        private string _nick;
        private string _pass;
        private string _channel;

        private Queue<TimeSpan> _seconds = new Queue<TimeSpan>();
        private DateTime _startingTime = DateTime.UtcNow;

        public TwitchPlatform()
        {
            if (!File.Exists("Settings/twitchsettings.xml"))
            {
                Logger.Log(Logger.Level.ERROR, "Settings file \"{0}\" does not exist", "twitchsettings.xml");
                State = PlatformState.Unready;
                return;
            }
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load("Settings/twitchsettings.xml");
            }
            catch
            {
                Logger.Log(Logger.Level.ERROR, "Error reading settings file \"{0}\"", "Settings/twitchsettings.xml");
                State = PlatformState.Unready;
                return;
            }

            var nick = doc.SelectSingleNode("settings/nick");
            var pass = doc.SelectSingleNode("settings/pass");
            var channel = doc.SelectSingleNode("settings/channel");

            if ((nick == null) || (pass == null) || (channel == null))
            {
                Logger.Log(Logger.Level.ERROR, "Settings file \"{0}\" is missing required values", "Settings/twitchsettings.xml");
                State = PlatformState.Unready;
                return;
            }

            _nick = nick.InnerXml.ToLower();
            _pass = pass.InnerXml;
            _channel = channel.InnerXml.ToLower();
        }

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
                        SendString("PONG applebot", true);
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

                        ProcessMessage(this, new Message(user, message));
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

            SendString(string.Format("PASS {0}", _pass), true);
            SendString(string.Format("NICK {0}", _nick), true);

            SendString(string.Format("JOIN #{0}", _channel), true);
        }

        // TODO: This gives elevated a 2:1 priority, obviously it should be 1:0, .NET threading knowledge is required

        private object _sendLock = new object();

        public override void Send<T1>(T1 data)
        {
            if (!data.Elevated)
            {
                lock (_sendLock)
                {
                    SendString(string.Format("PRIVMSG #{0} :{1}", _channel, data.Content), data.Elevated);
                    return;
                }
            }
            SendString(string.Format("PRIVMSG #{0} :{1}", _channel, data.Content), data.Elevated);
        }

        private void SendString(string data, bool priority)
        {
            lock (_streamLock)
            {
                // TODO: This seems to work just fine but I feel like there is a way to produce a smoother buffer
                _seconds.Enqueue(DateTime.UtcNow - _startingTime);

                while (_seconds.Count > 15)
                {
                    while (_seconds.First() < (DateTime.UtcNow - _startingTime) - TimeSpan.FromSeconds(30))
                    {
                        _seconds.Dequeue();
                    }
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }

                _writer.WriteLine(data);
                _writer.Flush();
            }
        }

        private object _tmiLock = new object();
        private string[] _tmiPrevious;
        private DateTime _tmiLastUpdate;
        private Task _tmiUpdateTask;

        public override bool CheckElevatedStatus(string sender)
        {
            lock (_tmiLock)
            {
                if ((_tmiUpdateTask == null) || _tmiUpdateTask.IsCompleted)
                    if ((_tmiLastUpdate == null) || (_tmiLastUpdate < (DateTime.UtcNow - TimeSpan.FromSeconds(30))))
                    {
                        _tmiUpdateTask = new Task(new Action(() =>
                        {
                            while (true)
                            {
                                try
                                {
                                    Logger.Log(Logger.Level.PLATFORM, "Updating Twitch moderator list");

                                    XmlDocument doc = new XmlDocument();
                                    using (WebClient client = new WebClient())
                                    {
                                        byte[] buffer = client.DownloadData("http://tmi.twitch.tv/group/user/" + _channel + "/chatters");
                                        XmlDictionaryReader reader = JsonReaderWriterFactory.CreateJsonReader(buffer, XmlDictionaryReaderQuotas.Max);
                                        XElement element = XElement.Load(reader);

                                        doc.LoadXml(element.ToString());
                                    }
                                    List<string> elevated = new List<string>();

                                    XmlNodeList moderators = doc.SelectNodes("//chatters/moderators//item");
                                    foreach (XmlNode user in moderators)
                                        elevated.Add(user.InnerXml);
                                    XmlNodeList staff = doc.SelectNodes("//chatters/staff//item");
                                    foreach (XmlNode user in staff)
                                        elevated.Add(user.InnerXml);
                                    XmlNodeList admins = doc.SelectNodes("//chatters/admins//item");
                                    foreach (XmlNode user in admins)
                                        elevated.Add(user.InnerXml);
                                    XmlNodeList globals = doc.SelectNodes("//chatters/global_mods//item");
                                    foreach (XmlNode user in globals)
                                        elevated.Add(user.InnerXml);

                                    if (elevated.Any())
                                    {
                                        _tmiPrevious = elevated.ToArray();
                                    }
                                    break;
                                }
                                catch
                                {
                                    Logger.Log(Logger.Level.ERROR, "Error fetching Twitch moderator list, retrying");
                                }
                            }
                            Logger.Log(Logger.Level.PLATFORM, "Twitch moderator list updated");
                            _tmiLastUpdate = DateTime.UtcNow;
                        }));
                        _tmiUpdateTask.Start();
                    }
            }

            if (sender == _channel)
                return true;

            string[] mods = _tmiPrevious;

            if (mods == null)
                return false;

            if (mods.Contains(sender))
                return true;

            return false;
        }
    }
}
