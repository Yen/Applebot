using System;
using ApplebotAPI;

namespace VisualCSharpExample
{
    [PlatformRegistrar(typeof(Platform))]
    public class VisualCSharpCommand : Command
    {
        public VisualCSharpCommand() : base("Visual CSharp Command")
        { }

        public override void HandleMessage<T1, T2>(T1 message, T2 sender)
        {
            Logger.Log("Response from Visual CSharp Command");
        }
    }
}
