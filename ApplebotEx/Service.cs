using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApplebotEx
{
    public abstract class Service : IService
    {
        protected ILogger Logger;
        protected IReadOnlyList<ServiceInfo> ServiceInfos;

        public virtual bool Initialize() => true;

        public void InitializeInternals(ILogger logger, IReadOnlyList<ServiceInfo> serviceInfos)
        {
            Logger = logger;
            ServiceInfos = serviceInfos;
        }

        public virtual void ServiceAdd(IService service) { }
    }
}
