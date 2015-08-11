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

        public class Print : Function
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
                    return "Print";
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

        public class ReloadCommands : Function
        {
            public string Expression
            {
                get
                {
                    return "reload-commands";
                }
            }

            public string Name
            {
                get
                {
                    return "Reload Commands";
                }
            }

            public void Execute(BotCore core, BotSettings settings, params string[] args)
            {
                Logger.Log(Logger.Level.WARNING, "Are you sure you want to reload commands, all data within current command instances will be lost? y/n");
                char input = Console.ReadLine()[0];
                if((input == 'y') || (input == 'Y'))
                {
                    Logger.Log(Logger.Level.INPUT, "Reloading commands");
                    core.ReloadCommands();
                }
                else
                {
                    Logger.Log(Logger.Level.INPUT, "Command reloading aborted");
                }
            }
        }

    }
}
