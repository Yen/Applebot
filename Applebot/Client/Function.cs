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

    public static class DefaultFunctions
    {

        // Just for testing

        public class PrintFunction : Function
        {
            public string Expression
            {
                get
                {
                    return "print";
                }
            }

            public string Name
            {
                get
                {
                    return "Print Function";
                }
            }

            public void Execute(BotCore core, BotSettings settings, params string[] args)
            {
                if (args.Length == 0)
                {
                    Logger.Log(Logger.Level.INPUT, "Print function must have arguments");
                    return;
                }

                StringBuilder builder = new StringBuilder();
                builder.Append(">>");
                for (var i = 0; i < args.Length; i++)
                {
                    builder.AppendFormat(" [{0}]({1})", i, args[i]);
                }

                Logger.Log(Logger.Level.INPUT, builder.ToString());
            }
        }

    }
}
