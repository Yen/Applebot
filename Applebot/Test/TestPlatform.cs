using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ApplebotAPI;

namespace Test
{
    class TestPlatform : Platform
    {
        public override void Send<T1>(T1 data)
        {
            throw new NotImplementedException();
        }
    }
}
