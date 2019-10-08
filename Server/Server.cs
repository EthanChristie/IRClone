using System;
using System.Text;
using System.Text.Json;
using Core;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Server
{
    public class Server
    {
        public static void Main()
        {
            var factory = new ConnectionFactory { HostName = "localhost" };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                BindJoinMessages(channel);
                BindChatMessages(channel);
                Console.WriteLine(" [*] Waiting for messages");
                Console.WriteLine(" Press [enter] to exit.");
                Console.ReadLine();
            }
        }

        private static void BindChatMessages(IModel channel)
        {
            channel.ExchangeDeclare(IRCloneConstants.Exchanges.ServerExchangeName, ExchangeType.Direct);
            channel.ExchangeDeclare(IRCloneConstants.Exchanges.ClientExchangeName, ExchangeType.Fanout);
            var queueName = channel.QueueDeclare().QueueName;

            channel.QueueBind(
                queue: queueName,
                exchange: IRCloneConstants.Exchanges.ServerExchangeName,
                routingKey: "user_chatted"
            );

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var messageDto = JsonSerializer.Deserialize<ChatMessageDto>(ea.Body);

                var message = $"<{messageDto.Username}>: {messageDto.Message}";

                channel.BasicPublish(
                    exchange: IRCloneConstants.Exchanges.ClientExchangeName,
                    routingKey: "user_chatted",
                    body: Encoding.UTF8.GetBytes(message)
                );

                Console.WriteLine("[Debug] " + message);
            };

            channel.BasicConsume(
                queue: queueName,
                autoAck: true,
                consumer: consumer
            );
        }

        private static void BindJoinMessages(IModel channel)
        {
            channel.ExchangeDeclare(IRCloneConstants.Exchanges.ServerExchangeName, ExchangeType.Direct);
            channel.ExchangeDeclare(IRCloneConstants.Exchanges.ClientExchangeName, ExchangeType.Fanout);
            var queueName = channel.QueueDeclare().QueueName;

            channel.QueueBind(
                queue: queueName,
                exchange: IRCloneConstants.Exchanges.ServerExchangeName,
                routingKey: "user_joined"
            );

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var username = Encoding.UTF8.GetString(ea.Body);
                var message = $"<Server>: {username} has joined the chat!";

                channel.BasicPublish(
                    exchange: IRCloneConstants.Exchanges.ClientExchangeName,
                    routingKey: "user_joined",
                    body: Encoding.UTF8.GetBytes(message)
                );

                Console.WriteLine("[Debug] " + message);
            };

            channel.BasicConsume(
                queue: queueName,
                autoAck: true,
                consumer: consumer
            );
        }

        
    }
}
