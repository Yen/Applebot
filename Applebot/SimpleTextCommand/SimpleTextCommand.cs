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

        public SimpleTextCommand()
        {
            _expressions.Add(new Regex("^!text"));

            string configLocation = "SimpleTextCommand.xml";

            XmlDocument settings = new XmlDocument();

            if (!File.Exists(configLocation))
            {
                Logger.Log(Logger.Level.WARNING, "SimpleTextCommand config not found, one will be created");
                XmlNode initRootNode = settings.CreateElement("patterns");
                settings.AppendChild(initRootNode);

                XmlNode samplePattern = settings.CreateElement("pattern");

                XmlAttribute sampleTrigger = settings.CreateAttribute("trigger");
                sampleTrigger.Value = "check";
                samplePattern.Attributes.Append(sampleTrigger);

                XmlAttribute sampleResponse = settings.CreateAttribute("response");
                sampleResponse.Value = "Responding to SimpleTextCommand \"check\".";
                samplePattern.Attributes.Append(sampleResponse);

                initRootNode.AppendChild(samplePattern);

                settings.Save(configLocation);
            }


            try
            {
                settings.Load(configLocation);
            }
            catch
            {
                Logger.Log(Logger.Level.ERROR, "Couldn't load SimpleTextCommand config file");
            }

            XmlNodeList patterns = settings.SelectNodes("/patterns/pattern");

            foreach(XmlNode node in patterns)
            {
                string trigger = node.Attributes["trigger"].Value;
                Logger.Log(Logger.Level.MESSAGE, "SimpleTextCommand: Adding pattern \"" + trigger + "\"");
                _expressions.Add(new Regex("^!" + trigger));
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
            sender.WriteChatMessage("Build test", false);
        }
    }
}
