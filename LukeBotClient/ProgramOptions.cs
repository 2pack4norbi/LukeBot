using LukeBot.Common;
using CommandLine;


namespace LukeBotClient
{
    internal class ProgramOptions
    {
        [Value(0,
            HelpText = "Provide IP address to connect to",
            Required = false,
            Default = LukeBot.Common.Constants.DEFAULT_SERVER_HTTPS_DOMAIN)]
        public string Address { get; set; }

        [Option('p', "port",
            HelpText = "Provide a custom port to connect to",
            Default = LukeBot.Common.Constants.SERVERCLI_DEFAULT_PORT)]
        public int Port { get; set; }

        public ProgramOptions()
        {
            Address = LukeBot.Common.Constants.DEFAULT_SERVER_HTTPS_DOMAIN;
            Port = LukeBot.Common.Constants.SERVERCLI_DEFAULT_PORT;
        }
    }
}