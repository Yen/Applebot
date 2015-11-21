using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ApplebotAPI;
using System.Collections.Specialized;
using System.Xml;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Threading;

namespace DiscordPlatformBeta
{
    public class DiscordPlatform : Platform
    {
        NameValueCollection _loginData;
        string _loginToken;

        public DiscordPlatform()
        {
            if (!LoadConfigs())
            {
                State = PlatformState.Unready;
                return;
            }

            _loginToken = GetLoginToken();


        }

        private string GetLoginToken()
        {
            Logger.Log(Logger.Level.PLATFORM, "Attempting to recieve auth token from Discord login server");

            try
            {
                using (WebClient client = new WebClient())
                {
                    byte[] buffer = client.UploadValues("http://discordapp.com/api/auth/login", _loginData);

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

        private bool LoadConfigs()
        {
            if (!File.Exists("Settings/discordsettings.xml"))
            {
                Logger.Log(Logger.Level.ERROR, "Settings file \"{0}\" does not exist", "discordsettings.xml");
                return false;
            }
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load("Settings/discordsettings.xml");
            }
            catch
            {
                Logger.Log(Logger.Level.ERROR, "Error reading settings file \"{0}\"", "Settings/discordsettings.xml");
                return false;
            }

            var emailBuf = doc.SelectSingleNode("settings/email");
            var passBuf = doc.SelectSingleNode("settings/pass");

            if ((emailBuf == null) || (passBuf == null))
            {
                Logger.Log(Logger.Level.ERROR, "Settings file \"{0}\" is missing required values", "Settings/discordsettings.xml");
                return false;
            }

            string email = emailBuf.InnerXml;
            string pass = passBuf.InnerXml;

            _loginData = new NameValueCollection();
            _loginData.Add("email", email);
            _loginData.Add("password", pass);

            return true;
        }

        public override void Run()
        {
            throw new NotImplementedException();
        }

        public override void Send<T1>(T1 data)
        {
            throw new NotImplementedException();
        }
    }
}
