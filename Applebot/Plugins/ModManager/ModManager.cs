using ApplebotAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

// holy duplicated code batman
// ACHTUNG, not for release

namespace ModManager
{
    [PlatformRegistrar(typeof(TwitchPlatform.TwitchPlatform))]
    public class ModManager : Command
    {
        string settingsFile = "settings/ModManager.txt";
        List<string> mods = new List<string>();

        public ModManager() : base("ModManager")
        {
            Expressions.Add(new Regex("^!up\\b"));
            Expressions.Add(new Regex("^!down\\b"));
            Expressions.Add(new Regex("^!modmanager\\b"));
            bool success = Reload();
            if (!success)
                Logger.Log(Logger.Level.WARNING, "Mod list couldn't be loaded! File should be in " + settingsFile);
        }

        private bool Reload()
        {
            try
            {
                using (StreamReader reader = new StreamReader(settingsFile))
                {
                    mods.Clear();
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        mods.Add(line);
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
            if (parts[0] == "!modmanager" && parts.Length > 1 && platform.CheckElevatedStatus(message))
            {
                switch (parts[1])
                {
                    case "reload":
                        bool success = Reload();
                        platform.Send(new SendData(success ? "Mod list was reloaded successfully." : "Mod list couldn't be loaded. Check file path?", false, message));
                        break;
                }
            }

            if (parts[0] == "!up" && mods.Contains(message.Sender))
            {
                Logger.Log(Logger.Level.DEBUG, "up");
                platform.Send(new SendData(".mod " + message.Sender, false, message));
            }

            if (parts[0] == "!down" && mods.Contains(message.Sender))
            {
                Logger.Log(Logger.Level.DEBUG, "down");
                platform.Send(new SendData(".unmod " + message.Sender, false, message));
            }
        }
    }
}