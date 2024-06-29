namespace LukeBot
{
    internal abstract class Command
    {
        public UserPermissionLevel PermissionLevel { get; private set; }

        public Command(UserPermissionLevel permissionLevel)
        {
            PermissionLevel = permissionLevel;
        }

        public abstract string Execute(CLIMessageProxy cliProxy, string[] args);
    }
}