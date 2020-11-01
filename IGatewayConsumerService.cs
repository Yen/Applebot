using System.Threading;
using System.Threading.Tasks;

namespace Applebot
{

    interface IGatewayConsumerService : IApplebotService
    {
        Task ConsumeMessageAsync(IGatewayMessage message, CancellationToken ct);
    }

}