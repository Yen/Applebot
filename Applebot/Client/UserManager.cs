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

        private BotSettings _settings;
        private List<string> _operators = new List<string>();

        public UserManager(BotSettings settings)
        {
            _settings = settings;

            InvokeRefresh();
        }

        public void InvokeRefresh()
        {
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

        public bool IsUserElevated(string user)
        {
            string buffer = user.ToLower();

            // Host are always elevated even if twitch api is derp
            if (buffer.Equals(((string)_settings["channel"]).ToLower().Substring(1)))
                return true;

            if (_operators.Contains(buffer))
                return true;

            return false;
        }

    }
}
