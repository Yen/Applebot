using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplebotAPI
{
    public abstract class Platform : ISender
    {
        public abstract void Send(string data);
    }

    public interface ISender
    {
        void Send(string data);
    }
}
