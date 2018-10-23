using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Data.Models;
using ExpirmentalModel;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.FabricTransport;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Client;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Utility;

[assembly: FabricTransportServiceRemotingProvider(RemotingListenerVersion = RemotingListenerVersion.V2, RemotingClientVersion = RemotingClientVersion.V2)]
namespace Data
{
    public interface IDataService : IService
    {
        Task<long> GetCurrentCounter();
        Task<long> TimeTableStorage();
        Task<long> TimeQueueStorage();
        Task<long> InterServiceRqCall();
    }

    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class Data : StatefulService, IDataService
    {
        ServiceProxyFactory proxyFactory = new ServiceProxyFactory((c) =>
        {
            var settings = new FabricTransportRemotingSettings();
            return new FabricTransportServiceRemotingClientFactory(settings);
        });

        public async Task<long> InterServiceRqCall()
        {
            ServiceEventSource.Current.Message("Starting Inter Service call on Data Service");
            var range = Enumerable.Range(1, 1000).ToArray();
            IUtilityService client = proxyFactory.CreateServiceProxy<IUtilityService>(new Uri("fabric:/experiment/Utility"));
            Stopwatch s = new Stopwatch();
            s.Start();
            //Parallel.ForEach(range, async (current) =>
            //{
            //    await client.InterServiceRqCallAsync(new InterServiceMessage()
            //    {
            //         Name = current.ToString(),
            //    });
            //});
            try
            {
                await client.InterServiceRqCallAsync(new InterServiceMessage()
                {
                    Name = 1.ToString(),
                });
            }catch(Exception ex)
            {

            }
            
            s.Stop();
            return await Task.Run<long>(() => s.ElapsedMilliseconds);
        }

        public async Task<long> TimeQueueStorage()
        {
            var json = "{PartitionKey:1, RowKey:1, Id:1, Timestamp:2018-10-08T08:24:48.552Z, Name: simon segal}";
            var range = Enumerable.Range(1, 1000).ToArray();
            Stopwatch s = new Stopwatch();
            s.Start();
            Parallel.ForEach(range, (current) =>
            {
                // Retrieve storage account from connection string.
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=bbtempau;AccountKey=fHI6lZtAEvZmqT4LiBlhKUzjU0URX4IVOslERphG8fw1K/GVGS9RqS+PlukqbQj7MRMlN/DAY0xpKXuZNraO+w==;EndpointSuffix=core.windows.net");

                // Create the queue client.
                CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

                // Retrieve a reference to a queue.
                CloudQueue queue = queueClient.GetQueueReference("speedtest");

                // Create the queue if it doesn't already exist.
                queue.CreateIfNotExists();

                // Create a message and add it to the queue.
                CloudQueueMessage message = new CloudQueueMessage(json);
                queue.AddMessage(message);
            });
            s.Stop();
            return await Task.Run<long>(() => s.ElapsedMilliseconds);
        }

        public async Task<long> TimeTableStorage()
        {
            var range = Enumerable.Range(1, 1000).ToArray();
            Stopwatch s = new Stopwatch();
            s.Start();
            Parallel.ForEach(range,  (current) =>
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=bbtempau;AccountKey=fHI6lZtAEvZmqT4LiBlhKUzjU0URX4IVOslERphG8fw1K/GVGS9RqS+PlukqbQj7MRMlN/DAY0xpKXuZNraO+w==;EndpointSuffix=core.windows.net");

                // Create the table client.
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

                // Create the CloudTable object that represents the "people" table.
                CloudTable table = tableClient.GetTableReference("speedtest");
                table.CreateIfNotExists();

                // Create a new customer entity.
                Entity entity = new Entity()
                {
                    Id = current,
                    Name = "TableEntity",
                    RowKey = DateTime.Now.Ticks.ToString()+current,
                    PartitionKey = 0.ToString()
                };

                // Create the TableOperation object that inserts the customer entity.
                TableOperation insertOperation = TableOperation.Insert(entity);

                // Execute the insert operation.
                table.Execute(insertOperation);
            });
            s.Stop();
            return await Task.Run<long>(() => s.ElapsedMilliseconds);
        }

        public Data(StatefulServiceContext context): base(context){ }

        public async Task<long> GetCurrentCounter()
        {
            var myDictionary = await StateManager.GetOrAddAsync<IReliableDictionary<string, long>>("myDictionary");
            long result;
            using (var tx = this.StateManager.CreateTransaction())
            {
                var res = await myDictionary.TryGetValueAsync(tx, "Counter");
                result = res.Value;
            }
            return result;
        }

        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return this.CreateServiceRemotingReplicaListeners();
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
    }
}
