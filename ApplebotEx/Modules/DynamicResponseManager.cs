using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ApplebotEx.Modules
{
    public class DynamicResponseManager : ResourceDependentService<List<DynamicResponseManager.Pattern>>
    {
        public class Pattern
        {
            [JsonProperty("trigger")]
            public string Trigger;

            [JsonProperty("is_regex")]
            public bool IsRegex;

            [JsonProperty("response")]
            public string Response;
        };

        public DynamicResponseManager() : base("Resources/DynamicResponseManager.Patterns.json")
        { }

        public override void ServiceAdd(IService service)
        {
            if (service is IChatMessageHost)
                (service as IChatMessageHost).ReceiveMessage += _HandleMessage;
        }

        private void _HandleMessage(IChatMessageHost host, object metadata, string user, string message)
        {
            // input commands check
            if (Regex.Match(message, @"^!dynamic\b", RegexOptions.IgnoreCase).Success)
            {
                var permissionsHost = host as IBotPermissions;
                if (permissionsHost == null || !permissionsHost.HasBotPermissions(metadata))
                {
                    host.SendMessage(metadata, "Elevated permissions required");
                    return;
                }

                var parts = message.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 2)
                {
                    host.SendMessage(metadata, "Missing parameters");
                    return;
                }

                switch (parts[1].ToLower())
                {
                    case "reload":
                        lock (ResourceLock)
                        {
                            ReloadResource();
                            host.SendMessage(metadata, $"{Resource.Count} {(Resource.Count == 1 ? "pattern" : "patterns")} loaded");
                        }
                        return;
                    case "add_command":
                        {
                            if (parts.Length < 4)
                            {
                                host.SendMessage(metadata, "Missing parameters");
                                return;
                            }
                            var command = parts[2].StartsWith("!") ? parts[2] : $"!{parts[2]}";
                            if (_AddCommand(command, _RemoveParts(message, 3)))
                                host.SendMessage(metadata, $"Added command -> {command}");
                            else
                                host.SendMessage(metadata, $"Command already existed, overridden -> {command}");
                            return;
                        }
                    case "remove_command":
                        {
                            if (parts.Length < 3)
                            {
                                host.SendMessage(metadata, "Missing parameters");
                                return;
                            }
                            var command = parts[2].StartsWith("!") ? parts[2] : $"!{parts[2]}";
                            if (_RemoveCommand(command))
                                host.SendMessage(metadata, $"Removed command -> {command}");
                            else
                                host.SendMessage(metadata, $"Command does not exist -> {command}");
                            return;
                        }
                    case "add_regex":
                        if (parts.Length < 4)
                        {
                            host.SendMessage(metadata, "Missing parameters");
                            return;
                        }

                        // check if regex is valid
                        try
                        {
                            Regex.Match(string.Empty, parts[2]);
                        }
                        catch
                        {
                            host.SendMessage(metadata, "Error parsing regex");
                            return;
                        }

                        if (_AddRegex(parts[2], _RemoveParts(message, 3)))
                            host.SendMessage(metadata, $"Added regex -> {parts[2]}");
                        else
                            host.SendMessage(metadata, $"Regex already exists, overridden -> {parts[2]}");
                        return;
                    case "remove_regex":
                        if (parts.Length < 3)
                        {
                            host.SendMessage(metadata, "Missing parameters");
                            return;
                        }
                        if (_RemoveRegex(parts[2]))
                            host.SendMessage(metadata, $"Removed regex -> {parts[2]}");
                        else
                            host.SendMessage(metadata, $"Regex does not exist -> {parts[2]}");
                        return;
                    case "list_commands":
                        var commands = Resource.Where(x => x.IsRegex == false).Select(x => x.Trigger);
                        if (commands.Count() == 0)
                        {
                            host.SendMessage(metadata, "No commands present");
                            return;
                        }
                        var list = string.Join(" | ", commands);
                        host.SendMessage(metadata, $"Commands -> {list}");
                        return;
                    default:
                        host.SendMessage(metadata, "Unknown sub-command");
                        return;
                }
            }

            lock (ResourceLock)
            {
                foreach (var p in Resource.Where(x => x.IsRegex == false))
                    if (Regex.Match(message, $@"^{p.Trigger}\b", RegexOptions.IgnoreCase).Success)
                    {
                        host.SendMessage(metadata, p.Response);
                        break;
                    }

                foreach(var p in Resource.Where(x => x.IsRegex == true))
                    if (Regex.Match(message, p.Trigger).Success)
                        host.SendMessage(metadata, p.Response);
            }
        }

        private bool _AddCommand(string trigger, string response)
        {
            var pattern = new Pattern { Trigger = trigger, Response = response, IsRegex = false };
            lock (ResourceLock)
                if (Resource.RemoveAll(x => x.IsRegex == false && x.Trigger == trigger) == 0)
                {
                    Resource.Add(pattern);
                    SaveResource();
                    return true;
                }
                else
                {
                    Resource.Add(pattern);
                    SaveResource();
                    return false;
                }
        }

        private bool _RemoveCommand(string trigger)
        {
            lock (ResourceLock)
                if (Resource.RemoveAll(x => x.IsRegex == false && x.Trigger == trigger) != 0)
                {
                    SaveResource();
                    return true;
                }
                else
                    return false;
        }

        private bool _AddRegex(string trigger, string response)
        {
            var pattern = new Pattern { Trigger = trigger, Response = response, IsRegex = true };
            lock (ResourceLock)
                if (Resource.RemoveAll(x => x.IsRegex == true && x.Trigger == trigger) == 0)
                {
                    Resource.Add(pattern);
                    SaveResource();
                    return true;
                }
                else
                {
                    Resource.Add(pattern);
                    SaveResource();
                    return false;
                }
        }

        private bool _RemoveRegex(string trigger)
        {
            lock (ResourceLock)
                if (Resource.RemoveAll(x => x.IsRegex == true && x.Trigger == trigger) != 0)
                {
                    SaveResource();
                    return true;
                }
                else
                    return false;
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
