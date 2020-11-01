using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Applebot
{

    interface IGatewayService : IApplebotService
    {

        /// <summary>
        /// Task completes when the gateway is shutdown.
        /// </summary>
        Task ShutdownTask { get; }

        IAsyncEnumerable<IGatewayMessage> ExecuteGatewayAsync(CancellationToken ct);

    }

}