using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApplebotEx.Modules
{
    public delegate void ChatMessageHostReceiveDelegate(IChatMessageHost host, object metadata, string user, string message);

    public interface IChatMessageHost
    {
        event ChatMessageHostReceiveDelegate ReceiveMessage;

        bool SendMessage(object metadata, string message);
    }
}
