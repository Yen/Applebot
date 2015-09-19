using ApplebotAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace SimpleTextCommand
{
    public class SimpleTextCommand : Command
    {
        // design stolen from bashtech's GeoBot

        private XmlNodeList _patterns;
        private string _configLocation;
        private XmlDocument _commandSettings;
        private XmlNode _rootNode;

        public SimpleTextCommand() : base("SimpleTextCommand")
        {
            Expressions.Add(new Regex("^!command\\b"));
            Expressions.Add(new Regex("^!autoreply\\b"));

            _configLocation = "settings/SimpleTextCommand.xml";

            _commandSettings = new XmlDocument();

            if (!File.Exists(_configLocation))
            {
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

            foreach (XmlNode node in _patterns)
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

                Logger.Log(Logger.Level.APPLICATION, "SimpleTextCommand: Added pattern \"" + trigger + "\"");

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

        public override void HandleMessage<T1, T2>(T1 message, T2 platform)
        {
            string[] parts = message.Content.Split(' ');

            if (parts[0] == "!command" || parts[0] == "!autoreply")
            {
                bool isComplex = (parts[0] == "!autoreply");
                string syntaxHelp = parts[0];
                bool elevated = platform.CheckElevatedStatus(message);

                if (!elevated)
                {
                    Logger.Log(Logger.Level.WARNING, "User is not owner, aborting");
                    return;
                }

                if (parts.Length < 2)
                {
                    platform.Send(new SendData("Missing parameters. :v", false, message));
                    return;
                }

                // TODO: More graceful removal of autoreplies

                if (parts[1] == "remove")
                {
                    if (parts.Length < 3)
                    {
                        platform.Send(new SendData(string.Format("Syntax: {0} remove [command]", syntaxHelp), false, message));
                        return;
                    }

                    bool replaced = RemovePattern(parts[2]);

                    if (replaced)
                    {
                        platform.Send(new SendData("Removed pattern " + parts[2] + ".", false, message));
                    }
                    else
                    {
                        platform.Send(new SendData("Pattern " + parts[2] + " doesn't exist. :v", false, message));
                    }

                    return;

                }

                if (parts[1] == "add")
                {
                    if (parts.Length < 4)
                    {
                        platform.Send(new SendData(string.Format("Syntax: {0} add [command] [response]", syntaxHelp), false, message));
                        return;
                    }

                    string response = message.Content.Substring(parts[0].Length + parts[1].Length + parts[2].Length + 3);

                    try
                    {
                        bool replaced = AddPattern(parts[2], response, isComplex);

                        if (replaced)
                        {
                            platform.Send(new SendData("Replaced pattern " + parts[2] + ".", false, message));
                        }
                        else
                        {
                            platform.Send(new SendData("Added pattern " + parts[2] + ".", false, message));
                        }

                    }
                    catch
                    {
                        platform.Send(new SendData("Invalid regex?", false, message));
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

                    if (type == "regex")
                    {
                        Regex r = new Regex(trigger);
                        if (r.IsMatch(message.Content))
                        {
                            platform.Send(new SendData(node.Attributes["response"].Value, false, message));
                        }
                    }

                    if (type == "text")
                    {
                        if (trigger == message.Content.Substring(1))
                        {
                            platform.Send(new SendData(node.Attributes["response"].Value, false, message));
                        }
                    }

                }
            }
        }

    }
}
