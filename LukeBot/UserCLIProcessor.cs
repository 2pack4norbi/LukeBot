using System.Collections.Generic;
using LukeBot.Globals;
using LukeBot.Interface;
using CommandLine;

namespace LukeBot
{
    [Verb("add", HelpText = "Add user")]
    internal class UserAddCommand
    {
        [Value(0, MetaName = "username", Required = true, HelpText = "Name of user to add")]
        public string Name { get; set; }

        public UserAddCommand()
        {
            Name = "";
        }
    }

    [Verb("list", HelpText = "List available users")]
    internal class UserListCommand
    {
    }

    [Verb("remove", HelpText = "Remove user")]
    internal class UserRemoveCommand
    {
        [Value(0, MetaName = "username", Required = true, HelpText = "Name of user to remove")]
        public string Name { get; set; }

        public UserRemoveCommand()
        {
            Name = "";
        }
    }

    [Verb("select", HelpText = "Select user for further commands")]
    internal class UserSelectCommand
    {
        [Value(0, MetaName = "username", Required = false, Default = "", HelpText = "Name of user to select. Leave empty to deselect.")]
        public string Name { get; set; }

        public UserSelectCommand()
        {
            Name = "";
        }
    }

    [Verb("password", HelpText = "Set a password for current or selected user")]
    internal class UserPasswordCommand
    {
        [Value(0, MetaName = "username", Required = false, Default = "", HelpText = "Name of user to change password for")]
        public string Name { get; set; }

        public UserPasswordCommand()
        {
            Name = "";
        }
    }

    [Verb("update", HelpText = "Update user profile settings.")]
    internal class UserUpdateCommand
    {
        [Value(0, MetaName = "username", Required = false, Default = "", HelpText = "Name of user to change password for. Can be omitted to affect selected user.")]
        public string Name { get; set; }

        [Option('p', "permission", SetName = "permission", HelpText =
            "Set permission level for user. Available levels:\n" +
            "  - User\n" +
            "  - Admin\n")]
        public UserPermissionLevel PermissionLevel { get; set; }

        public UserUpdateCommand()
        {
            Name = "";
            PermissionLevel = UserPermissionLevel.None;
        }
    }

    internal class UserCLIProcessor: ICLIProcessor
    {
        private const string COMMAND_NAME = "user";
        private LukeBot mLukeBot;
        private CLIMessageProxy mCLI;

        void HandleAddUserCommand(UserAddCommand args, out string msg)
        {
            try
            {
                mLukeBot.AddUser(args.Name);
                msg = "User " + args.Name + " added successfully";
            }
            catch (System.Exception e)
            {
                msg = "Failed to add user " + args.Name + ": " + e.Message;
            }
        }

        void HandleListUsersCommand(UserListCommand args, out string msg)
        {
            msg = "Available users:";

            List<string> usernames = mLukeBot.GetUsernames();
            foreach (string u in usernames)
            {
                msg += "\n  " + u;
            }
        }

        void HandleRemoveUserCommand(UserRemoveCommand args, out string msg)
        {
            if (!mCLI.Ask("Are you sure you want to remove user " + args.Name + "? This will remove all associated data!"))
            {
                msg = "User removal aborted";
                return;
            }

            try
            {
                mLukeBot.RemoveUser(args.Name);
                msg = "User " + args.Name + " removed.";
            }
            catch (System.Exception e)
            {
                msg = "Failed to remove user " + args.Name + ": " + e.Message;
            }
        }

        void HandleSelectUserCommand(UserSelectCommand args, out string msg)
        {
            try
            {
                mLukeBot.SelectUser(args.Name);

                if (mLukeBot.GetCurrentUser() == null)
                {
                    msg = "Cleared selected user";
                }
                else
                {
                    msg = "Selected user " + mLukeBot.GetCurrentUser().Username;
                }
            }
            catch (System.Exception e)
            {
                msg = "Failed to select user " + args.Name + ": " + e.Message;
            }
        }

        void HandlePasswordUserCommand(UserPasswordCommand args, out string msg)
        {
            try
            {
                UserContext user;
                if (args.Name == null || args.Name.Length == 0)
                    user = mLukeBot.GetCurrentUser();
                else
                    user = mLukeBot.GetUser(args.Name);

                string newPwd = mCLI.Query(true, "New password");
                string newPwdRepeat = mCLI.Query(true, "Repeat new password");

                if (newPwd != newPwdRepeat)
                {
                    msg = "New passwords do not match";
                    return;
                }

                user.SetPassword(newPwd);
                msg = "Password changed";
            }
            catch (System.Exception e)
            {
                msg = "Failed to change password: " + e.Message;
            }
        }

        void HandleUpdateUserCommand(UserUpdateCommand args, out string msg)
        {
            try
            {
                UserContext user;
                if (args.Name == null || args.Name.Length == 0)
                    user = mLukeBot.GetCurrentUser();
                else
                    user = mLukeBot.GetUser(args.Name);

                if (args.PermissionLevel != UserPermissionLevel.None)
                {
                    user.SetPermissionLevel(args.PermissionLevel);
                    mCLI.Message("Permission level set to " + args.PermissionLevel.ToString());
                }

                msg = "Changes to user " + user.Username + " applied.";
            }
            catch (System.Exception e)
            {
                msg = "Failed to update user: " + e.Message;
            }
        }

        public void AddCLICommands(LukeBot lb)
        {
            mLukeBot = lb;

            UserInterface.CLI.AddCommand(COMMAND_NAME, UserPermissionLevel.Admin, (CLIMessageProxy cliProxy, string[] args) =>
            {
                mCLI = cliProxy;

                string result = "";
                Parser.Default.ParseArguments<UserAddCommand, UserListCommand, UserRemoveCommand, UserSelectCommand, UserPasswordCommand, UserUpdateCommand>(args)
                    .WithParsed<UserAddCommand>((UserAddCommand args) => HandleAddUserCommand(args, out result))
                    .WithParsed<UserListCommand>((UserListCommand args) => HandleListUsersCommand(args, out result))
                    .WithParsed<UserRemoveCommand>((UserRemoveCommand args) => HandleRemoveUserCommand(args, out result))
                    .WithParsed<UserSelectCommand>((UserSelectCommand args) => HandleSelectUserCommand(args, out result))
                    .WithParsed<UserPasswordCommand>((UserPasswordCommand args) => HandlePasswordUserCommand(args, out result))
                    .WithParsed<UserUpdateCommand>((UserUpdateCommand args) => HandleUpdateUserCommand(args, out result))
                    .WithNotParsed((IEnumerable<Error> errs) => CLIUtils.HandleCLIError(errs, COMMAND_NAME, out result));
                return result;
            });
        }
    }
}