namespace LukeBot
{
    internal abstract class Command
    {
        public UserPermissionLevel PermissionLevel { get; private set; }

        public Command(UserPermissionLevel permissionLevel)
        {
            PermissionLevel = permissionLevel;
        }

        public bool IsPermitted(UserPermissionLevel userLevel)
        {
            return userLevel >= PermissionLevel;
        }

        public abstract string Execute(CLIMessageProxy cliProxy, string[] args);
    }
}