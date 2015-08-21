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
        private XmlNodeList _patterns;
        private string _configLocation;
        private XmlDocument _commandSettings;
        private XmlNode _rootNode;

        public SimpleTextCommand(CommandData data) : base("Simple Text Command", data)
        {
            Expressions.Add(new Regex("^!text\\b"));
            Expressions.Add(new Regex("^!match\\b"));

            _configLocation = "SimpleTextCommand.xml";

            _commandSettings = new XmlDocument();

            if (!File.Exists(_configLocation)) {
                Logger.Log(Logger.Level.WARNING, "SimpleTextCommand config not found, one will be created");
                _rootNode = _commandSettings.CreateElement("patterns");

                _commandSettings.AppendChild(_rootNode);

                _commandSettings.Save(_configLocation);
            }

            try
            {
                _commandSettings.Load(_configLocation);
            }
            catch
            {
                Logger.Log(Logger.Level.ERROR, "Couldn't load SimpleTextCommand config file");
            }

            _rootNode = _commandSettings.FirstChild;
            _patterns = _commandSettings.SelectNodes("/patterns/pattern");

            foreach(XmlNode node in _patterns)
            {
                string trigger = node.Attributes["trigger"].Value;

                if (node.Attributes["type"].Value == "text")
                {
                    Expressions.Add(new Regex("^!" + trigger + "\\b"));
                }
                else
                {
                    Expressions.Add(new Regex(trigger));
                }

                Logger.Log(Logger.Level.LOG, "SimpleTextCommand: Added pattern \"" + trigger + "\"");

            }

        }

        private void UpdateXml()
        {
            _patterns = _commandSettings.SelectNodes("/patterns/pattern");
            _commandSettings.Save(_configLocation);
        }

        private bool RemovePattern(string trigger)
        {
            bool replaced = false;
            
            lock (_commandSettings)
            {
                XmlNodeList existingMatches = _commandSettings.SelectNodes("/patterns/pattern[@trigger='" + trigger + "']");
                if (existingMatches.Count > 0)
                {
                    foreach (XmlNode node in existingMatches)
                    {
                        _rootNode.RemoveChild(node);
                    }
                    replaced = true;
                }

                UpdateXml();
            }

            return replaced;
        }

        private bool AddPattern(string trigger, string response, bool isComplex)
        {

            lock (_commandSettings)
            {
                bool replaced = RemovePattern(trigger);

                if (isComplex)
                {
                    Regex r = new Regex(trigger);
                    r.IsMatch("");
                }


                XmlNode samplePattern = _commandSettings.CreateElement("pattern");

                XmlAttribute sampleTrigger = _commandSettings.CreateAttribute("trigger");
                sampleTrigger.Value = trigger;
                samplePattern.Attributes.Append(sampleTrigger);

                XmlAttribute sampleResponse = _commandSettings.CreateAttribute("response");
                sampleResponse.Value = response;
                samplePattern.Attributes.Append(sampleResponse);

                XmlAttribute sampleType = _commandSettings.CreateAttribute("type");

                if (isComplex)
                {
                    sampleType.Value = "regex";
                }
                else
                {
                    sampleType.Value = "text";
                }

                samplePattern.Attributes.Append(sampleType);

                _rootNode.AppendChild(samplePattern);

                UpdateXml();

                if (!replaced)
                {

                    if (isComplex)
                    {
                        Expressions.Add(new Regex(trigger));
                    }
                    else
                    {
                        Expressions.Add(new Regex("^!" + trigger + "\\b"));
                    }

                }

                return replaced;
            }
            
        }

        public override void Execute(MessageArgs args)
        {
            string[] parts = args.Content.Split(' ');

            if (parts[0] == "!text" || parts[0] == "!match")
            {
                bool isComplex = (parts[0] == "!match");
                string syntaxHelp = parts[0];

                string owner = _data.Settings["channel"].ToString().Substring(1);

                if (args.User != owner)
                {
                    Logger.Log(Logger.Level.WARNING, "User is not owner, aborting");
                    return;
                }

                if (parts.Length < 2)
                {
                    _data.Core.WriteChatMessage("Missing parameters. :v", false);
                    return;
                }

                if (parts[1] == "remove")
                {
                    if (parts.Length < 3)
                    {
                        _data.Core.WriteChatMessage("Syntax: {0} remove [command]", false, syntaxHelp);
                        return;
                    }

                    bool replaced = RemovePattern(parts[2]);

                    if (replaced)
                    {
                        _data.Core.WriteChatMessage("Removed pattern " + parts[2] + ".", false);
                    }
                    else
                    {
                        _data.Core.WriteChatMessage("Pattern " + parts[2] + " doesn't exist. :v", false);
                    }

                    return;

                }

                if (parts[1] == "add")
                {
                    if (parts.Length < 4)
                    {
                        _data.Core.WriteChatMessage("Syntax: {0} add [command] [response]", false, syntaxHelp);
                        return;
                    }

                    string response = args.Content.Substring(parts[0].Length + parts[1].Length + parts[2].Length + 3);

                    try
                    {
                        bool replaced = AddPattern(parts[2], response, isComplex);

                        if (replaced)
                        {
                            _data.Core.WriteChatMessage("Replaced pattern " + parts[2] + ".", false);
                        }
                        else
                        {
                            _data.Core.WriteChatMessage("Added pattern " + parts[2] + ".", false);
                        }

                    }
                    catch
                    {
                        _data.Core.WriteChatMessage("Invalid regex?", false);
                        return;
                    }

                    
                    return;
                }
                
            }

            lock (_patterns)
            {
                foreach (XmlNode node in _patterns)
                {
                    string trigger = node.Attributes["trigger"].Value;
                    string type = node.Attributes["type"].Value;

                    if (type == "regex") {
                        Regex r = new Regex(trigger);
                        if (r.IsMatch(args.Content)) {
                            _data.Core.WriteChatMessage(node.Attributes["response"].Value, false);
                        }
                    }

                    if (type == "text")
                    {
                        if (trigger == args.Content.Substring(1))
                        {
                            _data.Core.WriteChatMessage(node.Attributes["response"].Value, false);
                        }
                    }

                }
            }

        }
    }
}
