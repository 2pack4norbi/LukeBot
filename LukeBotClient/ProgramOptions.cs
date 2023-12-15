using LukeBot.Common;
using CommandLine;


namespace LukeBotClient
{
    internal class ProgramOptions
    {
        [Option('a', "address",
            HelpText = "Provide IP address to connect to",
            Default = Constants.SERVER_DEFAULT_ADDRESS)]
        public string Address { get; set; }

        [Option('p', "port",
            HelpText = "Provide a custom port to connect to",
            Default = Constants.SERVER_DEFAULT_PORT)]
        public int Port { get; set; }

        public ProgramOptions()
        {
            Address = Constants.SERVER_DEFAULT_ADDRESS;
            Port = Constants.SERVER_DEFAULT_PORT;
        }
    }
}