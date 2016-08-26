using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApplebotEx.Modules
{
    public delegate bool TimeoutMessageHandlerOverrideDelegate(IChatMessageHost host, object metadata);

    public class TimeoutMessageHandler
    {
        public ChatMessageHostReceiveDelegate ReceiveHandler { get { return _HandleMessage; } }

        private TimeSpan _Timeout;
        private ChatMessageHostReceiveDelegate _ReceiveHandler;
        private TimeoutMessageHandlerOverrideDelegate _OverridePredicate;
        private DateTime _LastCall = DateTime.MinValue;
        private object _Lock = new object();

        public TimeoutMessageHandler(TimeSpan timeout, ChatMessageHostReceiveDelegate receiveHandler, TimeoutMessageHandlerOverrideDelegate overridePredicate)
        {
            _Timeout = timeout;
            _ReceiveHandler = receiveHandler;
            _OverridePredicate = overridePredicate;
        }

        public TimeoutMessageHandler(TimeSpan timeout, ChatMessageHostReceiveDelegate receiveHandler)
            : this(timeout, receiveHandler, BotPermissionsPredicate)
        { }

        public static bool BotPermissionsPredicate(IChatMessageHost host, object metadata)
        {
            var botperms = host as IBotPermissions;
            if (botperms == null)
                return false;

            return botperms.HasBotPermissions(metadata);
        }

        private void _HandleMessage(IChatMessageHost host, object metadata, string user, string message)
        {
            // if the timeout is in effect or the override predicate does not pass, ignore message
            lock (_Lock)
                if (!(DateTime.UtcNow - _LastCall > _Timeout || _OverridePredicate(host, metadata)))
                    return;
                else
                    _LastCall = DateTime.UtcNow;

            _ReceiveHandler(host, metadata, user, message);
        }
    }
}
