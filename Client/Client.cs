using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;

namespace Client
{
    public class Client
    {
        private const string IRCloneServer = "irclone_server";
        private const string IRCloneClient = "irclone_client";

        public static void Main(string[] args)
        {
            var factory = new ConnectionFactory { HostName = "localhost" };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                Console.WriteLine("Your username: ");
                var username = Console.ReadLine();

                channel.ExchangeDeclare(IRCloneServer, ExchangeType.Direct);
                channel.ExchangeDeclare(IRCloneClient, ExchangeType.Fanout);
                var queueName = channel.QueueDeclare().QueueName;

                var basicProperties = new BasicProperties
                {
                    Timestamp = new AmqpTimestamp(DateTime.Now.Ticks)
                };

                channel.BasicPublish(
                    exchange: IRCloneServer,
                    routingKey: "user_joined",
                    basicProperties: basicProperties,
                    body: Encoding.UTF8.GetBytes(username)
                );


                channel.QueueBind(
                    queue: queueName,
                    exchange: "irclone_client",
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

                Console.ReadLine();
            }
                
        }
    }
}
