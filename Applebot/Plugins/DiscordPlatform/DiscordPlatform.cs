using ApplebotAPI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace DiscordPlatform
{
    public class DiscordPlatform : Platform
    {

        ClientWebSocket _socket;
        NameValueCollection _loginData;

        public DiscordPlatform()
        {
            if (!File.Exists("Settings/discordsettings.xml"))
            {
                Logger.Log(Logger.Level.ERROR, "Settings file \"{0}\" does not exist", "discordsettings.xml");
                State = PlatformState.Unready;
                return;
            }
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load("Settings/discordsettings.xml");
            }
            catch
            {
                Logger.Log(Logger.Level.ERROR, "Error reading settings file \"{0}\"", "Settings/discordsettings.xml");
                State = PlatformState.Unready;
                return;
            }

            var emailBuf = doc.SelectSingleNode("settings/email");
            var passBuf = doc.SelectSingleNode("settings/pass");

            if ((emailBuf == null) || (passBuf == null))
            {
                Logger.Log(Logger.Level.ERROR, "Settings file \"{0}\" is missing required values", "Settings/discordsettings.xml");
                State = PlatformState.Unready;
                return;
            }

            string email = emailBuf.InnerXml;
            string pass = passBuf.InnerXml;

            _loginData = new NameValueCollection();
            _loginData.Add("email", email);
            _loginData.Add("password", pass);
        }

        private string GetLoginToken()
        {
            Logger.Log(Logger.Level.PLATFORM, "Attempting to recieve auth token from Discord login server");

            try
            {
                using (WebClient client = new WebClient())
                {
                    byte[] buffer = client.UploadValues("https://discordapp.com/api/auth/login", _loginData);

                    JToken json = JToken.Parse(Encoding.UTF8.GetString(buffer));
                    return json["token"].ToString();
                }
            }
            catch
            {
                Logger.Log(Logger.Level.ERROR, "Error getting Discord auth token, retrying");
                Thread.Sleep(TimeSpan.FromSeconds(1)); //Just stops spam if the server is offline or something
                return GetLoginToken();
            }
        }

        public override void Run()
        {
            string token = GetLoginToken();

            _socket = new ClientWebSocket();

            Logger.Log(Logger.Level.PLATFORM, "Attempting connection to Discord websocket hub server");

            _socket.ConnectAsync(new Uri("wss://discordapp.com/hub"), CancellationToken.None).Wait();
        }

        public override void Send<T1>(T1 data)
        {

        }
    }
}
