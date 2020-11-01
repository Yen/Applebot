using System.Threading;
using System.Threading.Tasks;

namespace Applebot
{

    interface IApplebotService
    {

        string FriendlyName { get; }

        Task InitializeAsync(CancellationToken ct) => Task.CompletedTask;

    }

}