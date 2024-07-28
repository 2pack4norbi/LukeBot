using System;
using System.Collections.Generic;
using LukeBot.Globals;
using LukeBot.Twitch.Common.Command;
using Command = LukeBot.Twitch.Common.Command;
using CommandLine;


namespace LukeBot
{
    [Verb("add", HelpText = "Add command for user")]
    public class TwitchAddCommand
    {
        [Value(0, MetaName = "name", Required = true, HelpText = "Name of command to add.")]
        public string Name { get; set; }

        [Value(1, MetaName = "type", Required = true, HelpText =
            "Type of command to add. Available command types: " +
            "  print, shoutout, addcom, editcom, delcom, counter, songrequest"
        )]
        public Command::Type Type { get; set; }

        [Value(2, MetaName = "value", Required = false, HelpText = "Value for the command.")]
        public IEnumerable<string> Value { get; set; }

        public TwitchAddCommand()
        {
            Name = "";
            Type = Command::Type.print;
        }
    }

    [Verb("delete", HelpText = "Delete command for user")]
    public class TwitchDeleteCommand
    {
        [Value(0, MetaName = "name", Required = true, HelpText = "Name of command to delete")]
        public string Name { get; set; }

        public TwitchDeleteCommand()
        {
            Name = "";
        }
    }

    [Verb("edit", HelpText = "Edit existing Twitch command")]
    public class TwitchEditCommand
    {
        [Value(0, MetaName = "name", Required = true, HelpText = "Name of command to delete")]
        public string Name { get; set; }

        [Value(1, MetaName = "new_value", Required = true, HelpText = "New value to set to command")]
        public IEnumerable<string> Value { get; set; }

        public TwitchEditCommand()
        {
            Name = "";
        }
    }

    [Verb("list", HelpText = "List available Twitch commands for selected user")]
    public class TwitchListCommand
    {
        public TwitchListCommand()
        {
        }
    }

    [Verb("modify", HelpText = "Modify command's parameters")]
    public class TwitchModifyCommand
    {
        [Value(0, MetaName = "name", Required = true, HelpText = "Name of command to modify")]
        public string Name { get; set; }

        [Option('a', "allow", SetName = "allow", HelpText =
            "Allow selected users to execute command. Available user groups are:\n" +
            "  Broadcaster, Moderator, VIP, Subscriber, Chatter\n" +
            "\n" +
            "Above groups must be provided in a comma-separated list, case-agnostic, without spaces.\n" +
            "Command also accepts \"Everyone\" (case-agnostic) as an alias of all groups above at once.\n" +
            "\n" +
            "It is possible to provide only first letters of a group.\n"
        )]
        public string Allowed { get; set; }

        [Option('d', "deny", SetName = "deny", HelpText =
            "Deny selected users to execute command. Available user groups are:\n" +
            "  Broadcaster, Moderator, VIP, Subscriber, Chatter\n" +
            "\n" +
            "Above groups must be provided in a comma-separated list, case-agnostic, without spaces.\n" +
            "Command also accepts \"Everyone\" (case-agnostic) as an alias of all groups above at once.\n" +
            "\n" +
            "It is possible to provide only first letters of a group.\n"
        )]
        public string Denied { get; set; }

        [Option('e', "enabled", HelpText =
            "Set if command is enabled. This decides whether LukeBot will respond to a command in chat."
        )]
        public bool? Enabled { get; set; }

        [Option('l', "list", SetName = "list", HelpText = "List current command modifiers")]
        public bool List { get; set; }

        public TwitchModifyCommand()
        {
        }
    }

    internal class TwitchCommandCLIProcessor
    {
        private LukeBot mLukeBot;

        internal TwitchCommandCLIProcessor(LukeBot lb)
        {
            mLukeBot = lb;
        }

        public void HandleAddcom(TwitchAddCommand cmd, CLIMessageProxy CLI, out string msg)
        {
            try
            {
                string lbUser = CLI.GetCurrentUser();

                Twitch.Command.ICommand twCmd = GlobalModules.Twitch.AllocateCommand(lbUser, cmd.Name, cmd.Type, string.Join(' ', cmd.Value));
                if (twCmd == null)
                {
                    msg = "Invalid command type";
                    return;
                }

                GlobalModules.Twitch.AddCommandToChannel(lbUser, cmd.Name, twCmd);
            }
            catch (System.Exception e)
            {
                msg = "Failed to add command " + cmd.Name + ": " + e.Message;
                return;
            }

            msg = "Added " + cmd.Type + " twitch command " + cmd.Name;
        }

