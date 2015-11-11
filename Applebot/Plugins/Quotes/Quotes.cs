using ApplebotAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace PingCommand
{
    [PlatformRegistrar(typeof(TwitchPlatform.TwitchPlatform))]
    public class Quotes : Command
    {
        // TODO: literally any kind of thread safety

        private List<string> quotes;
        private string configLocation = "settings/Quotes.txt";
        private Random random = new Random();
        private int lastIndex;
        private bool firstRun = true;

        public Quotes() : base("Quotes", TimeSpan.FromSeconds(30)) // fuck you
        {
            if (!File.Exists(configLocation))
            {
                Logger.Log(Logger.Level.WARNING, "Quote file not found, created at " + configLocation);
                File.Create(configLocation).Close();
            }

            try
            {
                quotes = new List<string>(File.ReadAllLines(configLocation));
            }
            catch
            {
                Logger.Log(Logger.Level.WARNING, "Couldn't open quote file (is this even possible?)");
                return;
            }

            Expressions.Add(new Regex("(?i)^!quote\\b"));
        }

        public bool UpdateQuoteFile()
        {
            lock (configLocation)
            {
                try
                {
                    File.WriteAllLines(configLocation, quotes);
                }
                catch
                {
                    Logger.Log(Logger.Level.WARNING, "Couldn't save quote file!");
                    return false;
                }
                return true;
            }
        }

        public string RandomQuote()
        {
            int index = random.Next(quotes.Count);
            if (!(firstRun == true || quotes.Count == 1))
            {
                while (index == lastIndex) // in hell, this never terminates
                {
                    index = random.Next(quotes.Count);
                }
            }

            lastIndex = index;
            firstRun = false;
            return $"(#{index + 1}) {quotes[index]}";
        }

        public override void HandleMessage<T1, T2>(T1 message, T2 platform)
        {
            lock (quotes) // there is probably a more nuanced way of doing this
            {
                string[] parts = message.Content.Split(' ');
                bool elevated = platform.CheckElevatedStatus(message);
                int index;

                if (parts.Length == 1)
                {
                    if (quotes.Count == 0) { platform.Send(new SendData("There are no quotes! :v", false, message)); return; }
                    platform.Send(new SendData(RandomQuote(), false, message));
                    return;
                }

                if (!elevated) { return; }

                switch (parts[1])
                {
                    case "add":
                        if (parts.Length < 3) { platform.Send(new SendData("Usage: !quote add <quote>", false, message)); break; }

                        string newQuote = message.Content.Substring(parts[0].Length + parts[1].Length + 2);
                        quotes.Add(newQuote); UpdateQuoteFile();
                        platform.Send(new SendData($"Added quote #{quotes.Count}.", false, message));
                        break;

                    case "remove":
                        if (parts.Length < 3) { platform.Send(new SendData("Usage: !quote remove <index>", false, message)); break; }

                        if (int.TryParse(parts[2], out index))
                        {
                            index = index - 1;
                            if (quotes.ElementAtOrDefault(index) == null) { platform.Send(new SendData($"That quote doesn't exist! :v", false, message)); break; }
                            quotes.RemoveAt(index); UpdateQuoteFile();
                            platform.Send(new SendData($"Removed quote #{index + 1}.", false, message)); break;
                        }
                        else
                        {
                            platform.Send(new SendData("Usage: !quote remove <index>", false, message)); break;
                        }
                    case "undo":
                        if (quotes.Count < 1) { platform.Send(new SendData("No quotes to remove!", false, message)); break; }

                        quotes.RemoveAt(quotes.Count - 1); UpdateQuoteFile();
                        platform.Send(new SendData($"Removed last quote (#{quotes.Count + 1}).", false, message)); break;
                    case "count":
                        if (quotes.Count == 0) { platform.Send(new SendData($"There are no saved quotes.", false, message)); break; }
                        string pluralPrefix = quotes.Count == 1 ? "is" : "are";
                        string pluralSuffix = quotes.Count == 1 ? "quote" : "quotes";
                        platform.Send(new SendData($"There {pluralPrefix} {quotes.Count} saved {pluralSuffix}.", false, message));
                        break;

                    default:
                        if (int.TryParse(parts[1], out index))
                        {
                            index = index - 1;
                            if (quotes.ElementAtOrDefault(index) != null)
                            {
                                platform.Send(new SendData($"#{index + 1}: {quotes[index]}", false, message));
                                break;
                            }
                        }
                        platform.Send(new SendData(RandomQuote(), false, message));
                        break;
                }
            }
        }
    }
}