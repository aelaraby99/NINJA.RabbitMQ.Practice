/*
 * NOTES - Preserved commented code from original Program.cs
 * 
 * This file contains all the commented code that was in the original Program.cs
 * to preserve the learning notes and experimental code.
 */

// Original connection setup code:
// var factory = new ConnectionFactory()
// {
//     HostName = "localhost",
//     UserName = "guest",
//     Password = "guest",
//     Port = 5672
// };

// Quality of Service setting:
// channel.BasicQos(prefetchSize: 0,prefetchCount: 1,global: false); // Set prefetch count to 1 to allow processing of 1 messages at a time

// Alternative queue declaration:
// channel.QueueDeclare(
//     queue: "orders",
//     durable: true,
//     exclusive: false,
//     autoDelete: true);

// Queue binding example:
// channel.QueueBind(
//     queue: "weather-forecasts",
//     exchange: "weather-exchange",
//     routingKey: "forecast.*");

// Multiple consumer loop:
// for (int i = 1;i <= 3;i++)
// {
//     // fair dispatch
//     var consumer = new EventingBasicConsumer(channel);
//     var consumerId = i;
//     consumer.Received += (sender,ea) =>
//     {
//         var body = ea.Body.ToArray();
//         var message = Encoding.UTF8.GetString(body);
//         Console.WriteLine($"Consumer {consumerId} Received Message: {message}");
//         Thread.Sleep(1000); // Simulate processing time
//         channel.BasicAck(deliveryTag: ea.DeliveryTag,multiple: false);
//         //channel.BasicNack(deliveryTag: ea.DeliveryTag,multiple: false,requeue: true); = channel.BasicReject(deliveryTag: ea.DeliveryTag,requeue: true);
//     };

// Priority consumer arguments:
// var argys = new Dictionary<string,object>();
// if(i == 1)
//     argys.Add("x-priority",10);
// if(i == 2)
//     argys.Add("x-priority",5);

// Alternative consume setup:
// channel.BasicConsume(
//     queue: "orders",
//     autoAck: false,
//     consumer: consumer);

// Alternative consume with arguments:
// channel.BasicConsume(
//    queue: "weather-forecasts",
//    autoAck: false,
//    arguments:argys,
//    consumer: consumer);
// }
