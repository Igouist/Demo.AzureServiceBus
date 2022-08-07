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

        /// <summary>
        /// ���X��C������h�T��
        /// </summary>
        /// <returns></returns>
        [HttpGet("Receive")]
        public async Task<string> Receive()
        {
            // �M�o�e�T�������X�t���h�G���إ� Client �� Receiver
            var connectionString = "YOUR SERVICE BUS CONNECTION STRING";
            await using var client = new ServiceBusClient(connectionString);

            // �M�e���� ServiceBusSender �@�ˡA������ DisposeAsync ��k���ڭ̥Χ�������
            // �άO�����ϥ� await using �]�_��
            var queueName = "YOUR QUEUE NAME";
            await using var receiver = client.CreateReceiver(queueName);

            // �ϥ� ReceiveMessageAsync �ӧ�T��Ū���X��
            var message = await receiver.ReceiveMessageAsync();
            var body = message.Body.ToString();
            return body;
        }

        /// <summary>
        /// ���X��C�����T��
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task Dequeue()
        {
            // �M�o�e�T�������X�t���h�G���إ� Client �� Processor
            var connectionString = "YOUR SERVICE BUS CONNECTION STRING";
            await using var client = new ServiceBusClient(connectionString);

            // �M�e���� ServiceBusSender �@�ˡA������ DisposeAsync ��k���ڭ̥Χ�������
            // �άO�����ϥ� await using �]�_��
            var queueName = "YOUR QUEUE NAME";
            await using var processor = client.CreateProcessor(queueName);

            // �i�D Processor �ڭ̷Q���B�z�T��
            processor.ProcessMessageAsync += MessageHandler;
            processor.ProcessErrorAsync += ErrorHandler;

            // �� Processor �W�u�A�}�l�����T��
            await processor.StartProcessingAsync();

            // ��ڤW�|������ processor �@���B�z�e�Ӫ��T��
            // �o��N�N��N��]�Ӥ@�U�U
            await Task.Delay(1000);

            // �� Processor �U�Z��
            await processor.StopProcessingAsync();
        }

        /// <summary>
        /// �B�z��C�T��
        /// </summary>
        /// <returns></returns>
        private static async Task MessageHandler(ProcessMessageEventArgs args)
        {
            // �q�T���� Body ���X�ڭ̵o�e�ɶ�i�h�����e
            var message = args.Message.Body.ToString();

            // ��T�����e���A�Q�����ơC�o��N�L�X�ӬݭӤ@���N��N��
            Console.WriteLine(message);

            // �i�D Service Bus �o�ӰT�������\�B�z�F
            await args.CompleteMessageAsync(args.Message);
        }

        /// <summary>
        /// �B�z��C���~�T��
        /// </summary>
        /// <returns></returns>
        private static async Task ErrorHandler(ProcessErrorEventArgs args)
        {
            // �q�T�������X���~�T��
            var exception = args.Exception.ToString();

            // ��T�������~�B�z�A�Ҧp�s���x�t�Τ������C�o��]�L�X�ӬݭӤ@���N��N��
            Console.WriteLine(exception);
        }
    }
}