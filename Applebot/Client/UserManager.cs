using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public class UserManager
    {

        private BotSettings _settings;
        private List<string> _operators = new List<string>();

        public UserManager(BotSettings settings)
        {
            _settings = settings;

            InvokeRefresh();
        }

        public void InvokeRefresh()
        {
            lock(this)
            {
                // TODO: fetch user list http://tmi.twitch.tv/group/user/zogzer/chatters
            }
        }

        public bool IsUserElevated(string user)
        {
            string buffer = user.ToLower();

            // Host are always elevated even if twitch api is derp
            if (buffer.Equals(((string)_settings["channel"]).ToLower().Substring(1)))
                return true;

            if (_operators.Contains(buffer))
                return true;

            return false;
        }

    }
}
