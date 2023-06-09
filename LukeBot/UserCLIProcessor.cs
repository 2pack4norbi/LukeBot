using System.Collections.Generic;
using LukeBot.Globals;
using LukeBot.Interface;
using CommandLine;

namespace LukeBot
{
    [Verb("add", HelpText = "Add user")]
    public class UserAddCommand
    {
        [Value(0, MetaName = "username", Required = true, HelpText = "Name of user to add")]
        public string Name { get; set; }

        public UserAddCommand()
        {
            Name = "";
        }
    }

    [Verb("list", HelpText = "List available users")]
    public class UserListCommand
    {
    }

    [Verb("remove", HelpText = "Remove user")]
    public class UserRemoveCommand
    {
        [Value(0, MetaName = "username", Required = true, HelpText = "Name of user to remove")]
        public string Name { get; set; }

        public UserRemoveCommand()
        {
            Name = "";
        }
    }

    [Verb("select", HelpText = "Select user for further commands")]
    public class UserSelectCommand
    {
        [Value(0, MetaName = "username", Required = false, Default = "", HelpText = "Name of user to select. Leave empty to deselect.")]
        public string Name { get; set; }

        public UserSelectCommand()
        {
            Name = "";
        }
    }

    internal class UserCLIProcessor: ICLIProcessor
    {
        private const string COMMAND_NAME = "user";
        private LukeBot mLukeBot;

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
            if (!CLI.Instance.Ask("Are you sure you want to remove user " + args.Name + "? This will remove all associated data!"))
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

        public void AddCLICommands(LukeBot lb)
        {
            mLukeBot = lb;

            CLI.Instance.AddCommand(COMMAND_NAME, (string[] args) =>
            {
                string result = "";
                Parser.Default.ParseArguments<UserAddCommand, UserListCommand, UserRemoveCommand, UserSelectCommand>(args)
                    .WithParsed<UserAddCommand>((UserAddCommand args) => HandleAddUserCommand(args, out result))
                    .WithParsed<UserListCommand>((UserListCommand args) => HandleListUsersCommand(args, out result))
                    .WithParsed<UserRemoveCommand>((UserRemoveCommand args) => HandleRemoveUserCommand(args, out result))
                    .WithParsed<UserSelectCommand>((UserSelectCommand args) => HandleSelectUserCommand(args, out result))
                    .WithNotParsed((IEnumerable<Error> errs) => CLIUtils.HandleCLIError(errs, COMMAND_NAME, out result));
                return result;
            });
        }
    }
}