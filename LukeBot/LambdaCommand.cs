namespace LukeBot
{
    internal class LambdaCommand: Command
    {
        private CLIBase.CmdDelegate mDelegate;

        public LambdaCommand(UserPermissionLevel permissionLevel, CLIBase.CmdDelegate d)
            : base(permissionLevel)
        {
            mDelegate = d;
        }

        public override string Execute(CLIMessageProxy cliProxy, string[] args)
        {
            return mDelegate(cliProxy, args);
        }
    }
}