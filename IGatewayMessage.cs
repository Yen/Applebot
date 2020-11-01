using System.Threading;
using System.Threading.Tasks;

namespace Applebot
{

    interface IGatewayMessage
    {
        bool IsSenderAdministrator { get; }

        string Content { get; }
        
        Task RespondInChatAsync(string line, CancellationToken ct);
        Task RespondToSenderAsync(string line, CancellationToken ct);
    }

}