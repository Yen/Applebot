using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ApplebotAPI;

namespace Test
{
    [PlatformRegistrar(typeof(Lmao))]
    public class TestCommand : Command
    {
        public TestCommand() : base("TestCommand")
        { }

        public override void HandleMessage<T1, T2>(T1 message, T2 sender)
        {
            Logger.Log("[{0}]: {1}", "Test", message.Content);
        }
    }
}
