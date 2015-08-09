using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Client
{
    class BotSettings
    {

        private Dictionary<string, string> _settings = new Dictionary<string, string>();

        public BotSettings(params string[] configs)
        {
            foreach (string config in configs)
            {
                XmlDocument doc = new XmlDocument();
                try
                {
                    doc.Load(config);
                }
                catch
                {
                    throw new ManualException("Failed to load config file ({0})", config);
                }

                XmlNodeList root = doc.DocumentElement.SelectNodes("setting");

                foreach (XmlNode node in root)
                {
                    string key = node["key"].InnerText;
                    string value = node["value"].InnerText;

                    if ((key == null) || (value == null))
                    {
                        throw new ManualException("Wrong format data in config file ({0})", config);
                    }

                    _settings[key] = value;
                }

            }
        }

        public string this[string key]
        {
            get
            {
                if (_settings.ContainsKey(key))
                    return _settings[key];
                return null;
            }
        }


    }
}
