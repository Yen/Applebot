using System;
using System.Threading;
using System.Threading.Tasks;

namespace Applebot
{

    interface IBackgroundService : IApplebotService
    {

        Task ExecuteBackgroundAsync(GatewayServiceResolver gatewayServiceResolver, CancellationToken ct);

    }

}