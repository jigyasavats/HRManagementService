using System.Text;
using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;

namespace HRManagementService.Pipeline
{
    public class ServiceBusService : IAsyncDisposable
    {
        private readonly ServiceBusClient _client;

        public ServiceBusService(string connectionString)
        {
            _client = new ServiceBusClient(connectionString);
        }

        private async Task PublishAsync(string queueName, object eventData)
        {
            await using var sender = _client.CreateSender(queueName);
            var json = JsonConvert.SerializeObject(eventData);
            var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(json));
            await sender.SendMessageAsync(message);
        }

        public async Task<T?> PublishAndProcessAsync<T>(string queueName, object eventData, Func<string, Task<T>> handler)
        {
            await PublishAsync(queueName, eventData);

            await using var receiver = _client.CreateReceiver(queueName, new ServiceBusReceiverOptions
            {
                ReceiveMode = ServiceBusReceiveMode.PeekLock
            });

            var message = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(30));

            if (message == null)
                throw new TimeoutException($"No message received from queue '{queueName}' within 30 seconds.");

            var json = Encoding.UTF8.GetString(message.Body);
            var result = await handler(json);

            // Mark message as processed — removes it from queue permanently
            await receiver.CompleteMessageAsync(message);
            return result;
        }

        public async ValueTask DisposeAsync()
        {
            await _client.DisposeAsync();
        }
    }
}
