using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using ExpirmentalModel;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.FabricTransport;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.V1.FabricTransport.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

[assembly: FabricTransportServiceRemotingProvider(RemotingListenerVersion = RemotingListenerVersion.V1, RemotingClientVersion = RemotingClientVersion.V1)]
namespace Utility
{
    public interface IUtilityService : IService
    {
        Task<long> InterServiceRqCallAsync(InterServiceMessage msg);
        Task<long> Add100MessagesToTheQueue();
        Task<bool> StartPoppingOffQueue();
    }

    internal sealed class Utility : StatefulService, IUtilityService
    {
        #region Constructor

        public Utility(StatefulServiceContext context)
            : base(context)
        { }

        #endregion

        #region Public Interface

        public async Task<long> Add100MessagesToTheQueue()
        {
            var queue = await StateManager.GetOrAddAsync<IReliableQueue<InterServiceMessage>>("loggingQueue");
            for(var i = 0; i <= 1000; i++)
            {
                var msg = new InterServiceMessage() { Name = i.ToString() };
                using (var tx = StateManager.CreateTransaction())
                {
                    await queue.EnqueueAsync(tx, msg);

                    await tx.CommitAsync();
                    await Task.Delay(5);
                }
            }
            long count;
            using (var tx = StateManager.CreateTransaction())
            {
                count = queue.GetCountAsync(tx).Result;
            }

            return await Task.FromResult<long>(count);
        }

        public async Task<long> InterServiceRqCallAsync(InterServiceMessage msg)
        {
            ServiceEventSource.Current.Message("Starting Inter Service call on Utility", new object[1] { msg });
            var queue = await StateManager.GetOrAddAsync<IReliableQueue<InterServiceMessage>>("loggingQueue");

            using (var tx = StateManager.CreateTransaction())
            {
                await queue.EnqueueAsync(tx, msg);

                await tx.CommitAsync();
            }
            long count;
            using (var tx = StateManager.CreateTransaction())
            {
                count = queue.GetCountAsync(tx).Result;
            }

            return await Task.FromResult<long>(count);
        }

        public async Task<bool> StartPoppingOffQueue()
        {
            Task.Run(async () =>
            {
                var queue = await StateManager.GetOrAddAsync<IReliableQueue<InterServiceMessage>>("loggingQueue");

                for (var i = 0; i <= 1000; i++)
                {
                    using (var tx = StateManager.CreateTransaction())
                    {
                        await queue.TryDequeueAsync(tx);

                        await tx.CommitAsync();
                    }
                    Task.Delay(100);
                }
            });
            return await Task.FromResult(true);
        }

        #endregion

        #region SF Overiddes

        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        //protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        //{
        //    return this.CreateServiceRemotingReplicaListeners();
        //}

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new[] { new ServiceReplicaListener(context => new FabricTransportServiceRemotingListener(context, this), listenOnSecondary: true) };
        }

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following sample code with your own logic 
            //       or remove this RunAsync override if it's not needed in your service.

            var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>("myDictionary");

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using (var tx = this.StateManager.CreateTransaction())
                {
                    var result = await myDictionary.TryGetValueAsync(tx, "Counter");

                    ServiceEventSource.Current.ServiceMessage(this.Context, "Current Counter Value: {0}",
                        result.HasValue ? result.Value.ToString() : "Value does not exist.");

                    await myDictionary.AddOrUpdateAsync(tx, "Counter", 0, (key, value) => ++value);

                    // If an exception is thrown before calling CommitAsync, the transaction aborts, all changes are 
                    // discarded, and nothing is saved to the secondary replicas.
                    await tx.CommitAsync();
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        #endregion
    }
}
