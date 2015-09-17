using ApplebotAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace UptimeCommand
{
    [PlatformRegistrar(typeof(TwitchPlatform.TwitchPlatform))]

    public class UptimeCommand : Command
    {

        public UptimeCommand() : base("UptimeCommand")
        {
            Expressions.Add(new Regex("^!uptime\\b"));
        }

        public override void HandleMessage<T1, T2>(T1 message, T2 platform)
        {
            //TODO: highlight tracking?

            string[] parts = message.Content.Split(' ');
            string owner = (platform as TwitchPlatform.TwitchPlatform).Channel;
            //owner = "outspire";

            string rawData = new WebClient().DownloadString("https://api.twitch.tv/kraken/streams/" + owner);

            XmlDocument parsedData = new XmlDocument();

            using (var reader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(rawData), XmlDictionaryReaderQuotas.Max))
            {
                XElement xml = XElement.Load(reader);
                parsedData.LoadXml(xml.ToString());
            }

            XmlNode bufferNode = parsedData.SelectSingleNode("//stream/created_at");

            if (bufferNode == null)
            {
                Logger.Log(Logger.Level.WARNING, "API returned null stream node (stream offline?)");
                platform.Send(new SendData("Error retrieving stream info. :v", false));
                return;
            }

            string upSince = bufferNode.InnerText;

            DateTime dt = Convert.ToDateTime(upSince);
            TimeSpan ts = DateTime.Now.Subtract(dt);

            double numMinutes = ts.TotalMinutes;

            string hours = Math.Floor(numMinutes / 60).ToString();
            string minutes = Math.Floor(numMinutes % 60).ToString();
            string seconds = Math.Floor(numMinutes % 60).ToString();

            string output = String.Format("Live for {0} {1}, {2} {3}.", hours, hours == "1" ? "hour" : "hours", minutes, minutes == "1" ? "minute" : "minutes");
            platform.Send(new SendData(output, false));
        }
    }
}
