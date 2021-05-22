using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace TT.FileParserFunction
{
    public class MessageBus : IMessageBus
    {
        private readonly ILogger<MessageBus> _log;
        private readonly string _connectionString;
        private readonly string _queueName;

        public MessageBus(ILogger<MessageBus> log, IConfiguration configuration)
        {
            _log = log;
            _connectionString = configuration.GetValue<string>("ServiceBusConnection");
            _queueName = configuration.GetValue<string>("ServiceBusQueue");
        }

        public async Task<bool> SendMessage(FileInfo fileInfo)
        {
            try
            {
                await using (ServiceBusClient client = new ServiceBusClient(_connectionString))
                {
                    var sender = client.CreateSender(_queueName);
                    var jsonString = JsonSerializer.Serialize(fileInfo);
                    var message = new ServiceBusMessage(jsonString);

                    await sender.SendMessageAsync(message);

                    _log.LogInformation($"Sent a single message to the queue: {_queueName} with info {jsonString}");
                    
                    return true;
                }
            }

            catch (Exception exp)
            {
                _log.LogError($"Exception occurred when sending message to Azure bus - {exp.Message}");
                return false;
            }
        }
    }
}
