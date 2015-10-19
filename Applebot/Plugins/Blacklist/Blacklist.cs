using ApplebotAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Blacklist
{
    [PlatformRegistrar(typeof(TwitchPlatform.TwitchPlatform))]
    public class Blacklist : Command
    {
        string settingsFile = "settings/Blacklist.txt";
        List<string> permittedUsers = new List<string>();
        int fixedTriggers = 2;

        public Blacklist() : base("Blacklist", TimeSpan.FromSeconds(0))
        {
            Expressions.Add(new Regex("^!blacklist\\b"));
            Expressions.Add(new Regex("^!permit\\b"));
            bool success = Reload();
            if (!success)
                Logger.Log(Logger.Level.WARNING, "Blacklist couldn't be loaded! File should be in " + settingsFile);
        }

        private bool Reload()
        {
            try
            {
                using (StreamReader reader = new StreamReader(settingsFile))
                {
                    if (Expressions.Count > fixedTriggers) // this is kind of an ugly place for this to be, but leaving it outside the block is ugly from a functionality standpoint, handle later?
                        Expressions.RemoveRange(fixedTriggers, Expressions.Count - fixedTriggers);
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        Expressions.Add(new Regex(line));
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }


        public override void HandleMessage<T1, T2>(T1 message, T2 platform)
        {
            string[] parts = message.Content.Split(' ');

            if (parts[0] == "!blacklist" && parts.Length > 1 && platform.CheckElevatedStatus(message))
            {
                switch (parts[1])
                {
                    case "reload":
                        bool success = Reload();
                        platform.Send(new SendData(success ? "Blacklist was reloaded successfully." : "Blacklist couldn't be loaded. Check file path?", false, message));
                        break;
                }
            }

            if (parts[0] == "!permit" && parts.Length > 1 && platform.CheckElevatedStatus(message))
            {
                permittedUsers.Add(parts[1].ToLower());
                platform.Send(new SendData($"Permitted {parts[1]}. Their next blacklisted message will be allowed.", false, message));
            }

            // have to recheck here to handle edge case of "non-elevated user attempts to use administrative command", they get purged otherwise (not the WORST behavior but not gr9) 
            // this is kinda ugly, maybe core should pass the index of the expression that triggers the command so rechecks like this aren't necessary?
            // honestly "amalgamate" commands are kind of a mess tho

            bool triggered = false;
            foreach (var regex in Expressions.Skip(fixedTriggers))
            {
                if (regex.Match(message.Content).Success)
                    triggered = true;
            }

            if (!platform.CheckElevatedStatus(message) && triggered)
            {
                if (permittedUsers.Contains(message.Sender))
                {
                    permittedUsers.Remove(message.Sender);
                }
                else
                {

                    Thread.Sleep(250);
                    platform.Send(new SendData(String.Format(".timeout {0} 1", message.Sender), false, message));
                }

            }
        }
    }
}
