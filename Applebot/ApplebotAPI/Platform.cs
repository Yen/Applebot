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
            if (!PlatformType.IsSubclassOf(typeof(Platform)))
            {
                throw new ArgumentException(string.Format("Argument must be a subclass of {0}", typeof(Platform)), "platformType");
            }
        }
    }

    public abstract class Platform : ISender
    {
        public abstract void Send<T1>(T1 data)
            where T1 : SendData;
    }

    public class SendData
    {
        public string Content { get; private set; }

        public SendData(string content)
        {
            Content = content;
        }
    }

    public interface ISender
    {
        void Send<T1>(T1 data)
            where T1 : SendData;
    }
}
