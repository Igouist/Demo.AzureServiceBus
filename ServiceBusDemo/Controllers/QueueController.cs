using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Mvc;

namespace ServiceBusDemo.Controllers
{
    /// <summary>
    /// Service Bus Queue �ܽd�� Controller
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class QueueController : ControllerBase
    {
        /// <summary>
        /// �N�T����J��C
        /// </summary>
        [HttpPost]
        public async Task Enqueue([FromBody] string context)
        {
            // �� ServiceBus ���s�u�r��إ� Client
            // �s�u�r��i�H�b Azure ServiceBus �������@�Φs����h���
            // ServiceBusClient �Χ��O�o�n�I�s DisposeAsync() ������
            // �άO�����ϥ� await using �]�_��
            var connectionString = "YOUR SERVICE BUS CONNECTION STRING";
            await using var client = new ServiceBusClient(connectionString);

            // �ǻ� Queue ���W�r�� CreateSender ��k�ӫإ� Sender
            // �M ServiceBusClient �@�ˡA������ DisposeAsync ��k������
            // �άO�����ϥ� await using �]�_��
            var queueName = "YOUR QUEUE NAME";
            await using var sender = client.CreateSender(queueName);

            // �N�n�ǰe���T���]�˦� ServiceBusMessage
            // �èϥ� ServiceBusSender.SendMessageAsync �ǰe�X�h
            var message = new ServiceBusMessage(context);
            await sender.SendMessageAsync(message);
        }

        /// <summary>
        /// �N�@��T����J��C
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        [HttpPost("Batch")]
        public async Task EnqueueBatch([FromBody] string context)
        {
            // ��T�����ƭӤQ���A���˧ڭ̦��ܦh�T��
            var contexts = Enumerable.Repeat(context, 10);
            
            // �M��h�T�������X�@�ˡG�إ� Client �� Sender
            var connectionString = "YOUR SERVICE BUS CONNECTION STRING";
            await using var client = new ServiceBusClient(connectionString);
            
            var queueName = "YOUR QUEUE NAME";
            await using var sender = client.CreateSender(queueName);

            // �q Sender �ӫإߤ@��T���]�����l�t�]���Pı�^
            using var messageBatch = await sender.CreateMessageBatchAsync();

            // �N�T���v�@���թ��o��T�����]��H���l�t�]���Pı�^
            foreach (var text in contexts)
            {
                var message = new ServiceBusMessage(text);
                if (messageBatch.TryAddMessage(message) is false)
                {
                    throw new Exception("��J�T������");
                }
            }

            // ���Ӷl�t�]��X�h
            await sender.SendMessagesAsync(messageBatch);
        }
    }
}