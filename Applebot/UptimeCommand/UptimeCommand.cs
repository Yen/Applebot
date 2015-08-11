using Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Net;
using System.Xml;
using System.Runtime.Serialization.Json;
using System.Xml.Linq;

namespace UptimeCommand
{
    public class UptimeCommand : Command
    {
        private List<Regex> _expressions = new List<Regex>();

        public UptimeCommand()
        {
            _expressions.Add(new Regex("^!uptime\\b"));
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
                return "Uptime Command";
            }
        }

        public void Execute(string user, string message, BotCore sender, BotSettings settings)
        {
            //TODO: highlight tracking?

            try
            {
                string[] parts = message.Split(' ');
                string owner = settings["channel"].ToString().Substring(1);
                //owner = "witwix";

                string rawData = new WebClient().DownloadString("https://api.twitch.tv/kraken/streams/" + owner);
                Console.WriteLine("retrieved");

                XmlDocument parsedData = new XmlDocument();

                using (var reader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(rawData), XmlDictionaryReaderQuotas.Max))
                {
                    XElement xml = XElement.Load(reader);
                    parsedData.LoadXml(xml.ToString());
                }

                XmlNode bufferNode = parsedData.SelectSingleNode("//stream/created_at");
                string upSince = bufferNode.InnerText;

                DateTime dt = Convert.ToDateTime(upSince);
                TimeSpan ts = DateTime.Now.Subtract(dt);

                double numMinutes = ts.TotalMinutes;

                string hours = Math.Floor(numMinutes / 60).ToString();
                string minutes = Math.Floor(numMinutes % 60).ToString();
                string seconds = Math.Floor(numMinutes % 60).ToString();

                string output = String.Format("Live for {0} {1}, {2} {3}.", hours, hours == "1" ? "hour" : "hours", minutes, minutes == "1" ? "minute" : "minutes");
                sender.WriteChatMessage(output, false);
                return;
            }
            catch (Exception ex)
            {
                Logger.Log(Logger.Level.ERROR, ex.ToString());
            }

            sender.WriteChatMessage("Couldn't retrieve data. :v", false); 

        }
    }
}
