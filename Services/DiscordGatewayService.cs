using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace Applebot.Services
{
    class DiscordServiceConfiguration
    {
        [JsonRequired]
        public string OAuthToken { get; set; }
    }

    class DiscordMessage : IGatewayMessage
    {
        public DiscordGatewayService Service { get; }

        public SocketMessage SocketMessage { get; }

        public bool IsSenderAdministrator => false;

        public string Content { get; }

        public DiscordMessage(DiscordGatewayService service, SocketMessage socketMessage, string content)
        {
            Service = service;
            SocketMessage = socketMessage;
            Content = content;
        }

        public async Task RespondInChatAsync(string line, CancellationToken ct)
        {
            await SocketMessage.Channel.SendMessageAsync(line);
        }

        public async Task RespondToSenderAsync(string line, CancellationToken ct)
        {
            await SocketMessage.Channel.SendMessageAsync(line, messageReference: new MessageReference(SocketMessage.Id));
        }
    }

    class DiscordGatewayService : IGatewayService, IDisposable
    {
        public string FriendlyName => "Discord Gateway";

        public DiscordSocketClient Client { get; private set; }

        private TaskCompletionSource _ShutdownTaskSource = new TaskCompletionSource();
        public Task ShutdownTask => _ShutdownTaskSource.Task;

        // we use a channel here to invert the control flow from discord.net event handing
        // to a IAsyncEnumerable loop that our interface expects
        private Channel<DiscordMessage> _MessageChannel;

        public DiscordGatewayService()
        {
            _MessageChannel = Channel.CreateUnbounded<DiscordMessage>();
        }

        public void Dispose()
        {
            Client?.Dispose();
            _ShutdownTaskSource.SetResult();
        }

        public async Task InitializeAsync(CancellationToken ct)
        {
            var serviceConfig = await ConfigurationResolver.LoadConfigurationAsync<DiscordGatewayService, DiscordServiceConfiguration>();

            var clientConfig = new DiscordSocketConfig
            {
                AlwaysDownloadUsers = true,
                GuildSubscriptions = true,
            };
            Client = new DiscordSocketClient(clientConfig);

            // Client.Log += msg =>
            // {
            //     Console.WriteLine(msg.Message);
            //     return Task.CompletedTask;
            // };

            Client.MessageReceived += _HandleMessageReceived;
            Client.Disconnected += ex =>
            {
                _MessageChannel.Writer.Complete(ex);
                return Task.CompletedTask;
            };

            await Client.LoginAsync(TokenType.Bot, serviceConfig.OAuthToken);

            // would be cool if we could use a custom status here that was not a game but it seems
            // the discord API does not properly support this yet and the library subsequently also
            await Client.SetGameAsync("with Electric Sheep"); // *Playing* with Electric Sheep

            await Client.StartAsync();
        }

        private async Task _HandleMessageReceived(SocketMessage message)
        {
            await _MessageChannel.Writer.WriteAsync(new DiscordMessage(this, message, message.Content));
        }

        public async IAsyncEnumerable<IGatewayMessage> ExecuteGatewayAsync([EnumeratorCancellation] CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                yield return await _MessageChannel.Reader.ReadAsync(ct);
            }
        }
    }

}