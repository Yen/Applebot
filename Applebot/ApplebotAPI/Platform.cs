using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplebotAPI
{


    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class PlatformRegistrar : Attribute
    {
        public Type PlatformType { get; private set; }

        public PlatformRegistrar(Type platformType)
        {
            PlatformType = platformType;
            if (!PlatformType.IsSubclassOf(typeof(Platform)) && !(PlatformType == typeof(Platform)))
            {
                throw new ArgumentException(string.Format("Argument must be a subclass of {0}", typeof(Platform)), "platformType");
            }
        }
    }

    public enum PlatformState
    {
        Ready,
        Unready
    }

    public abstract class Platform
    {
        public event EventHandler<Message> MessageRecieved;

        public PlatformState State { get; protected set; } = PlatformState.Ready;

        public abstract void Send<T1>(T1 data)
            where T1 : SendData;

        public abstract void Run();

        protected void ProcessMessage(Platform sender, Message message)
        {
            MessageRecieved(sender, message);
        }
    }

    public class SendData
    {
        public string Content { get; private set; }

        public SendData(string content)
        {
            Content = content;
        }
    }
}
