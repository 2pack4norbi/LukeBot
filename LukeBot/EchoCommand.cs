namespace LukeBot
{
    internal class EchoCommand: Command
    {
        public EchoCommand(UserPermissionLevel permissionLevel)
            : base(permissionLevel)
        {
        }

        public override string Execute(CLIMessageProxy cliProxy, string[] args)
        {
            return string.Join(' ', args);
        }
    }
}