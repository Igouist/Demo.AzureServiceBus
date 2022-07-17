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
    }
}