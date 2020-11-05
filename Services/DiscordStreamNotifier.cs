using System;
using System.Threading;
using System.Threading.Tasks;
using Applebot.Services;

namespace Applebot.Services
{

    class DiscordStreamNotifier : IBackgroundService
    {
        public string FriendlyName => "Discord Stream Notifier";

        public async Task ExecuteBackgroundAsync(GatewayServiceResolver gatewayServiceResolver, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                var discordGateway = gatewayServiceResolver.TryGetGateway<DiscordGatewayService>();
                if (discordGateway == null)
                {
                    await Task.Delay(1000, ct);
                    continue;
                }

                Task GuildMemberUpdatedAsync(Discord.WebSocket.SocketGuildUser oldMember, Discord.WebSocket.SocketGuildUser newMember)
                {
                    var userStartedStreaming = newMember.Activity?.Type == Discord.ActivityType.Streaming
                        && oldMember.Activity?.Type != Discord.ActivityType.Streaming;

                    if (userStartedStreaming)
                    {
                        Console.WriteLine($"User {newMember.Username} started streaming");
                    }

                    return Task.CompletedTask;
                }

                discordGateway.Client.GuildMemberUpdated += GuildMemberUpdatedAsync;
                try
                {
                    // we wait forever or until the gateway shutsdown, idea here being that if
                    // we cancel we cancel, but if the gateway shutsdown it will loop back and
                    // hook up the event listener again.
                    await Task.WhenAny(
                        discordGateway.ShutdownTask,
                        Task.Delay(Timeout.Infinite, ct)
                    );
                }
                finally
                {
                    discordGateway.Client.GuildMemberUpdated -= GuildMemberUpdatedAsync;
                }
            }
        }
    }
}