using RabbitMQ.Client;
using System;
using Topshelf;
using Timer = System.Timers.Timer;
using System.Timers;
using System.Threading;
using System.Configuration;
using log4net;
using System.Reflection;

namespace Publisher
{
    public class Program
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));
        public static void Main()
        {
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            log4net.Config.XmlConfigurator.Configure(logRepository, new System.IO.FileInfo("log4net.config"));
            log.Info("Main started");

            if (ConfigurationManager.AppSettings.Get("RunAsService") == "1")
            {
                HostFactory.Run(x =>
                {
                   x.Service<Publisher>(s =>
                    {
                        s.ConstructUsing(name => new Publisher(log));
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
                new Publisher(log).StartApp();
                Console.ReadLine();
            }
        }
    }
    public class Publisher
    {
        private Timer _timer;
        private ILog _log;
        public Publisher(ILog log)
        {
            _log = log;
        }

        public void StartApp()
        {
            _log.Info("Startpp started");
            _timer = new Timer();
            _timer.Elapsed += QueueMessages_Producer;
            _timer.Interval = 2000;
            _timer.Enabled = true;
        }

        private void QueueMessages_Producer(object source, ElapsedEventArgs e)
        {
            _timer.Stop();
            var factory = new ConnectionFactory() { HostName = "localhost" };
            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();
            channel.QueueDeclare(queue: "RabbitAkka", durable: false, exclusive: false, autoDelete: false, arguments: null);
            var props = channel.CreateBasicProperties();
            props.ContentType = "text/plain";
            props.DeliveryMode = 2;
            int i;
            for (i = 1; i < 101; i++)
            {
                string message = "Message number " + i + " is " + Guid.NewGuid().ToString() + Guid.NewGuid().ToString() + Guid.NewGuid().ToString();
                channel.BasicPublish(exchange: "", routingKey: "RabbitAkka", basicProperties: props, body: System.Text.Encoding.ASCII.GetBytes(message));
            }
            Console.WriteLine(i);
            Thread.Sleep(10000);
            _timer.Start();
        }

        public void StopApp()
        {
            _timer.Stop();
        }
    }
}