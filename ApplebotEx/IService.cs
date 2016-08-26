using System.Collections.Generic;

namespace ApplebotEx
{
    public interface IService
    {
        void InitializeInternals(ILogger logger, IReadOnlyList<ServiceInfo> serviceInfos);

        bool Initialize();

        void ServiceAdd(IService service);
    }
}
