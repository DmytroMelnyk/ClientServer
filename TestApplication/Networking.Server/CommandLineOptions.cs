namespace Networking.Server
{
    using CommandLine;
    using CommandLine.Text;

    public class CommandLineOptions
    {
        [Option('p', "port", Required = true, HelpText = "Port number which server will be listening at")]
        public int Port { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}
