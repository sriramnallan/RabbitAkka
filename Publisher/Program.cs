using RabbitMQ.Client;
using System;
using Topshelf;
using Timer = System.Timers.Timer;
using System.Timers;
using System.Threading;
using System.Configuration;
using Serilog;
using Common;

namespace Publisher
{
    public class Program
    {
        public static void Main()
        {
            if (ConfigurationManager.AppSettings.Get("RunAsService") == "1")
            {
                HostFactory.Run(x =>
                {
                    x.Service<Publisher>(s =>
                     {
                         s.ConstructUsing(name => new Publisher());
                         s.WhenStarted(tc => tc.StartApp());
                         s.WhenStopped(tc => tc.StopApp());
                     });
                    x.RunAsLocalSystem();
                    x.SetDescription("Publisher Host");
                    x.SetDisplayName("Publisher Host");
                    x.SetServiceName("Publisher");
                    x.StartAutomatically();
                });
            }
            else
            {
                new Publisher().StartApp();
                Console.ReadLine();
            }
        }
    }
    public class Publisher
    {
        private Timer _timer;
        private IModel _channel;

        public Publisher()
        {
            var logFile = ConfigurationManager.AppSettings.Get("LogFile");
            Log.Logger = new LoggerConfiguration()
               .WriteTo.Console()
               .WriteTo.RollingFile(logFile, retainedFileCountLimit: 7)
               .CreateLogger();
        }

        public void StartApp()
        {
            Log.Information("Publisher service started");

            var factory = new ConnectionFactory() { HostName = "localhost" };
            var connection = factory.CreateConnection();
            _channel = connection.CreateModel();
            _channel.QueueDeclare(queue: "RabbitAkka", durable: false, exclusive: false, autoDelete: false, arguments: null);

            Log.Information("Queue Declared");
            _timer = new Timer();
            _timer.Elapsed += QueueMessages_Producer;
            _timer.Interval = 2000;
            _timer.Enabled = true;
        }

        private void QueueMessages_Producer(object source, ElapsedEventArgs e)
        {
            _timer.Stop();

            var props = _channel.CreateBasicProperties();
            props.DeliveryMode = 1;

            for (int i = 1; i < 101; i++)
            {
                var messageContent = "Message number " + i + " is " + Guid.NewGuid().ToString() + Guid.NewGuid().ToString() + Guid.NewGuid().ToString();
                var message = new Message(i, messageContent, DateTime.Now);
                _channel.BasicPublish(exchange: "", routingKey: "RabbitAkka", basicProperties: props, body: Message.SerializeIntoBinary(message));
                Log.Information("Message {0} is published", i);
            }
            Thread.Sleep(10000);

            _timer.Start();
        }

        public void StopApp()
        {
            _timer.Stop();
        }
    }
}