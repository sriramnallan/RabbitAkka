using RabbitMQ.Client;
using System;
using Topshelf;
using Timer = System.Timers.Timer;
using System.Timers;
using System.Threading;
using System.Configuration;
using Serilog;

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