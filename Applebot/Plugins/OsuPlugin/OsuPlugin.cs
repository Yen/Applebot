using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ApplebotAPI;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Xml;

namespace OsuPlugin
{
    [PlatformRegistrar(typeof(TwitchPlatform.TwitchPlatform))]
    public class OsuPlugin : Command
    {
        private string _apiKey;
        private enum Modes
        {
            Standard = 0,
            Taiko = 1,
            CtB = 2,
            Mania = 3
        }

        public OsuPlugin() : base("OsuPlugin", TimeSpan.FromSeconds(0))
        {

            if (!File.Exists("Settings/osuplugin.xml"))
            {
                Logger.Log(Logger.Level.ERROR, "Settings file \"{0}\" does not exist", "Settings/osuplugin.xml");
                return;
            }
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load("Settings/osuplugin.xml");
            }
            catch
            {
                Logger.Log(Logger.Level.ERROR, "Error reading settings file \"{0}\"", "Settings/osuplugin.xml");
                return;
            }

            var apiKey = doc.SelectSingleNode("settings/apiKey");

            if (apiKey == null)
            {
                Logger.Log(Logger.Level.ERROR, "Settings file \"{0}\" is missing required values", "Settings/osuplugin.xml");
                return;
            }

            _apiKey = apiKey.InnerXml;

            Expressions.Add(new Regex(@"osu\.ppy\.sh\/(s|b)\/"));
        }

        public override void HandleMessage<T1, T2>(T1 message, T2 platform)
        {
            var matches = new Regex(@"osu\.ppy\.sh\/(s|b)\/([0-9+]*)").Matches(message.Content);
            foreach (var match in matches)
            {
                var parts = (match as Match).Value.Split('/');

                HttpWebRequest request = WebRequest.CreateHttp($"http://osu.ppy.sh/api/get_beatmaps?k={_apiKey}&{parts[1]}={parts[2]}");

                WebResponse response;
                try
                {
                    response = request.GetResponse();
                }
                catch
                {
                    Logger.Log(Logger.Level.WARNING, "Failed to retrieve data from osu!api server");
                    continue;
                }

                StreamReader reader = new StreamReader(response.GetResponseStream());
                string data = reader.ReadToEnd();
                reader.Close();
                response.Close();

                JArray json;
                try
                {
                    json = JArray.Parse(data);
                }
                catch
                {
                    Logger.Log(Logger.Level.WARNING, "Error parsing osu!api data");
                    continue;
                }

                if(json.Count == 0)
                {
                    Logger.Log(Logger.Level.WARNING, "Queried beatmaps were not found in osu!db");
                    continue;
                }

                var working = json[0];
                var beatmap = new
                {
                    BeatmapID = working["beatmap_id"].ToString(),
                    DifficultySize = float.Parse(working["diff_size"].ToString()),
                    DifficultyOverall = float.Parse(working["diff_overall"].ToString()),
                    DifficultyApproach = float.Parse(working["diff_approach"].ToString()),
                    DifficultyDrain = float.Parse(working["diff_drain"].ToString()),
                    Artist = working["artist"].ToString(),
                    Title = working["title"].ToString(),
                    Creator = working["creator"].ToString(),
                    BPM = int.Parse(working["bpm"].ToString()),
                    Source = working["source"].ToString(),
                    DifficultyRating = Math.Round(float.Parse(working["difficultyrating"].ToString()), 2),
                    Mode = int.Parse(working["mode"].ToString()),
                    Version = working["version"].ToString()
                };

                string readableMode = Enum.GetName(typeof(Modes), beatmap.Mode);

                switch (parts[1][0])
                {
                    case 's':
                        platform.Send(new SendData($"{message.Sender} linked \"{beatmap.Artist} - {beatmap.Title}\" by {beatmap.Creator}", false, message));
                        break;
                    case 'b':
                        string post = (readableMode == "Mania") ? $"Mania [{beatmap.DifficultySize}K]" : $"{readableMode}";
                        platform.Send(new SendData($"{message.Sender} linked \"{beatmap.Artist} - {beatmap.Title} [{beatmap.Version}]\" by {beatmap.Creator} - {post}, {beatmap.DifficultyRating}★", false, message));
                        break;
                }
            }
        }
    }
}
