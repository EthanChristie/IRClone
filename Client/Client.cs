using System;
using System.Text;
using System.Text.Json;
using Core;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;
using Server;

namespace Client
{
    public class Client
    {
        public static void Main()
        {
            var factory = new ConnectionFactory { HostName = "localhost" };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                Console.WriteLine("Your username: ");
                var username = Console.ReadLine();

                channel.ExchangeDeclare(IRCloneConstants.Exchanges.ServerExchangeName, ExchangeType.Direct);
                channel.ExchangeDeclare(IRCloneConstants.Exchanges.ClientExchangeName, ExchangeType.Fanout);
                var queueName = channel.QueueDeclare().QueueName;

                var basicProperties = new BasicProperties
                {
                    Timestamp = new AmqpTimestamp(DateTime.Now.Ticks)
                };

                channel.BasicPublish(
                    exchange: IRCloneConstants.Exchanges.ServerExchangeName,
                    routingKey: "user_joined",
                    basicProperties: basicProperties,
                    body: Encoding.UTF8.GetBytes(username)
                );


                channel.QueueBind(
                    queue: queueName,
                    exchange: IRCloneConstants.Exchanges.ClientExchangeName,
                    routingKey: ""
                );

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var message = Encoding.UTF8.GetString(ea.Body);
                    Console.WriteLine(message);
                };

                channel.BasicConsume(
                    queue: queueName,
                    autoAck: true,
                    consumer: consumer
                );

                

                while (true)
                {
                    var chatLine = Console.ReadLine();

                    var body = JsonSerializer.Serialize(new ChatMessageDto {
                        Username = username,
                        Message = chatLine
                    });

                    channel.BasicPublish(
                        exchange: IRCloneConstants.Exchanges.ServerExchangeName,
                        routingKey: "user_chatted",
                        basicProperties: basicProperties,
                        body: Encoding.UTF8.GetBytes(body)
                    );
                }
            }
        }
    }
}
