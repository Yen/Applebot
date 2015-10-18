using ApplebotAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DriveByPlugin
{
    [PlatformRegistrar(typeof(TwitchPlatform.TwitchPlatform))]
    public class DriveByPlugin : Command
    {
        List<string> users = new List<string>();
        Regex filter = new Regex(@"/^(https?:\/\/)?([\da-z\.-]+)\.([a-z\.]{2,6})([\/\w \.-]*)*\/?$/"); // URLs. won't catch broken ones, but who the hell bothers to click those anyway

        public DriveByPlugin() : base("DriveByPlugin", TimeSpan.FromSeconds(0))
        {
            Expressions.Add(new Regex(".*")); // xd
        }

        public override void HandleMessage<T1, T2>(T1 message, T2 platform)
        {
            string sender = message.Sender;
            if (!users.Contains(sender)) {
                if (filter.IsMatch(message.Content)) {
                    Thread.Sleep(250);
                    platform.Send(new SendData(String.Format(".timeout {0} 1", message.Sender), false, message));
                    Logger.Log(Logger.Level.APPLICATION, $"Purged {sender} for drive-by linking");
                }
                else
                {
                    Logger.Log(Logger.Level.APPLICATION, $"{sender} added to \"known\" list");
                }               
            }

        }
    }
}
