namespace LukeBot.Interface
{
    internal class LambdaCommand: Command
    {
        private CLIBase.CmdDelegate mDelegate;

        public LambdaCommand(CLIBase.CmdDelegate d)
        {
            mDelegate = d;
        }

        public string Execute(string[] args)
        {
            return mDelegate(args);
        }
    }
}