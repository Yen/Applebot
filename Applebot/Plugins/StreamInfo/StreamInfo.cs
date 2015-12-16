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

namespace StreamInfo
{
    [PlatformRegistrar(typeof(TwitchPlatform.TwitchPlatform))]
    public class StreamInfo : Command
    {

        // this plugin should not need to exist

        // dear people in charge of the twitch android and ios apps:
        // please put stream titles and selected games somewhere on the player screen
        // i am tired of being asked what game i'm playing after explicitly specifying what game i'm playing
        // also, i kind of hate you

        // -tyronesama

        public StreamInfo() : base("StreamInfo")
        {
            Expressions.Add(new Regex("^!title\\b"));
            Expressions.Add(new Regex("^!game\\b"));
        }

        public override void HandleMessage<T1, T2>(T1 message, T2 platform)
        {
            string[] parts = message.Content.Split(' ');
            string owner = (platform as TwitchPlatform.TwitchPlatform).Channel;

            //DEBUG -- saltybet is always up
            //owner = "saltybet";

            string rawData = new WebClient().DownloadString("https://api.twitch.tv/kraken/channels/" + owner);

            XmlDocument parsedData = new XmlDocument();

            using (var reader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(rawData), XmlDictionaryReaderQuotas.Max))
            {
                XElement xml = XElement.Load(reader);
                parsedData.LoadXml(xml.ToString());
            }

            XmlNode titleNode = parsedData.SelectSingleNode("//status");
            XmlNode gameNode = parsedData.SelectSingleNode("//game");

            if (titleNode == null || gameNode == null)
            {
                Logger.Log(Logger.Level.WARNING, "Couldn't get data from \"channels\" endpoint - this probably shouldn't happen");
                platform.Send(new SendData("Error retrieving stream info. :v", false, message));
                return;
            }

            string title = titleNode.InnerText;
            string game = gameNode.InnerText;

            platform.Send(new SendData($"{owner} is playing {game} - \"{title}\"", false, message));
        }
    }
}
