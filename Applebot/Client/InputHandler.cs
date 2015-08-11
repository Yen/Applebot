using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    class InputHandler
    {

        private BotCore _core;
        private BotSettings _settings;

        public InputHandler(BotCore core, BotSettings settings)
        {
            _core = core;
            _settings = settings;
        }

        public void Run()
        {
            while (true)
            {
                string line;
                while ((line = Console.ReadLine()) != null)
                {
                    Logger.Log(Logger.Level.INPUT, line);
                }
            }
        }

    }
}
