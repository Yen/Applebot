using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplebotAPI
{
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
