using LukeBot.Common;
using LukeBot.Logging;
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

                Logger.Log().Error("ERROR: {0}", e.Tag);
            }
        }

        static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {
            FileUtils.SetUnifiedCWD();
            Logger.SetProjectRootDir(Directory.GetCurrentDirectory());

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
                Logger.Log().Error("Caught exception: " + e.Message + "\n" + e.StackTrace);
            }
        }
    }
}
