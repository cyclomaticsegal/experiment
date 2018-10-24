using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.FabricTransport;
using Microsoft.ServiceFabric.Services.Remoting.V1.FabricTransport.Client;
using Utility;

namespace experimentApi.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        ServiceProxyFactory proxyFactory = new ServiceProxyFactory((c) =>
        {
            var settings = new FabricTransportRemotingSettings();
            return new FabricTransportServiceRemotingClientFactory(settings);
        });

        // Default Get
        [HttpGet]
        public string Get()
        {
            return Heartbeat();
        }

        [HttpGet]
        [Route("Heartbeat")]
        public string Heartbeat()
        {
            return "UTC Heartbeat at: " + DateTime.UtcNow.ToLongDateString();
        }

        [HttpGet]
        [Route("GetReplicaList")]
        public async Task<List<string>> ReplicaList()
        {
            return new List<string>();
            var resolver = ServicePartitionResolver.GetDefault();
            var fabricClient = new FabricClient();
            var apps = fabricClient.QueryManager.GetApplicationListAsync().Result;
            foreach (var app in apps)
            {
                Console.WriteLine($"Discovered application:'{app.ApplicationName}");

                var services = fabricClient.QueryManager.GetServiceListAsync(app.ApplicationName).Result;
                foreach (var service in services)
                {
                    Console.WriteLine($"Discovered Service:'{service.ServiceName}");

                    var partitions = fabricClient.QueryManager.GetPartitionListAsync(service.ServiceName).Result;
                    foreach (var partition in partitions)
                    {
                        Console.WriteLine($"Discovered Service Partition:'{partition.PartitionInformation.Kind} {partition.PartitionInformation.Id}");


                        ServicePartitionKey key;
                        switch (partition.PartitionInformation.Kind)
                        {
                            case ServicePartitionKind.Singleton:
                                key = ServicePartitionKey.Singleton;
                                break;
                            case ServicePartitionKind.Int64Range:
                                var longKey = (Int64RangePartitionInformation)partition.PartitionInformation;
                                key = new ServicePartitionKey(longKey.LowKey);
                                break;
                            case ServicePartitionKind.Named:
                                var namedKey = (NamedPartitionInformation)partition.PartitionInformation;
                                key = new ServicePartitionKey(namedKey.Name);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException("partition.PartitionInformation.Kind");
                        }
                        var resolved = resolver.ResolveAsync(service.ServiceName, key, CancellationToken.None).Result;
                        foreach (var endpoint in resolved.Endpoints)
                        {
                            Console.WriteLine($"Discovered Service Endpoint:'{endpoint.Address}");
                            //endpoint.Role == ServiceEndpointRole.
                        }
                    }
                }
            }
        }

        [HttpGet]
        [Route("Put100LogMessagesOnQueue")]
        public async Task<string> Put100LogMessagesOnQueue()
        {
            IUtilityService client = proxyFactory.CreateServiceProxy<IUtilityService>(new Uri("fabric:/experiment/Utility"));
            var messagesOnQ = await client.Add100MessagesToTheQueue();
            return messagesOnQ.ToString();
        }

        [HttpGet]
        [Route("StartPoppingTheQueue")]
        public async Task<string> StartPoppingTheQueue()
        {
            IUtilityService client = proxyFactory.CreateServiceProxy<IUtilityService>(new Uri("fabric:/experiment/Utility"));
            var messagesOnQ = await client.StartPoppingOffQueue();
            return messagesOnQ.ToString();
        }

        [HttpGet]
        [Route("InterProcessResultsAsync")]
        public async Task<string> InterProcessResultsAsync()
        {
            IDataService client = proxyFactory.CreateServiceProxy<IDataService>(new Uri("fabric:/experiment/Data"));
            var message = await client.InterServiceRqCall();
            return message.ToString();
        }

        [HttpGet]
        [Route("TableResultsAsync")]
        public async Task<string> TableResultsAsync()
        {
            IDataService client = proxyFactory.CreateServiceProxy<IDataService>(new Uri("fabric:/experiment/Data"));
            var message = await client.TimeTableStorage();
            return message.ToString();
        }

        [HttpGet]
        [Route("QueueResultsAsync")]
        public async Task<string> QueueResultsAsync()
        {
            IDataService client = proxyFactory.CreateServiceProxy<IDataService>(new Uri("fabric:/experiment/Data"));
            var message = await client.TimeQueueStorage();
            return message.ToString();
        }

        // GET api/values/5
        [HttpGet("{id}")]
        [Route("GetCounter")]
        public async Task<string> GetCounter(int id)
        {
            IDataService client = proxyFactory.CreateServiceProxy<IDataService>(new Uri("fabric:/experiment/Data"));
            try
            {
                var message = await client.GetCurrentCounter();
                return message.ToString();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
