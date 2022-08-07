using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Mvc;

namespace ServiceBusDemo.Controllers
{
    /// <summary>
    /// Service Bus Topic 示範用 Controller
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class TopicController : ControllerBase
    {
        /// <summary>
        /// 將訊息放入主題
        /// </summary>
        [HttpPost]
        public async Task Enqueue([FromBody] string context)
        {
            // 用 ServiceBus 的連線字串建立 Client
            // 連線字串可以在 Azure ServiceBus 頁面的共用存取原則找到
            // ServiceBusClient 用完記得要呼叫 DisposeAsync() 來關掉
            // 或是直接使用 await using 包起來
            var connectionString = "YOUR SERVICE BUS CONNECTION STRING";
            await using var client = new ServiceBusClient(connectionString);

            // 傳遞 Topic 的名字給 CreateSender 方法來建立 Sender
            // 和 ServiceBusClient 一樣，有提供 DisposeAsync 方法來關閉
            // 或是直接使用 await using 包起來
            var topicName = "YOUR TOPIC NAME";
            await using var sender = client.CreateSender(topicName);

            // 將要傳送的訊息包裝成 ServiceBusMessage
            // 並使用 ServiceBusSender.SendMessageAsync 傳送出去
            var message = new ServiceBusMessage(context);
            await sender.SendMessageAsync(message);
        }
    }
}
