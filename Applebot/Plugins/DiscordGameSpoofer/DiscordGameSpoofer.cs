using ApplebotAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordGameSpoofer
{
    [PlatformRegistrar(typeof(DiscordPlatform.DiscordPlatform))]
    public class DiscordGameSpoofer : Command
    {
        public DiscordGameSpoofer() : base("DiscordGameSpoofer")
        {
            Expressions.Add(new Regex("^!status\\b"));
        }

        public override void HandleMessage<T1, T2>(T1 message, T2 platform)
        {
            if (!platform.CheckElevatedStatus(message)) return;

            string[] parts = message.Content.Split(' ');

            if (parts.Length == 1)
            {
                (platform as DiscordPlatform.DiscordPlatform).SetGame("");
                return;
            }

            string newstr = string.Join(" ", parts, 1, parts.Count() - 1);
            (platform as DiscordPlatform.DiscordPlatform).SetGame(newstr);
        }
    }
}