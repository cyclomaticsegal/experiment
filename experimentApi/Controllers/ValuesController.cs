using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.FabricTransport;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Client;

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

        // GET api/values
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public async Task<string> Get(int id)
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
