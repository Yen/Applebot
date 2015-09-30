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
            try
            {
                using (StreamReader reader = new StreamReader(settingsFile))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        Expressions.Add(new Regex(line));
                    }
                }
            } catch
            {
                Logger.Log(Logger.Level.WARNING, "Blacklist file not found, deal with this");
                return;
            }
        }

        public override void HandleMessage<T1, T2>(T1 message, T2 platform)
        {
            if (!platform.CheckElevatedStatus(message))
            {
                Thread.Sleep(250);
                platform.Send(new SendData(String.Format(".timeout {0} 1", message.Sender), false, message));
            }
        }
    }
}
