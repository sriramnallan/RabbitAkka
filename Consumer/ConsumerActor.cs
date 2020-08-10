using RabbitMQ.Client;
using System;
using Akka.Actor;
using System.Threading;
using Serilog;
using Common;

namespace Consumer
{
    public class ConsumerActor : ReceiveActor
    {
        readonly IModel _channel;
        public ConsumerActor(IModel channel)
        {
            _channel = channel;
            Receive<Message>(message => ProcessStringMessage(message));
        }
        private void ProcessStringMessage(Message message)
        {
            if (message.MessageId == 50)
            {
                var props = _channel.CreateBasicProperties();
                props.DeliveryMode = 1;
                _channel.BasicPublish(exchange: "", routingKey: "UnprocessedRabbitAkka", basicProperties: props, body: Message.SerializeIntoBinary(message));
            }
            Console.WriteLine(message.MessageContent);
            Log.Information(Thread.CurrentThread.ManagedThreadId.ToString());
        }
    }
}
