using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{

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

    [AttributeUsage(AttributeTargets.Class)]
    public class BackendRegister : Attribute
    {
        public Type BackendType { get; private set; }

        public BackendRegister(Type backendType)
        {
            BackendType = backendType;
        }
    }

    public abstract class Backend
    {
        public abstract Task Run();
    }

    public abstract class Backend<TConnectionInfo, TLoginInfo> : Backend
        where TConnectionInfo : BackendConnectionInfo
        where TLoginInfo : BackendLoginInfo
    {

        //public Backend(BotCore core)

        public abstract void Connect(TConnectionInfo connect);
        public abstract void Login(TLoginInfo login);
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
        }

        public override void Login(TwitchBackendLoginInfo login)
        {
            //Blah
        }

        public override Task Run()
        {
           while(true)
            {
                Logger.Log(Logger.Level.ERROR, "a");
                System.Threading.Thread.Sleep(1000);
            }
        }
    }

}
