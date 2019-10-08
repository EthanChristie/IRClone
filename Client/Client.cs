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

                var currentChatLine = "";

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var message = Encoding.UTF8.GetString(ea.Body);
                    var currentChatLineLength = currentChatLine.Length;
                    Console.CursorLeft = 0;
                    Console.Write(new string(' ', currentChatLineLength));
                    Console.CursorLeft = 0;

                    Console.WriteLine(message);
                    Console.Write(currentChatLine);
                };

                channel.BasicConsume(
                    queue: queueName,
                    autoAck: true,
                    consumer: consumer
                );

                

                while (true)
                {
                    while (true)
                    {
                        // Turns out console apps can be a bit finnicky!
                        var input = Console.ReadKey();

                        switch (input.Key)
                        {
                            case ConsoleKey.LeftArrow:
                                Console.SetCursorPosition(Math.Max(0, Console.CursorLeft - 1), Console.CursorTop);
                                break;
                            case ConsoleKey.RightArrow:
                                Console.CursorLeft = Math.Min(Console.CursorLeft + 1, currentChatLine.Length);
                                break;
                            case ConsoleKey.Backspace:
                                currentChatLine = currentChatLine.Substring(0, Math.Max(currentChatLine.Length - 1, 0));
                                Console.CursorLeft = 0;
                                Console.Write(currentChatLine + ' ');
                                Console.CursorLeft--;
                                break;
                            default:
                            {
                                if (char.IsLetterOrDigit(input.KeyChar) ||
                                    char.IsWhiteSpace(input.KeyChar) ||
                                    char.IsPunctuation(input.KeyChar))
                                {
                                    currentChatLine += input.KeyChar;
                                }

                                break;
                            }
                        }

                        if (input.Key == ConsoleKey.Enter) 
                        {
                            break;
                        }
                    }

                    var body = JsonSerializer.Serialize(new ChatMessageDto {
                        Username = username,
                        Message = currentChatLine
                    });

                    currentChatLine = "";

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
