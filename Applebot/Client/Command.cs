using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Client
{
    public interface Command
    {
        string Name { get; }

        List<Regex> Expressions { get; }

        void Execute(string user, string message, BotCore sender, BotSettings settings);
    }
}
