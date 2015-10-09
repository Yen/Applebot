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

        public Blacklist() : base("Blacklist")
        {
            Expressions.Add(new Regex("^!blacklist\\b"));
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
                    if (Expressions.Count > 1) // this is kind of an ugly place for this to be, but leaving it outside the block is ugly from a functionality standpoint, handle later?
                        Expressions.RemoveRange(1, Expressions.Count - 1);
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



            if (!platform.CheckElevatedStatus(message))
            {
                Thread.Sleep(250);
                platform.Send(new SendData(String.Format(".timeout {0} 1", message.Sender), false, message));
            }
        }
    }
}
