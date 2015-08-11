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

        public SimpleTextCommand()
        {
            _expressions.Add(new Regex("^!text"));

            _configLocation = "SimpleTextCommand.xml";

            _settings = new XmlDocument();

            if (!File.Exists(_configLocation)) {
                Logger.Log(Logger.Level.WARNING, "SimpleTextCommand config not found, one will be created");
                XmlNode initRootNode = _settings.CreateElement("patterns");
                _settings.AppendChild(initRootNode);

                XmlNode samplePattern = _settings.CreateElement("pattern");

                XmlAttribute sampleTrigger = _settings.CreateAttribute("trigger");
                sampleTrigger.Value = "check";
                samplePattern.Attributes.Append(sampleTrigger);

                XmlAttribute sampleResponse = _settings.CreateAttribute("response");
                sampleResponse.Value = "Responding to SimpleTextCommand \"check\".";
                samplePattern.Attributes.Append(sampleResponse);

                initRootNode.AppendChild(samplePattern);

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

            XmlNodeList patterns = _settings.SelectNodes("/patterns/pattern");
            _patterns = patterns;

            foreach(XmlNode node in patterns)
            {
                string trigger = node.Attributes["trigger"].Value;
                Logger.Log(Logger.Level.LOG, "SimpleTextCommand: Adding pattern \"" + trigger + "\"");
                _expressions.Add(new Regex("^!" + trigger + "\b"));
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

                sender.WriteChatMessage("Administrative command placeholder", false);
                return;
            }

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
