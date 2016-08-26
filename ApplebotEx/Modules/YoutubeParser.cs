using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;

namespace ApplebotEx.Modules
{
    public class YoutubeParser : ResourceDependentService<YoutubeParser.Settings>
    {
        public class Settings
        {
            [JsonProperty("api_key")]
            public string APIKey;
        }

        public class APIResponse
        {
            public class PageInfoStructure
            {
                [JsonProperty("totalResults")]
                public int TotalResults;

                [JsonProperty("resultsPerPage")]
                public int ResultsPerPage;
            }

            [JsonProperty("pageInfo")]
            public PageInfoStructure PageInfo;

            public class ItemStructure
            {
                [JsonProperty("id")]
                public string ID;

                public class SnippetStructure
                {
                    [JsonProperty("publishedAt")]
                    public string PublishedAt;

                    [JsonProperty("channelID")]
                    public string ChannelID;

                    [JsonProperty("title")]
                    public string Title;

                    [JsonProperty("description")]
                    public string Description;

                    [JsonProperty("channelTitle")]
                    public string ChannelTitle;

                    [JsonProperty("tags")]
                    public string[] Tags;
                }

                [JsonProperty("snippet")]
                public SnippetStructure Snippet;
            }

            [JsonProperty("items")]
            public ItemStructure[] Items;
        }

        private Regex _Regex = new Regex(@"((youtube\.com\/watch)|(youtu\.be\/))([^(\s)]*)");

        public YoutubeParser() : base("Resources/YoutubeParser.json")
        { }

        public override void ServiceAdd(IService service)
        {
            if (service is IChatMessageHost && !(service is DiscordBackend))
                (service as IChatMessageHost).ReceiveMessage += _HandleMessage;
        }

        private void _HandleMessage(IChatMessageHost host, object metadata, string user, string message)
        {
            var matches = _Regex.Matches(message);
            var ids = new List<string>();

            foreach (Match m in matches)
            {
                var uri = new Uri($"http://{m.Value}");

                var id = _GetVideoID(uri);
                if (id == null)
                    continue;
                if (ids.Contains(id))
                    continue;
                ids.Add(id);
            }

            foreach (var id in ids)
            {
                string title;
                string channel;

                try
                {
                    var query = new Dictionary<string, string>();
                    query.Add("part", "snippet");
                    query.Add("key", Resource.APIKey);
                    query.Add("id", id);

                    var apiUri = "https://www.googleapis.com/youtube/v3/videos";
                    var requestUri = new Uri(QueryHelpers.AddQueryString(apiUri, query));

                    var request = WebRequest.CreateHttp(requestUri);
                    using (var response = request.GetResponseAsync().Result)
                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        var json = JsonConvert.DeserializeObject<APIResponse>(reader.ReadToEnd());

                        if (json.PageInfo.TotalResults != 1)
                            continue;

                        title = json.Items.First().Snippet.Title;
                        channel = json.Items.First().Snippet.ChannelTitle;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error getting youtube video info from Google API server -> {ex.Message}");
                    continue;
                }

                host.SendMessage(metadata, $"{user} linked a video, \"{title}\" by {channel}");
            }
        }

        private static string _GetVideoID(Uri uri)
        {
            var queries = QueryHelpers.ParseQuery(uri.Query);

            if (queries.ContainsKey("v"))
                return queries["v"].First();

            var path = uri.AbsolutePath.Substring(1);
            if (path.Length != 0)
                return path;

            return null;
        }
    }
}
