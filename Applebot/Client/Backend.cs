using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{

    public enum BackendConnectionState
    {
        Connected,
        Closed
    }

    public enum BackendUsageState
    {
        Ready,
        Unavailable
    }

    public class BackendConnectionInfo
    {
        public string Host { get; private set; }
        public int Port { get; private set; }

        public BackendConnectionInfo(string host, int port)
        {
            Host = host;
            Port = port;
        }
    }

    public class BackendLoginInfo
    {
        public string Nick { get; private set; }
        public string Password { get; private set; }

        public BackendLoginInfo(string nick, string password)
        {
            Nick = nick;
            Password = password;
        }
    }

    public abstract class Backend<TConnectionInfo, TLoginInfo>
        where TConnectionInfo : BackendConnectionInfo
        where TLoginInfo : BackendLoginInfo
    {
        public BackendConnectionState ConnectionState { get; protected set; }
        public BackendUsageState UsageState { get; protected set; }

        //public Backend(BotCore core)
        public Backend()
        {
            ConnectionState = BackendConnectionState.Closed;
            UsageState = BackendUsageState.Unavailable;
        }

        public abstract void Connect(TConnectionInfo connect);
        public abstract void Login(TLoginInfo login);
        public abstract Task Run();
    }

    // Example

    public class TwitchBackendLoginInfo : BackendLoginInfo
    {
        public string Channel { get; private set; }

        TwitchBackendLoginInfo(string nick, string password, string channel) : base(nick, password)
        {
            Channel = channel;
        }
    }

    public class TwitchBackend : Backend<BackendConnectionInfo, TwitchBackendLoginInfo>
    {

        public override void Connect(BackendConnectionInfo connect)
        {
            //Blah

            ConnectionState = BackendConnectionState.Connected;
        }

        public override void Login(TwitchBackendLoginInfo login)
        {
            //Blah

            UsageState = BackendUsageState.Ready;
        }

        public override Task Run()
        {
            throw new NotImplementedException();
        }
    }

}
