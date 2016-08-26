using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ApplebotEx.Modules
{
    public class Quotes : ResourceDependentService<List<Quotes.Quote>>
    {
        private Random random = new Random();

        private int lastIndex = -1;

        public class Quote
        {
            [JsonProperty("response")]
            public string Response;

            [JsonProperty("addedBy")]
            public string AddedBy;
        };

        public Quotes() : base("Resources/Quotes.json")
        { }

        public override void ServiceAdd(IService service)
        {
            if (service is IChatMessageHost)
                (service as IChatMessageHost).ReceiveMessage += _HandleMessage;
        }

        public string RandomQuote() {
            Console.WriteLine("Pulling random quote");
            int index = random.Next(Resource.Count());
            if (Resource.Count() > 1) {
                while (index == lastIndex)
                    index = random.Next(Resource.Count);
            }
            return $"(#{index + 1}) {Resource[index].Response}";
        }

        private void _HandleMessage(IChatMessageHost host, object metadata, string user, string message)
        {
            try {
            // input commands check
            if (Regex.Match(message, @"^!quote\b", RegexOptions.IgnoreCase).Success)
            {
                var permissionsHost = host as IBotPermissions;      
                bool elevated = (permissionsHost == null || !permissionsHost.HasBotPermissions(metadata)) ? false : true;

                var parts = message.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 2)
                {
                    Console.WriteLine(Resource.Count());
                    if (Resource.Count == 0) {host.SendMessage(metadata, "There are no quotes!"); return;}
                    host.SendMessage(metadata, RandomQuote());
                    return;
                }

                switch (parts[1].ToLower())
                {
                    case "reload":
                        if (!elevated) {return;}
                        lock (ResourceLock)
                        {
                            ReloadResource();
                            host.SendMessage(metadata, $"{Resource.Count} {(Resource.Count == 1 ? "quote" : "quotes")} loaded");
                        }
                        return;
                    case "add":
                        if (!elevated) {return;}
                        if (parts.Length < 3) { host.SendMessage(metadata, "Usage: !quote add <quote>"); return; }
                        lock (ResourceLock)
                        {
                            var quote = new Quote {Response = _RemoveParts(message, 2), AddedBy = user};
                            Resource.Add(quote);
                            SaveResource();

                            host.SendMessage(metadata, $"Added quote #{Resource.Count}.");
                        }
                        return;
                    case "remove":
                        if (!elevated) {return;}
                        if (parts.Length < 3) { host.SendMessage(metadata, "Usage: !quote remove <index>"); return; }
                        lock (ResourceLock)
                        {
                            int target;
                            if (int.TryParse(parts[2], out target))
                            {
                                target = target - 1;
                                if (Resource.ElementAtOrDefault(target) == null) { host.SendMessage(metadata, $"That quote doesn't exist! :v"); return; }

                                Resource.RemoveAt(target); SaveResource();
                                host.SendMessage(metadata, $"Removed quote #{target + 1}."); return;
                            }
                            else
                            {
                                host.SendMessage(metadata, "Usage: !quote remove <index>"); return;
                            }
                        }
                    case "undo":
                        if (!elevated) {return;}
                        if (Resource.Count < 1) {host.SendMessage(metadata, "No quotes to remove!"); return;}
                        lock (ResourceLock)
                        {
                            Resource.RemoveAt(Resource.Count - 1); SaveResource();
                            host.SendMessage(metadata, $"Removed last quote (#{Resource.Count + 1}).");
                        }
                        return;
                    case "count":
                        if (Resource.Count == 0) {host.SendMessage(metadata, "There are no saved quotes.");}
                        string pluralPrefix = Resource.Count == 1 ? "is" : "are";
                        string pluralSuffix = Resource.Count == 1 ? "quote" : "quotes";
                        host.SendMessage(metadata, $"There {pluralPrefix} {Resource.Count} saved {pluralSuffix}.");
                        return;
                    default:
                        string fuzzy = _RemoveParts(message, 1).ToLower(); // fuzzy search
                        foreach (var q in Resource)
                        {
                            if (q.Response.ToLower().Contains(fuzzy)) {
                                host.SendMessage(metadata, $"(#{Resource.IndexOf(q) + 1}) {q.Response}");
                                return;
                            }
                        }
                        int index; // retrieve by index
                        if (int.TryParse(parts[1], out index))
                        {
                            if (Resource.ElementAtOrDefault(index - 1) != null)
                            {
                                host.SendMessage(metadata, $"(#{index}) {Resource[index - 1].Response}");
                                break;
                            }
                        }
                        host.SendMessage(metadata, "No quotes were found matching that.");
                        return;
                }
            }
                            } catch (Exception e) {Console.WriteLine(e.ToString());}
        }

        private static string _RemoveParts(string message, uint parts)
        {
            Func<char, bool> isNotWhitespace = (char c) => !char.IsWhiteSpace(c);
            var result = message.SkipWhile(char.IsWhiteSpace);
            for (uint i = 0; i < parts; i++)
            {
                result = result
                    .SkipWhile(isNotWhitespace)
                    .SkipWhile(char.IsWhiteSpace);
            }
            return new string(result.ToArray());
        }
    }
}
