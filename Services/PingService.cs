using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Applebot.Services
{
    class PingService : IGatewayConsumerService
    {
        public string FriendlyName => "Ping Pong";

        public async Task ConsumeMessageAsync(IGatewayMessage message, CancellationToken ct)
        {
            var parts = message.Content.Split(null, 2);
            if (parts.Length >= 1 && parts[0] == "!ping")
            {
                var localTime = DateTimeOffset.Now;

                var timeMessage = localTime.Hour switch
                {
                    < 4 or >= 20 => "I can see the stars clearly in the sky...",
                    < 6 => "The stars are dimming and the sun is near...",
                    >= 18 => "It's getting darker and the sun is dropping...",
                    _ => "The sun is high in the sky, I can see for miles..."
                };

                var fullMessage = string.Format(
                    CultureInfo.InvariantCulture,
                    "Pong! {0} I'd say it's about {1:HH:mm:ss} on {1:MMMM dd} from where I am!",
                    timeMessage,
                    localTime);

                await message.RespondToSenderAsync(fullMessage, ct);
            }

            if (parts.Length >= 1 && parts[0] == "!dead")
            {
                // throw new Exception("Yikes!");
            }
        }
    }
}