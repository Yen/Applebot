using Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Xml;
using System.IO;

namespace SimpleTextCommand
{
    public class SimpleTextCommand : Command
    {
        private List<Regex> _expressions = new List<Regex>();
        private XmlNodeList _patterns;
        private string _configLocation;
        private XmlDocument _settings;
        private XmlNode _rootNode;

        public SimpleTextCommand()
        {
            _expressions.Add(new Regex("^!text\\b"));

            _configLocation = "SimpleTextCommand.xml";

            _settings = new XmlDocument();

            if (!File.Exists(_configLocation)) {
                Logger.Log(Logger.Level.WARNING, "SimpleTextCommand config not found, one will be created");
                _rootNode = _settings.CreateElement("patterns");
                _settings.AppendChild(_rootNode);

                XmlNode samplePattern = _settings.CreateElement("pattern");

                XmlAttribute sampleTrigger = _settings.CreateAttribute("trigger");
                sampleTrigger.Value = "check";
                samplePattern.Attributes.Append(sampleTrigger);

                XmlAttribute sampleResponse = _settings.CreateAttribute("response");
                sampleResponse.Value = "Responding to SimpleTextCommand \"check\".";
                samplePattern.Attributes.Append(sampleResponse);

                _rootNode.AppendChild(samplePattern);

                _settings.Save(_configLocation);
            }

            try
            {
                _settings.Load(_configLocation);
            }
            catch
            {
                Logger.Log(Logger.Level.ERROR, "Couldn't load SimpleTextCommand config file");
            }

            _rootNode = _settings.FirstChild;
            _patterns = _settings.SelectNodes("/patterns/pattern");

            foreach(XmlNode node in _patterns)
            {
                string trigger = node.Attributes["trigger"].Value;
                Logger.Log(Logger.Level.LOG, "SimpleTextCommand: Adding pattern \"" + trigger + "\"");
                _expressions.Add(new Regex("^!" + trigger + "\\b"));
            }

        }

        public List<Regex> Expressions
        {
            get
            {
                return _expressions;
            }
        }

        public string Name
        {
            get
            {
                return "Simple Text Command";
            }
        }

        private void UpdateXml()
        {
            _patterns = _settings.SelectNodes("/patterns/pattern");
            _settings.Save(_configLocation);
        }

        private bool RemovePattern(string trigger)
        {
            bool replaced = false;

            XmlNodeList existingMatches = _settings.SelectNodes("/patterns/pattern[@trigger='" + trigger + "']");
            if (existingMatches.Count > 0)
            {
                foreach (XmlNode node in existingMatches)
                {
                    _rootNode.RemoveChild(node);
                }
                replaced = true;
            }

            UpdateXml();
            return replaced;
        }

        private bool AddPattern(string trigger, string response)
        {
            lock (_settings)
            {
                bool replaced = RemovePattern(trigger);


                XmlNode samplePattern = _settings.CreateElement("pattern");

                XmlAttribute sampleTrigger = _settings.CreateAttribute("trigger");
                sampleTrigger.Value = trigger;
                samplePattern.Attributes.Append(sampleTrigger);

                XmlAttribute sampleResponse = _settings.CreateAttribute("response");
                sampleResponse.Value = response;
                samplePattern.Attributes.Append(sampleResponse);

                _rootNode.AppendChild(samplePattern);

                UpdateXml();

                if (!replaced)
                {
                    _expressions.Add(new Regex("^!" + trigger + "\\b"));
                }

                return replaced;
            }
            
        }

        public void Execute(string user, string message, BotCore sender, BotSettings settings)
        {
            string[] parts = message.Split(' ');

            if (parts[0] == "!text")
            {
                string owner = settings["channel"].ToString().Substring(1);
                Logger.Log(Logger.Level.MESSAGE, "owner is {0}", owner);

                if (user != owner)
                {
                    Logger.Log(Logger.Level.WARNING, "User is not owner, aborting");
                    return;
                }

                if (parts.Length < 2)
                {
                    sender.WriteChatMessage("Missing parameters. :v", false);
                    return;
                }

                if (parts[1] == "remove")
                {
                    if (parts.Length < 3)
                    {
                        sender.WriteChatMessage("Syntax: !text remove [command]", false);
                        return;
                    }

                    bool replaced = RemovePattern(parts[2]);

                    if (replaced)
                    {
                        sender.WriteChatMessage("Removed pattern " + parts[2] + ".", false);
                    }
                    else
                    {
                        sender.WriteChatMessage("Pattern " + parts[2] + " doesn't exist. :v", false);
                    }

                    return;

                }

                if (parts[1] == "add")
                {
                    if (parts.Length < 4)
                    {
                        sender.WriteChatMessage("Syntax: !text add [command] [response]", false);
                        return;
                    }

                    string response = message.Substring(parts[0].Length + parts[1].Length + parts[2].Length + 3);
                    Logger.Log(Logger.Level.MESSAGE, "response is " + response);
                    bool replaced = AddPattern(parts[2], response);

                    if (replaced)
                    {
                        sender.WriteChatMessage("Replaced pattern " + parts[2] + ".", false);
                    }
                    else
                    {
                        sender.WriteChatMessage("Added pattern " + parts[2] + ".", false);
                    }

                    return;
                }
                
            }

            lock (_patterns)
            {
                foreach (XmlNode node in _patterns)
                {
                    string trigger = node.Attributes["trigger"].Value;
                    if (trigger == message.Substring(1))
                    {
                        sender.WriteChatMessage(node.Attributes["response"].Value, false);
                    }
                }
            }

        }
    }
}
