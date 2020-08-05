using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using Akka.Actor;
using Akka.Routing;
using System.Threading;
using Topshelf;
using Timer = System.Timers.Timer;
using System.Timers;
using System.Configuration;
using Serilog;

namespace Consumer
{
    public class Program
    {
        public static void Main()
        {
            if (ConfigurationManager.AppSettings.Get("RunAsService") == "1")
            {
                HostFactory.Run(x =>
                {
                    x.Service<Consumer>(s =>
                    {
                        s.ConstructUsing(name => new Consumer());
                        s.WhenStarted(tc => tc.StartApp());
                        s.WhenStopped(tc => tc.StopApp());
                    });
                    x.RunAsLocalSystem();
                    x.SetDescription("Conusmer Host");
                    x.SetDisplayName("Conusmer Host");
                    x.SetServiceName("Conusmer");
                    x.StartAutomatically();
                });
            }
            else
            {
                new Consumer().StartApp();
                Console.ReadLine();
            }
        }
    }

    public class Consumer
    {
        private Timer _timer;
      
        public Consumer()
        {
            var logFile = ConfigurationManager.AppSettings.Get("LogFile");
            Log.Logger = new LoggerConfiguration()
               .WriteTo.Console()
               .WriteTo.RollingFile(logFile, retainedFileCountLimit: 7)
               .CreateLogger();
        }

        public void StartApp()
        {
            Log.Information("Consumer service started");
            _timer = new Timer();
            _timer.Elapsed += ConsumeMessages_Consumer;
            _timer.Interval = 5000;
            _timer.Enabled = true;
        }

        private void ConsumeMessages_Consumer(object source, ElapsedEventArgs e)
        {
            _timer.Stop();
            ActorSystem MedData = ActorSystem.Create("RabbitAkka");
            Props BaseActor = Props.Create<ConsumerBaseActor>().WithRouter(new RoundRobinPool(200));
            IActorRef medActor = MedData.ActorOf(BaseActor);

            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "RabbitAkka", durable: false, exclusive: false, autoDelete: false, arguments: null);
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    medActor.Tell(body);
                };
                channel.BasicConsume(queue: "RabbitAkka", autoAck: true, consumer: consumer);
                Thread.Sleep(Timeout.Infinite);
            }
            
        }

        public void StopApp()
        {
        }
    }

    public class ConsumerBaseActor : UntypedActor
    {
        protected override void OnReceive(object message)
        {
            string str = System.Text.Encoding.ASCII.GetString((byte[])message);
            Console.WriteLine(str);
            Console.WriteLine(Thread.CurrentThread.ManagedThreadId);
        }
    }
}