using System;
using System.Net;
using Networking.Core;

namespace Networking.Client
{
    class Program
    {
        static IPEndPoint[] ConvertToEndPoints(string[] endPoints)
        {
            throw new NotImplementedException();
        }

        static void Main(string[] args)
        {
            var endPoints = ConvertToEndPoints(args);
            var client = new TcpClientImpl(endPoints);
            client.StartReadingMessagesAsync();
            StartMainLoop(client);
        }

        static async void  StartMainLoop(TcpClientImpl client)
        {
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
