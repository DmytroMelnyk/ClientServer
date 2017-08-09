namespace Networking.Server
{
    using System;
    using System.Net;
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
                    var ipEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), options.Port);
                    using (new TcpServerObservable(new TcpListenerObservable(ipEndPoint)))
                    {
                        Console.WriteLine("Press any key to stop server...");
                        Console.ReadLine();
                    }
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
