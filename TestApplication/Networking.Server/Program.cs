namespace Networking.Server
{
    using System;
    using System.Net;
    using CommandLine;
    using ConnectionBehavior;

    public class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                var options = new CommandLineOptions();
                if (Parser.Default.ParseArguments(args, options))
                {
                    var ipEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), options.Port);
                    new TcpServerImpl(ipEndPoint, new SimpleConnectionBehaviour()).StartListening();
                }
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
    }
}
