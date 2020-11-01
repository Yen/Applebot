using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Applebot
{

    class GatewayServiceResolver
    {
        private Dictionary<Type, IGatewayService> _GatewayServices = new Dictionary<Type, IGatewayService>();

        public void RegisterGateway(Type gatewayType, IGatewayService gateway)
        {
            _GatewayServices[gatewayType] = gateway;
        }

        public void UnregisterGateway(Type gatewayType)
        {
            _GatewayServices.Remove(gatewayType);
        }

        public IGatewayService TryGetGateway(Type gatewayType)
        {
            if (_GatewayServices.TryGetValue(gatewayType, out var gateway))
            {
                return gateway;
            }
            return null;
        }

        public TGateway TryGetGateway<TGateway>() where TGateway : IGatewayService
        {
            return (TGateway)TryGetGateway(typeof(TGateway));
        }
    }

}