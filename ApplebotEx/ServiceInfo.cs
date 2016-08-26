using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApplebotEx
{
    public struct ServiceInfo
    {
        public string Identifier { get; }
        public bool Running { get; }

        public ServiceInfo(string identifier, bool running)
        {
            Identifier = identifier;
            Running = running;
        }
    }
}
