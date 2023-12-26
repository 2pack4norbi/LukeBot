using LukeBot.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CommandLine;


namespace LukeBotClient
{
    class MainClass
    {
        public static ProgramOptions mProgOpts;

        public static void HandleProgramOptions(ProgramOptions opts)
        {
            mProgOpts = opts;
        }

        public static void HandleParsingError(IEnumerable<Error> errs)
        {
            foreach (Error e in errs)
            {
                if (e is HelpVerbRequestedError || e is HelpRequestedError)
                    continue;

                Console.WriteLine("ERROR: {0}", e.Tag);
            }
        }

        static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {
            FileUtils.SetUnifiedCWD();

            Parser.Default.ParseArguments<ProgramOptions>(args)
                .WithParsed<ProgramOptions>(HandleProgramOptions)
                .WithNotParsed(HandleParsingError);

            try
            {
                LukeBotClient client = new LukeBotClient(mProgOpts);
                await client.Run();
            }
            catch (System.Exception e)
            {
                Console.WriteLine("Caught exception: " + e.Message + "\n" + e.StackTrace);
            }
        }
    }
}
