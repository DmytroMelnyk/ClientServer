namespace Networking.Client
{
    using CommandLine;
    using CommandLine.Text;

    public class CommandLineOptions
    {
        [OptionArray('e', "endpoints", Required = true, HelpText = "List of known endpoints. E.g.: \"127.0.0.1:50000\" \"127.0.0.1:50001\"")]
        public string[] Endpoints { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}
