using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Networking.Core;

namespace Networking.Client
{
    class Program
    {
        static IPEndPoint[] ConvertToEndPoints(string[] endPoints)
        {
            return endPoints.Select(endPoint => Regex.Match(endPoint, "(?<ipAddress>.+):(?<port>\\d+)"))
                .Select(match => new IPEndPoint(IPAddress.Parse(match.Groups["ipAddress"].Value), int.Parse(match.Groups["port"].Value)))
                .ToArray();
        }

        static void Main(string[] args)
        {
            var endPoints = ConvertToEndPoints(args);
            Task.Run(async () => await StartMainLoop(endPoints)).Wait();
        }

        static async Task StartMainLoop(IPEndPoint[] endPoints)
        {
            var client = new TcpClientImpl(endPoints);
            await client.ConnectAsync().ConfigureAwait(false);
            client.StartReadingMessagesAsync();
            while (true)
            {
                Console.WriteLine("Enter a message");
                var textMessage = ToTextMessage(Console.ReadLine());
                await client.WriteMessageAsync(textMessage).ConfigureAwait(false);
            }
        }

        static IMessage ToTextMessage(string textMessage)
        {
            throw new NotImplementedException();
        }
    }
}
