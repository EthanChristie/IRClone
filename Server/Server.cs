using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Server
{
    public class Server
    {
        public static void Main(string[] args)
        {
            var factory = new ConnectionFactory { HostName = "localhost" };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare("irclone_server", ExchangeType.Direct);
                channel.ExchangeDeclare("irclone_client", ExchangeType.Fanout);
                var queueName = channel.QueueDeclare().QueueName;

                channel.QueueBind(
                    queue: queueName,
                    exchange: "irclone_server",
                    routingKey: "user_joined"
                );

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var username = Encoding.UTF8.GetString(ea.Body);
                    var message = $"<Server>: {username} has joined the chat!";

                    channel.BasicPublish(
                        exchange: "irclone_client",
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

                Console.ReadLine();
            }



            Console.WriteLine(" [*] Waiting for messages");

            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();
        }
        }
    }
