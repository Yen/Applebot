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

        public enum MessageType
        {
            STANDARD,
            PING
        }

        private string _user;
        private string _message;
        private BotSettings _settings;
        private BotCore _sender;
        private MessageType _type;

        public MessageHandler(string user, string message, BotSettings settings, BotCore sender, MessageType type)
        {
            _message = message;
            _user = user;
            _settings = settings;
            _sender = sender;
            _type = type;
        }

        public void Execute()
        {
            if (_type == MessageType.PING)
            {
                _sender.WriteMessage("PONG apple", true);
                return;
            }

            if ((bool)_settings["loggingMessages"])
                Logger.Log(Logger.Level.MESSAGE, "{0}: {1}", _user, _message);
        }

    }
}
