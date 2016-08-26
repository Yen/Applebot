using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace ApplebotEx
{
    public class Program
    {
        private static ILogger _Logger = new ConsoleLogger();

        public static void Main(string[] args)
        {
            _Logger.Log("ApplebotEx");

            var services = new List<IService>();

            // TODO: take in types and check constructor and module attribute info,
            // dotnetcore is bad at loading external code so perhaps in the future

            services.Add(new Modules.DiscordBackend());
            services.Add(new Modules.TwitchBackend());

            services.Add(new Modules.PingCommand());
            services.Add(new Modules.ApplebotInfoCommand());
            services.Add(new Modules.ModManager());
            services.Add(new Modules.YoutubeParser());
            services.Add(new Modules.DynamicResponseManager());

            _Run(services);
        }

        private static void _Run(IReadOnlyCollection<IService> services)
        {
            _Logger.Log($"Service count -> {services.Count}");

            var serviceInfos = new List<ServiceInfo>();

            // initialize service internals
            foreach (var s in services)
                s.InitializeInternals(new ConsoleLogger(s.GetType().Name), serviceInfos);

            // initialize all services
            var succeededServices = new List<IService>();
            var failedServices = new List<IService>();
            foreach (var s in services)
                if (s.Initialize())
                    succeededServices.Add(s);
                else
                    failedServices.Add(s);

            serviceInfos.AddRange(succeededServices.Select(x => new ServiceInfo(x.GetType().ToString(), true)));
            serviceInfos.AddRange(failedServices.Select(x => new ServiceInfo(x.GetType().ToString(), false)));
            // sort alphabetically
            serviceInfos.Sort((x, y) => x.Identifier.CompareTo(y.Identifier));

            // output service info
            lock (_Logger.Lock)
            {
                _Logger.Log("Service initialize pass complete");

                _Logger.Log($"Succeeded count -> {succeededServices.Count}");
                foreach (var s in succeededServices)
                    _Logger.Log($"\t-> {s.GetType()}", LoggerColor.Green);

                _Logger.Log($"Failed count -> {failedServices.Count}");
                foreach (var s in failedServices)
                    _Logger.Log($"\t-> {s.GetType()}", LoggerColor.Red);
            }

            // raise ServiceAdd for all services on all services
            foreach (var x in succeededServices)
                foreach (var y in succeededServices)
                    x.ServiceAdd(y);
        }
    }
}
