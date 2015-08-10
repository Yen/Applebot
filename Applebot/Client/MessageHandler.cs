using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    class MessageHandler
    {

        private BotSettings _settings;
        private BotCore _sender;

        public MessageHandler(BotSettings settings, BotCore sender)
        {
            _settings = settings;
            _sender = sender;
        }

        public void Execute(string user, string message)
        {
            if ((bool)_settings["loggingMessages"])
                Logger.Log(Logger.Level.MESSAGE, "{0}: {1}", user, message);
        }

    }
}
