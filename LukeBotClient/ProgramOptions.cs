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

        [Option("disable-ssl",
            HelpText = "Disable the use of SSL for connection. Production server will refuse a connection like that, use only for development/debug.",
            Default = false)]
        public bool DisableSSL { get; set; }

        public ProgramOptions()
        {
            Address = Constants.SERVER_DEFAULT_ADDRESS;
            Port = Constants.SERVER_DEFAULT_PORT;
            DisableSSL = false;
        }
    }
}