using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Applebot.Services;

namespace Applebot
{
    class Program
    {
        public static void ServiceLog(Type serviceType, string message)
        {
            Console.WriteLine($"[{serviceType}] {message}");
        }

        public static async Task Main(string[] args)
        {
            Console.WriteLine("Applebot! 🍎🍎🍎");

            // TODO: this is all pretty jank, ideally we will integrate this with the Microsoft.Extensions.DependencyInjection
            // platform and that will give us a centeralised way to deal with logging and configuration. for now though since
            // its not fully obvious how this is all gonna work we are doing everything ad-hoc, which is very little really

            var gatewayServiceTypes = new HashSet<Type>
            {
                typeof(TwitchGatewayService),
                typeof(DiscordGatewayService),
            };

            var backgroundServiceTypes = new HashSet<Type>
            {
                typeof(DiscordStreamNotifier)
            };

            var gatewayConsumerServiceTypes = new HashSet<Type>
            {
                typeof(PingService),
            };

            using var tokenSource = new CancellationTokenSource();

            var gatewayServiceResolver = new GatewayServiceResolver();

            // we force a semantic here with channels that each gateway consumer service should only ever be handling a
            // single gateway message at a time. unsure if this is really going to solve any problems
            // but its makes things easier to think about with the amount of async stuff going on
            var gatewayConsumerServices = gatewayConsumerServiceTypes
                .Select(st =>
                {
                    var channel = Channel.CreateUnbounded<IGatewayMessage>();
                    var gatewayConsumerTask = _ExecuteGatewayConsumerServiceAsync(st, channel.Reader, tokenSource.Token);
                    return (gatewayConsumerTask, channel);
                })
                .ToArray<(Task Task, Channel<IGatewayMessage> Channel)>();

            var gatewayConsumerServiceMessageChannels = gatewayConsumerServices
                .Select(gcs => gcs.Channel.Writer)
                .ToArray();

            var gatewayServiceTasks = gatewayServiceTypes
                .Select(st => _ExecuteGatewayServiceAsync(
                    st,
                    gatewayServiceResolver,
                    gatewayConsumerServiceMessageChannels,
                    tokenSource.Token))
                .ToArray();

            var backgroundServiceTasks = backgroundServiceTypes
                .Select(st => _ExecuteBackgroundServiceAsync(
                    st,
                    gatewayServiceResolver,
                    tokenSource.Token))
                .ToArray();

            var serviceTasks = Enumerable.Empty<Task>()
                .Concat(gatewayServiceTasks)
                .Concat(backgroundServiceTasks)
                .Concat(gatewayConsumerServices.Select(gcs => gcs.Task))
                .ToArray();

            await Task.WhenAny(serviceTasks);
            // we only get to this point if something in the application logic itself crashes, ideally the
            // services are taken care of and restart on their own when something goes wrong

            // this is just me trying to get some sort of info about what broke
            Console.WriteLine("Applebot encountered a fatal error and is terminating");
            foreach (var task in serviceTasks)
            {
                if (task.IsFaulted && task.Exception is not null)
                {
                    Console.WriteLine(task.Exception.ToString());
                }
            }

            // currently the cancellation of the application will not work correctly because the twitch
            // stream reader will not listen to cancellation requests, as such we just terminate the applicaiton
            // but ideally we can fix that and get some better shutdown / error reporting logic in place
            //tokenSource.Cancel();
            //await Task.WhenAll(serviceTasks);
        }

        private static async Task _ExecuteGatewayConsumerServiceAsync(Type serviceType, ChannelReader<IGatewayMessage> messageChannelReader, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                if (messageChannelReader.Completion.IsCompleted)
                {
                    throw new InvalidOperationException("Gateway consumer service message channel was marked as completed");
                }

                ServiceLog(serviceType, "Instantiating new instance");
                var service = (IGatewayConsumerService)Activator.CreateInstance(serviceType);

                try
                {
                    ServiceLog(serviceType, "Initializing");
                    await service.InitializeAsync(ct);
                    ServiceLog(serviceType, "Initialization success");

                    while (true)
                    {
                        var message = await messageChannelReader.ReadAsync();
                        await service.ConsumeMessageAsync(message, ct);
                    }
                }
                catch (Exception ex)
                {
                    ServiceLog(serviceType, "Failure");
                    ServiceLog(serviceType, ex.ToString());
                }
                finally
                {
                    (service as IDisposable)?.Dispose();
                }

                await Task.Delay(TimeSpan.FromSeconds(10), ct);
            }
        }

        private static async Task _ExecuteGatewayServiceAsync(
                Type serviceType,
                GatewayServiceResolver gatewayServiceResolver,
                IEnumerable<ChannelWriter<IGatewayMessage>> messageChannelWriters,
                CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                ServiceLog(serviceType, "Instantiating new instance");
                var service = (IGatewayService)Activator.CreateInstance(serviceType);

                try
                {
                    ServiceLog(serviceType, "Initializing");
                    await service.InitializeAsync(ct);
                    ServiceLog(serviceType, "Initialization success");

                    gatewayServiceResolver.RegisterGateway(serviceType, service);

                    await foreach (var message in service.ExecuteGatewayAsync(ct))
                    {
                        foreach (var channelWriter in messageChannelWriters)
                        {
                            await channelWriter.WriteAsync(message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ServiceLog(serviceType, "Failure");
                    ServiceLog(serviceType, ex.ToString());
                }
                finally
                {
                    gatewayServiceResolver.UnregisterGateway(serviceType);
                    (service as IDisposable)?.Dispose();
                }

                await Task.Delay(TimeSpan.FromSeconds(60), ct);
            }
        }

        private static async Task _ExecuteBackgroundServiceAsync(
            Type serviceType,
            GatewayServiceResolver gatewayServiceResolver,
            CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                ServiceLog(serviceType, "Instantiating new instance");
                var service = (IBackgroundService)Activator.CreateInstance(serviceType);

                try
                {
                    ServiceLog(serviceType, "Initializing");
                    await service.InitializeAsync(ct);
                    ServiceLog(serviceType, "Initialization success");

                    await service.ExecuteBackgroundAsync(gatewayServiceResolver, ct);

                    ct.ThrowIfCancellationRequested();
                    ServiceLog(serviceType, "Background service function returned without cancellation requested, this is not expected behavior");
                }
                catch (Exception ex)
                {
                    ServiceLog(serviceType, "Failure");
                    ServiceLog(serviceType, ex.ToString());
                }
                finally
                {
                    (service as IDisposable)?.Dispose();
                }

                await Task.Delay(TimeSpan.FromSeconds(10), ct);
            }
        }
    }
}
