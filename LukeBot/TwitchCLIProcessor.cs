using System;
using System.Collections.Generic;
using System.Linq;
using LukeBot.Common;
using LukeBot.Config;
using LukeBot.Globals;
using LukeBot.Interface;
using LukeBot.Module;
using CommandLine;


namespace LukeBot
{
    [Verb("command", HelpText = "Interact with Twitch Chat commands")]
    public class TwitchCommandSubverb
    {
    }

    [Verb("emote-refresh", HelpText = "Refreshes user's emotes. Use to reload emotes from third party providers (ex. FFZ) after adding new ones.")]
    public class TwitchEmoteRefreshSubverb
    {
    }

    [Verb("login", HelpText = "Set login to Twitch servers. This will invalidate current auth token if it exists.")]
    public class TwitchLoginSubverb
    {
    }

    [Verb("enable", HelpText = "Enable Twitch module")]
    public class TwitchEnableSubverb
    {
    }

    [Verb("disable", HelpText = "Disable Twitch module")]
    public class TwitchDisableSubverb
    {
    }

    internal class TwitchCLIProcessor: ICLIProcessor
    {
        private TwitchCommandCLIProcessor mCommandCLIProcessor;
        private LukeBot mLukeBot;

        private void CheckForLogin(CLIMessageProxy CLI)
        {
            Path path = Path.Start()
                .Push(Constants.PROP_STORE_USER_DOMAIN)
                .Push(CLI.GetCurrentUser())
                .Push(Constants.TWITCH_MODULE_NAME)
                .Push(Constants.PROP_STORE_LOGIN_PROP);

            if (!Conf.TryGet<string>(path, out string login))
            {
                login = CLI.Query(false, "Spotify login for user " + CLI.GetCurrentUser());
                if (login.Length == 0)
                {
                    throw new ArgumentException("No login provided");
                }

                Conf.Add(path, Property.Create<string>(login));
            }
        }

        private void HandleCommandSubverb(TwitchCommandSubverb arg, CLIMessageProxy CLI, string[] args, out string result)
        {
            result = mCommandCLIProcessor.Parse(CLI, args);
        }

        private void HandleEmoteRefreshSubverb(TwitchEmoteRefreshSubverb arg, CLIMessageProxy CLI, out string result)
        {
            try
            {
                GlobalModules.Twitch.RefreshEmotesForUser(CLI.GetCurrentUser());
                result = "Emotes refreshed";
            }
            catch (System.Exception e)
            {
                result = "Failed to refresh emotes: " + e.Message;
            }
        }

        private void HandleLoginSubverb(TwitchLoginSubverb arg, CLIMessageProxy CLI, string[] args, out string result)
        {
            result = "";

            if (args.Length != 1)
            {
                result = "Too many arguments - provide one argument being your Twitch login";
                return;
            }

            try
            {
                GlobalModules.Twitch.UpdateLoginForUser(CLI.GetCurrentUser(), args[0]);
                result = "Successfully updated Twitch login.";
            }
            catch (System.Exception e)
            {
                result = "Failed to update Twitch login: " + e.Message;
            }
        }

        public void HandleEnableSubverb(TwitchEnableSubverb arg, CLIMessageProxy CLI, out string msg)
        {
            msg = "";

            try
            {
                CheckForLogin(CLI);
                mLukeBot.GetUser(CLI.GetCurrentUser()).EnableModule(ModuleType.Twitch);
                msg = "Enabled module " + ModuleType.Twitch;
            }
            catch (System.Exception e)
            {
                msg = "Failed to enable Twitch module: " + e.Message;
            }
        }

        public void HandleDisableSubverb(TwitchDisableSubverb arg, CLIMessageProxy CLI, out string msg)
        {
            msg = "";

            try
            {
                mLukeBot.GetUser(CLI.GetCurrentUser()).DisableModule(ModuleType.Twitch);
                msg = "Disabled module " + ModuleType.Twitch;
            }
            catch (System.Exception e)
            {
                msg = "Failed to disable Twitch module: " + e.Message;
            }
        }

        public void AddCLICommands(LukeBot lb)
        {
            mLukeBot = lb;
            mCommandCLIProcessor = new TwitchCommandCLIProcessor(mLukeBot);

            UserInterface.CLI.AddCommand(Constants.TWITCH_MODULE_NAME, UserPermissionLevel.User, (CLIMessageProxy cliProxy, string[] args) =>
            {
                string result = "";
                string[] cmdArgs = args.Take(2).ToArray(); // filters out any additional options/commands that might confuse CommandLine
                Parser.Default.ParseArguments<TwitchCommandSubverb, TwitchEmoteRefreshSubverb, TwitchLoginSubverb, TwitchEnableSubverb, TwitchDisableSubverb>(cmdArgs)
                    .WithParsed<TwitchCommandSubverb>((TwitchCommandSubverb arg) => HandleCommandSubverb(arg, cliProxy, args.Skip(1).ToArray(), out result))
                    .WithParsed<TwitchEmoteRefreshSubverb>((TwitchEmoteRefreshSubverb arg) => HandleEmoteRefreshSubverb(arg, cliProxy, out result))
                    .WithParsed<TwitchLoginSubverb>((TwitchLoginSubverb arg) => HandleLoginSubverb(arg, cliProxy, args.Skip(1).ToArray(), out result))
                    .WithParsed<TwitchEnableSubverb>((TwitchEnableSubverb arg) => HandleEnableSubverb(arg, cliProxy, out result))
                    .WithParsed<TwitchDisableSubverb>((TwitchDisableSubverb arg) => HandleDisableSubverb(arg, cliProxy, out result))
                    .WithNotParsed((IEnumerable<Error> errs) => CLIUtils.HandleCLIError(errs, Constants.TWITCH_MODULE_NAME, out result));
                return result;
            });
        }
    }
}