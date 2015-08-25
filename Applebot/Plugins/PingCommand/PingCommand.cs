using ApplebotAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PingCommand
{
    [PlatformRegistrar(typeof(Platform))]
    public class PingCommand : Command
    {
        public PingCommand() : base("PingCommand")
        { }

        public override void HandleMessage<T1, T2>(T1 message, T2 sender)
        {
            Logger.Log("Ping command thing!");
        }
    }
}
