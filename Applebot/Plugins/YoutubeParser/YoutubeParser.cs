using ApplebotAPI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Xml;

namespace YoutubeParser
{
    [PlatformRegistrar(typeof(TwitchPlatform.TwitchPlatform))]
    public class YoutubeParser : Command
    {
        private string _apiKey;

        public YoutubeParser() : base("YoutubeParser")
        {
            if (!File.Exists("Settings/youtubeparser.xml"))
            {
                Logger.Log(Logger.Level.ERROR, "Settings file \"{0}\" does not exist", "Settings/youtubeparser.xml");
                return;
            }
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load("Settings/youtubeparser.xml");
            }
            catch
            {
                Logger.Log(Logger.Level.ERROR, "Error reading settings file \"{0}\"", "Settings/youtubeparser.xml");
                return;
            }

            var apiKey = doc.SelectSingleNode("settings/apiKey");

            if (apiKey == null)
            {
                Logger.Log(Logger.Level.ERROR, "Settings file \"{0}\" is missing required values", "Settings/youtubeparser.xml");
                return;
            }

            _apiKey = apiKey.InnerXml;

            Expressions.Add(new Regex(@"((youtube\.com\/watch\?v=)|(youtu\.be\/))"));
        }

        public override void HandleMessage<T1, T2>(T1 message, T2 platform)
        {
            Regex regex = new Regex(@"((youtube\.com\/watch)|(youtu\.be\/))([^(\s)]*)");
            var matches = regex.Matches(message.Content);

            foreach (var match in matches)
            {
                Uri uri = new Uri($"http://{(match as Match).Value}");

                string id = GetVideoID(uri);
                if (id == null)
                    continue;

                string queryString = $"part=id,snippet&id={id}&key={_apiKey}";
                HttpWebRequest request = WebRequest.CreateHttp($"https://www.googleapis.com/youtube/v3/videos?{queryString}");

                WebResponse response;
                try
                {
                    response = request.GetResponse();
                }
                catch
                {
                    Logger.Log(Logger.Level.WARNING, "Error response returned from google api servers, API key could be invalid");
                    continue;
                }
                StreamReader reader = new StreamReader(response.GetResponseStream());
                string data = reader.ReadToEnd();
                reader.Close();
                response.Close();

                JObject json;
                try
                {
                    json = JObject.Parse(data);
                }
                catch
                {
                    Logger.Log(Logger.Level.WARNING, "Error parsing youtube api data");
                    continue;
                }

                var results = GetJsonObject(json, "pageInfo", "totalResults");
                if (results == null || int.Parse(results.ToString()) != 1) continue;

                var snippet = GetJsonObject(json, "items", 0, "snippet");
                if (snippet == null) continue;

                string title = GetJsonObject(snippet, "title").ToString();
                string channel = GetJsonObject(snippet, "channelTitle").ToString().ToString();

                string result = $"{message.Sender} linked a video, \"{title}\" by {channel}";

                platform.Send(new SendData(result, false, message));
            }
        }

        private string GetVideoID(Uri uri)
        {
            var queries = HttpUtility.ParseQueryString(uri.Query);

            if (queries["v"] != null)
                return queries["v"];

            var path = uri.AbsolutePath.Substring(1);
            if (path.Length != 0)
                return path;

            return null;
        }

        private JToken GetJsonObject(JToken data, params object[] args)
        {
            JToken result = data;
            foreach (var arg in args)
            {
                result = result[arg];
                if (result == null)
                    return null;
            }
            return result;
        }
    }
}