        public void HandleDelcom(TwitchDeleteCommand cmd, CLIMessageProxy CLI, out string msg)
        {
            try
            {
                string lbUser = CLI.GetCurrentUser();
                GlobalModules.Twitch.DeleteCommandFromChannel(lbUser, cmd.Name);
            }
            catch (System.Exception e)
            {
                msg = "Failed to delete command " + cmd.Name + ": " + e.Message;
                return;
            }

            msg = "Twitch command " + cmd.Name + " deleted.";
        }

        public void HandleEditcom(TwitchEditCommand cmd, CLIMessageProxy CLI, out string msg)
        {
            try
            {
                string lbUser = CLI.GetCurrentUser();
                GlobalModules.Twitch.EditCommandFromChannel(lbUser, cmd.Name, string.Join(' ', cmd.Value));
            }
            catch (System.Exception e)
            {
                msg = "Failed to edit command " + cmd.Name + ": " + e.Message;
                return;
            }

            msg = "Edited twitch command " + cmd.Name;
        }

        public void HandleListcom(TwitchListCommand cmd, CLIMessageProxy CLI, out string msg)
        {
            try
            {
                string lbUser = CLI.GetCurrentUser();

                msg = "Available commands:\n";

                List<Command::Descriptor> cmds = GlobalModules.Twitch.GetCommandDescriptors(lbUser);

                foreach (Command::Descriptor c in cmds)
                {
                    msg += String.Format("  {0} ({1})\n", c.Name, c.Type.ToString());
                }
            }
            catch (System.Exception e)
            {
                msg = "Failed to fetch available commands: " + e.Message;
            }
        }

        public void HandleModcom(TwitchModifyCommand cmd, CLIMessageProxy CLI, out string msg)
        {
            try
            {
                string lbUser = CLI.GetCurrentUser();

                if (cmd.List)
                {
                    Command::Descriptor d = GlobalModules.Twitch.GetCommandDescriptor(lbUser, cmd.Name);

                    msg = "Modifiers of Twitch command " + cmd.Name + ":\n";
                    msg += "  Privileges: " + d.Privilege.GetStringRepresentation();
                    return;
                }
                else if (cmd.Allowed != null && cmd.Allowed.Length > 0)
                {
                    Command::User priv = cmd.Allowed.ToUserEnum();
                    if (priv == 0)
                    {
                        msg = "Invalid privilege list: " + cmd.Allowed;
                        return;
                    }

                    GlobalModules.Twitch.AllowPrivilegeInCommand(lbUser, cmd.Name, priv);
                    msg = "Command " + cmd.Name + " modified";
                    return;
                }
                else if (cmd.Denied != null && cmd.Denied.Length > 0)
                {
                    Command::User priv = cmd.Denied.ToUserEnum();
                    if (priv == 0)
                    {
                        msg = "Invalid privilege list: " + cmd.Allowed;
                        return;
                    }

                    GlobalModules.Twitch.DenyPrivilegeInCommand(lbUser, cmd.Name, priv);
                    msg = "Command " + cmd.Name + " modified";
                    return;
                }
                else if (cmd.Enabled != null)
                {
                    GlobalModules.Twitch.SetCommandEnabled(lbUser, cmd.Name, (bool)cmd.Enabled);
                    msg = "Command " + cmd.Name + " " + ((bool)cmd.Enabled ? "enabled" : "disabled");
                }
                else
                {
                    msg = "No action provided";
                }
            }
            catch (System.Exception e)
            {
                msg = "Failed - " + e.Message;
            }
        }

        public string Parse(CLIMessageProxy CLI, string[] args)
        {
            string result = "";
            Parser p = new Parser(with => with.HelpWriter = new CLIUtils.CLIMessageProxyTextWriter(CLI));
            p.ParseArguments<TwitchAddCommand, TwitchDeleteCommand, TwitchEditCommand,
                    TwitchListCommand, TwitchModifyCommand>(args)
                .WithParsed<TwitchAddCommand>((TwitchAddCommand arg) => HandleAddcom(arg, CLI, out result))
                .WithParsed<TwitchDeleteCommand>((TwitchDeleteCommand arg) => HandleDelcom(arg, CLI, out result))
                .WithParsed<TwitchEditCommand>((TwitchEditCommand arg) => HandleEditcom(arg, CLI, out result))
                .WithParsed<TwitchListCommand>((TwitchListCommand arg) => HandleListcom(arg, CLI, out result))
                .WithParsed<TwitchModifyCommand>((TwitchModifyCommand arg) => HandleModcom(arg, CLI, out result))
                .WithNotParsed((IEnumerable<Error> errs) => CLIUtils.HandleCLIError(errs, "twitch-command", out result));
            return result;
        }
    }
}