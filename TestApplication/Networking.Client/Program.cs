namespace Networking.Client
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using CommandLine;

    public class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                var options = new CommandLineOptions();
                if (Parser.Default.ParseArguments(args, options))
                {
                    var endPoints = ConvertToEndPoints(options.Endpoints);
                    Task.Run(() => StartMainLoop(endPoints)).Wait();
                }
            }
            catch (AggregateException ex)
            {
                Console.WriteLine(ex.InnerException.Message);
                Console.ReadKey();
                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadKey();
                return 1;
            }

            Console.ReadKey();
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
            var client = new TcpClientObservable(endPoints);
            client.Messages.Subscribe(Console.WriteLine, Console.WriteLine, () => Console.WriteLine("Completed"));
            while (true)
            {
                Console.WriteLine("Enter a message");
                var textMessage = Console.ReadLine();
                await client.WriteMessageAsync(textMessage, CancellationToken.None);
            }
        }
    }
}
