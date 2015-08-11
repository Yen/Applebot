using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    class ManualException : Exception
    {
        public ManualException() : base()
        { }

        public ManualException(string message) : base(message)
        { }

        public ManualException(string message, params object[] keys) : base(string.Format(message, keys))
        { }

        public ManualException(string message, Exception inner) : base(message, inner)
        { }

    }

    class Program
    {

        static void Main(string[] args)
        {
            Console.WriteLine("Applebot");

            try
            {
                Logger.Log(Logger.Level.LOG, "Loading settings");
                BotSettings settings = new BotSettings("settings.xml", "usersettings.xml");

                settings["loggingMessages"] = true;

                Logger.Log(Logger.Level.LOG, "Initialising user manager");
                UserManager manager = new UserManager(settings);

                Logger.Log(Logger.Level.LOG, "Initialising bot core");
                BotCore core = new BotCore(settings, manager);

                Logger.Log(Logger.Level.LOG, "Initialising input handler");
                InputHandler input = new InputHandler(core, settings);
                new Thread(input.Run).Start();

                Logger.Log(Logger.Level.LOG, "Running bot core loop");
                core.Run();
            }
            catch (ManualException e)
            {
                Logger.Log(Logger.Level.EXCEPTION, "Program was excaped due to manual exception being thrown ({0})", e.Message);
            }

            Console.WriteLine("Program ended");
            Console.ReadKey();
        }

    }
}
