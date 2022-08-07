using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Azure;

namespace ServiceBusDemo.Controllers
{
    /// <summary>
    /// Service Bus Queue + IAzureClientFactory 示範用 Controller
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class QueueWithDiController : ControllerBase
    {
        private readonly ServiceBusClient _serviceBusClient;

        public QueueWithDiController(
            IAzureClientFactory<ServiceBusClient> azureClientFactory)
        {
            _serviceBusClient = azureClientFactory.CreateClient(name: "ServiceBusClient");
        }

        /// <summary>
        /// 將訊息放入佇列
        /// </summary>
        [HttpPost]
        public async Task Enqueue([FromBody] string context)
        {
            var queueName = "YOUR QUEUE NAME";
            await using var sender = _serviceBusClient.CreateSender(queueName);
            
            var message = new ServiceBusMessage(context);
            await sender.SendMessageAsync(message);
        }
    }
}