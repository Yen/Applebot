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

        private string _user;
        private string _message;
        private BotSettings _settings;
        private BotCore _sender;

        public MessageHandler(string user, string message, BotSettings settings, BotCore sender)
        {
            _message = message;
            _user = user;
            _settings = settings;
            _sender = sender;
        }

        public void Execute()
        {
            if ((bool)_settings["loggingMessages"])
                Logger.Log(Logger.Level.MESSAGE, "{0}: {1}", _user, _message);
        }

    }
}
