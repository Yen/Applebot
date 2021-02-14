using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Applebot.Services
{

    class TwitchServiceConfiguration
    {
        [JsonRequired]
        public string Username { get; set; }

        [JsonRequired]
        public string OAuthToken { get; set; }

        [JsonRequired]
        public string[] Channels { get; set; }
    }

    class TwitchMessage : IGatewayMessage
    {
        public TwitchGatewayService Service { get; }

        public bool IsSenderAdministrator => false;

        public string Content { get; }

        public string Username { get; }
        public string Channel { get; }

        public TwitchMessage(TwitchGatewayService service, string channel, string username, string content)
        {
            Service = service;

            Username = username;
            Channel = channel;
            Content = content;
        }

        public async Task RespondInChatAsync(string line, CancellationToken ct)
        {
            await Service.WriteMessageAsync(Channel, line, ct);
        }

        public async Task RespondToSenderAsync(string line, CancellationToken ct)
        {
            await Service.WriteMessageAsync(Channel, $"@{Username} {line}", ct);
        }
    }

    // TODO: perhaps this should be using the new System.IO.Pipelines stuff
    class TwitchGatewayService : IGatewayService, IDisposable
    {
        public string FriendlyName => "Twitch Gateway";

        private TaskCompletionSource _ShutdownTaskSource = new TaskCompletionSource();
        public Task ShutdownTask => _ShutdownTaskSource.Task;

        private TcpClient _Connection;
        private StreamReader _ConnectionReader;
        private StreamWriter _ConnectionWriter;

        public void Dispose()
        {
            _ConnectionReader?.Dispose();
            _ConnectionWriter?.Dispose();
            _Connection?.Dispose();

            _ShutdownTaskSource.SetResult();
        }

        public async Task InitializeAsync(CancellationToken ct)
        {
            var serviceConfig = await ResourceResolver.LoadConfigurationAsync<TwitchGatewayService, TwitchServiceConfiguration>();

            _Connection = new TcpClient();
            _Connection.NoDelay = true;

            Console.WriteLine("Connecting to Twitch IRC gateway");
            await _Connection.ConnectAsync("irc.chat.twitch.tv", 6667, ct);

            _ConnectionReader = new StreamReader(_Connection.GetStream());
            _ConnectionWriter = new StreamWriter(_Connection.GetStream());
            _ConnectionWriter.AutoFlush = true;

            await _ConnectionWriter.WriteLineAsync($"PASS {serviceConfig.OAuthToken}".AsMemory(), ct);
            await _ConnectionWriter.WriteLineAsync($"NICK {serviceConfig.Username}".AsMemory(), ct);

            // TODO: create read function that utilizes cancellation token
            var responseLine = await _ConnectionReader.ReadLineAsync();
            var responseLineParts = responseLine.Split(null, 3);
            if (responseLineParts.Length < 3 || responseLineParts[1] != "001")
            {
                throw new Exception("Twitch IRC authentication failed");
            }

            Console.WriteLine("Twitch IRC connection authenticated");

            // this twitch capability gives us user member information they dont send by default.
            // there are other capabilities including ones that let us see users preferred display names
            // and chat color but this is not supported at this time
            await _ConnectionWriter.WriteLineAsync("CAP REQ :twitch.tv/membership".AsMemory(), ct);

            // join channels
            foreach (var channel in serviceConfig.Channels)
            {
                await _ConnectionWriter.WriteLineAsync($"JOIN #{channel}".AsMemory(), ct);
            }
        }

        public async IAsyncEnumerable<IGatewayMessage> ExecuteGatewayAsync([EnumeratorCancellation] CancellationToken ct)
        {
            string line;
            // TODO: create read function that utilizes cancellation token
            while ((line = await _ConnectionReader.ReadLineAsync()) != null)
            {
                var parts = line.Split(null, 3);
                if (parts[0] == "PING")
                {
                    if (parts.Length < 2)
                    {
                        throw new Exception("Twitch IRC ping is expected to have a server parameter");
                    }
                    await _ConnectionWriter.WriteLineAsync($"PONG {parts[1]}".AsMemory(), ct);
                    continue;
                }

                switch (parts[1])
                {
                    case "PRIVMSG":
                        {
                            var message = _HandleMessage(line);
                            if (message != null)
                            {
                                yield return message;
                            }
                        }
                        break;
                }
            }
        }

        private TwitchMessage _HandleMessage(string line)
        {
            var parts = line.Split(null, 4);
            if (parts.Length < 4)
            {
                return null;
            }

            var username = parts[0].Split('!', 2)[0].Substring(1);
            var channel = parts[2].Substring(1);
            var content = parts[3].Substring(1);

            return new TwitchMessage(this, channel, username, content);
        }

        public async Task WriteMessageAsync(string channel, string content, CancellationToken ct)
        {
            var escapedContent = content.Replace("\r", "").Replace("\n", "");
            await _ConnectionWriter.WriteLineAsync($"PRIVMSG #{channel} :{escapedContent}".AsMemory(), ct);
        }
    }

}