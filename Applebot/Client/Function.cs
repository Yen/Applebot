using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public interface Function
    {
        string Name { get; }

        string Expression { get; }

        void Execute(BotCore core, BotSettings settings, params string[] args);
    }
}
