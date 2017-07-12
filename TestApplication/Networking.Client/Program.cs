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

            var operations = Observable
                .Interval(TimeSpan.FromSeconds(0.1))
                .Where(_ => Console.KeyAvailable)
                .Select(_ => Console.ReadKey().KeyChar)
                .Multicast(new BehaviorSubject<char>(' '))
                .RefCount();

            var readOperations = operations
                .Where(x => x == 'r' || x == 'e')
                .Select(x => x == 'r')
                .Multicast(new BehaviorSubject<bool>(false))
                .RefCount();

            var rops = readOperations
                .DistinctUntilChanged()
                .Where(on => on)
                .SelectMany(Observable
                    .Interval(TimeSpan.FromSeconds(0.1))
                    .TakeUntil(readOperations.Where(on => !on)));

            var writeOperations = operations
                .Where(x => x == 'w' || x == 'q')
                .Select(x => x == 'w')
                .Multicast(new BehaviorSubject<bool>(false))
                .RefCount();

            var wops = writeOperations
                .DistinctUntilChanged()
                .Where(on => on)
                .SelectMany(Observable
                    .Interval(TimeSpan.FromSeconds(0.1))
                    .TakeUntil(writeOperations.Where(on => !on)));

            //rops.CombineLatest(wops, (x, y) => x + y).
            var anyActivityWasNotNoticed = rops.Merge(wops)
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
