using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Mvc;

namespace ServiceBusDemo.Controllers
{
    /// <summary>
    /// Service Bus Queue 示範用 Controller
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class QueueController : ControllerBase
    {
        /// <summary>
        /// 將訊息放入佇列
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

            // 傳遞 Queue 的名字給 CreateSender 方法來建立 Sender
            // 和 ServiceBusClient 一樣，有提供 DisposeAsync 方法來關閉
            // 或是直接使用 await using 包起來
            var queueName = "YOUR QUEUE NAME";
            await using var sender = client.CreateSender(queueName);

            // 將要傳送的訊息包裝成 ServiceBusMessage
            // 並使用 ServiceBusSender.SendMessageAsync 傳送出去
            var message = new ServiceBusMessage(context);
            await sender.SendMessageAsync(message);
        }

        /// <summary>
        /// 將一堆訊息放入佇列
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        [HttpPost("Batch")]
        public async Task EnqueueBatch([FromBody] string context)
        {
            // 把訊息重複個十次，假裝我們有很多訊息
            var contexts = Enumerable.Repeat(context, 10);
            
            // 和單則訊息的場合一樣：建立 Client 及 Sender
            var connectionString = "YOUR SERVICE BUS CONNECTION STRING";
            await using var client = new ServiceBusClient(connectionString);
            
            var queueName = "YOUR QUEUE NAME";
            await using var sender = client.CreateSender(queueName);

            // 從 Sender 來建立一批訊息（類似郵差包的感覺）
            using var messageBatch = await sender.CreateMessageBatchAsync();

            // 將訊息逐一嘗試放到這批訊息中（把信塞到郵差包的感覺）
            foreach (var text in contexts)
            {
                var message = new ServiceBusMessage(text);
                if (messageBatch.TryAddMessage(message) is false)
                {
                    throw new Exception("放入訊息失敗");
                }
            }

            // 把整個郵差包丟出去
            await sender.SendMessagesAsync(messageBatch);
        }
    }
}