namespace LukeBot
{
    internal interface CLIBase
    {
        public delegate string CmdDelegate(CLIMessageProxy cliProxy, string[] args);

        void MainLoop();
        void Teardown();
        void AddCommand(string cmd, Command c);
        void AddCommand(string cmd, UserPermissionLevel permissionLevel, CmdDelegate d);
    }
}