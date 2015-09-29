using ApplebotAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace ShortURLFilter
{
    [PlatformRegistrar(typeof(TwitchPlatform.TwitchPlatform))]
    public class ShortURLFilter : Command
    {

        string filter = @".*(bit\.ly|tinyurl\.com|clck\.ru|goo\.gl|x.co|j\.mp|ow\.ly).*";

        public ShortURLFilter() : base("ShortURLFilter", TimeSpan.Zero)
        {
            Expressions.Add(new Regex(filter));
        }

        public override void HandleMessage<T1, T2>(T1 message, T2 platform)
        {
            if (!platform.CheckElevatedStatus(message))
                platform.Send(new SendData(String.Format(".timeout {0} 1", message.Sender), false, message));
        }
    }
}
