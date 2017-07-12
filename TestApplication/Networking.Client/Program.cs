namespace Networking.Client
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using CommandLine;
    using ConnectionBehavior;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Reactive.Concurrency;
    using System.Reactive.Subjects;

    public class Program
    {
        public static int Main(string[] args)
        {
            //Observable
            //    .Interval(TimeSpan.FromSeconds(0.1))
            //    .Where(_ => Console.KeyAvailable)
            //    .Select(_ => Console.ReadKey().KeyChar)
            //        .Merge(Observable
            //            .Interval(TimeSpan.FromSeconds(1))
            //            .Select(_ => 'd'))
            //    .Throttle(TimeSpan.FromSeconds(0.9))
            //    .Timestamp()
            //    .Subscribe(x => Console.WriteLine(x.Timestamp));

            //Observable.Buffer(Observable
            //    .Interval(TimeSpan.FromSeconds(0.1))
            //    .Where(_ => Console.KeyAvailable)
            //    .Select(_ => Console.ReadKey().KeyChar), TimeSpan.FromSeconds(1))
            //    .Where(x => !x.Any())
            //    .Timestamp()
            //    .Subscribe(x => Console.WriteLine(x.Timestamp));

            var switches = new BehaviorSubject<bool>(false);
            var query = switches
                .DistinctUntilChanged()
                .Where(on => on)
                .SelectMany(Observable
                    .Interval(TimeSpan.FromSeconds(1))
                    .TakeUntil(switches.Where(on => !on)));

            var readWriteOperations = Observable
                .Interval(TimeSpan.FromSeconds(0.1))
                .Where(_ => Console.KeyAvailable)
                .Select(_ => Console.ReadKey().KeyChar)
                .Select(x => x == 't')
                .Multicast(new BehaviorSubject<bool>(false))
                .RefCount();

            var ops = readWriteOperations
                .DistinctUntilChanged()
                .Where(on => on)
                .SelectMany(Observable
                    .Interval(TimeSpan.FromSeconds(0.1))
                    .TakeUntil(readWriteOperations.Where(on => !on)));

            var anyActivityWasNotNoticed = ops
                .Window(TimeSpan.FromSeconds(1))
                .SelectMany(x => x.Any())
                .Where(x => !x);

            anyActivityWasNotNoticed.Timestamp()
                .Subscribe(x => Console.WriteLine(x.Timestamp));

            Thread.Sleep(Timeout.Infinite);

            //try
            //{
            //    var options = new CommandLineOptions();
            //    if (Parser.Default.ParseArguments(args, options))
            //    {
            //        var endPoints = ConvertToEndPoints(options.Endpoints);
            //        Task.Run(() => StartMainLoop(endPoints)).Wait();
            //    }
            //}
            //catch (AggregateException ex)
            //{
            //    Console.WriteLine(ex.InnerException.Message);
            //    Console.ReadKey();
            //    return 1;
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex.Message);
            //    Console.ReadKey();
            //    return 1;
            //}

            //Console.ReadKey();
            return 0;
        }

        private static IPEndPoint[] ConvertToEndPoints(string[] endPoints)
        {
            return endPoints.Select(endPoint => Regex.Match(endPoint, "(?<ipAddress>\\d{1,3}.\\d{1,3}.\\d{1,3}.\\d{1,3})\\s*:\\s*(?<port>\\d{1,5})"))
                .Select(match => new IPEndPoint(IPAddress.Parse(match.Groups["ipAddress"].Value), int.Parse(match.Groups["port"].Value)))
                .ToArray();
        }

        private static async Task StartMainLoop(IPEndPoint[] endPoints)
        {
            var behavior = new SimpleConnectionBehaviour(endPoints);
            var client = new TcpClientImpl(behavior);
            client.EstablishConnection(behavior.CurrentServerIPEndPoint);
            while (true)
            {
                Console.WriteLine("Enter a message");
                var textMessage = Console.ReadLine();
                await client.WriteMessageAsync(textMessage).ConfigureAwait(false);
            }
        }
    }
}
