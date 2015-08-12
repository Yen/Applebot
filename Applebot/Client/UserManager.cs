using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Client
{
    public class UserManager
    {

        public struct User
        {
            public bool IsElevated { get; private set; }

            public static User GenerateUser(bool elevated)
            {
                return new User { IsElevated = elevated };
            }
        }

        private BotSettings _settings;
        private List<string> _operators = new List<string>();
        private DateTime _lastUpdate;
        private TimeSpan _updateInterval = TimeSpan.FromSeconds(30);
        private readonly object _updateLock = new object();

        public UserManager(BotSettings settings)
        {
            _settings = settings;

            _lastUpdate = DateTime.UtcNow;

            InvokeRefresh();
        }

        public void InvokeRefresh()
        {
            Logger.Log(Logger.Level.LOG, "Updating operator list");

            lock (this)
            {
                XmlDocument data = new XmlDocument();

                using (WebClient client = new WebClient())
                {
                    byte[] buffer = client.DownloadData("http://tmi.twitch.tv/group/user/" + _settings["channel"].ToString().Substring(1).ToLower() + "/chatters");
                    if (buffer == null)
                    {
                        Logger.Log(Logger.Level.WARNING, "Error fetching tmi data, operator list update skipped");
                        return;
                    }
                    XmlDictionaryReader reader = JsonReaderWriterFactory.CreateJsonReader(buffer, XmlDictionaryReaderQuotas.Max);
                    XElement element = XElement.Load(reader);

                    data.LoadXml(element.ToString());
                }

                _operators.Clear();

                XmlNodeList moderators = data.SelectNodes("//chatters/moderators//item");

                foreach (XmlNode node in moderators)
                {
                    _operators.Add(node.InnerXml);
                }

            }
        }

        public User GetUserInfo(string user)
        {
            lock(_updateLock)
            {
                if (DateTime.UtcNow.Subtract(_updateInterval) > _lastUpdate)
                {
                    InvokeRefresh();
                    _lastUpdate = DateTime.UtcNow;
                }
            }

            string buffer = user.ToLower();

            // Host are always elevated even if twitch api is derp
            if (buffer.Equals(((string)_settings["channel"]).ToLower().Substring(1)))
                return User.GenerateUser(true);

            if (_operators.Contains(buffer))
                return User.GenerateUser(true);

            return User.GenerateUser(false);
        }

        public User this[string user]
        {
            get { return GetUserInfo(user); }
        }

    }
}
