using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ApplebotAPI;

namespace Test
{
    [PlatformRegistrar(typeof(TestPlatform))]
    public class TestCommand : Command
    {
        public TestCommand() : base("TestCommand")
        { }

        public override void HandleMessage<T1, T2>(T1 message, T2 sender)
        {
            Logger.Log("[{0}]: {1}", "Unknown", message.Content);
        }

        public void HandleMessage<T1>(TwitchMessage message, T1 sender)
            where T1 : ISender
        {
            Logger.Log("[{0}]: {1}", message.User, message.Content);
        }

        public void HandleMessage<T1>(T1 message, TestPlatform sender)
            where T1 : Message
        {
            Logger.Log("[{0}]: {1}", "aaa", message.Content);
        }
    }
}
