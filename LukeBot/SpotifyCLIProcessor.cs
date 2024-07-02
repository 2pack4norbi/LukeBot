using System;
using System.Collections.Generic;
using LukeBot.Common;
using LukeBot.Config;
using LukeBot.Globals;
using LukeBot.Interface;
using LukeBot.Module;
using CommandLine;


namespace LukeBot
{
    [Verb("login", HelpText = "Set login to Spotify servers. This will invalidate current auth token if it exists.")]
    public class SpotifyLoginSubverb
    {
        [Value(0, MetaName = "login", Required = true, HelpText = "Spotify login subverb")]
        public string Login { get; set; }

        public SpotifyLoginSubverb()
        {
            Login = "";
        }
    }

    [Verb("enable", HelpText = "Enable Spotify module")]
    public class SpotifyEnableSubverb
    {
    }

    [Verb("disable", HelpText = "Disable Spotify module")]
    public class SpotifyDisableSubverb
    {
    }

    internal class SpotifyCLIProcessor: ICLIProcessor
    {
        private LukeBot mLukeBot;

        private void CheckForLogin(CLIMessageProxy CLI)
        {
            Path path = Path.Start()
                .Push(Constants.PROP_STORE_USER_DOMAIN)
                .Push(CLI.GetCurrentUser())
                .Push(Constants.SPOTIFY_MODULE_NAME)
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

        private void HandleLoginSubverb(SpotifyLoginSubverb arg, CLIMessageProxy CLI, out string result)
        {
            result = "";

            try
            {
                GlobalModules.Spotify.UpdateLoginForUser(mLukeBot.GetUser(CLI.GetCurrentUser()).Username, arg.Login);
                result = "Successfully updated Spotify login.";
            }
            catch (System.Exception e)
            {
                result = "Failed to update Spotify login: " + e.Message;
            }
        }

        public void HandleEnableSubverb(SpotifyEnableSubverb arg, CLIMessageProxy CLI, out string msg)
        {
            msg = "";

            try
            {
                CheckForLogin(CLI);
                mLukeBot.GetUser(CLI.GetCurrentUser()).EnableModule(ModuleType.Spotify);
                msg = "Enabled module " + ModuleType.Spotify;
            }
            catch (System.Exception e)
            {
                msg = "Failed to enable Spotify module: " + e.Message;
            }
        }

        public void HandleDisableSubverb(SpotifyDisableSubverb arg, CLIMessageProxy CLI, out string msg)
        {
            msg = "";

            try
            {
                mLukeBot.GetUser(CLI.GetCurrentUser()).DisableModule(ModuleType.Spotify);
                msg = "Disabled module " + ModuleType.Spotify;
            }
            catch (System.Exception e)
            {
                msg = "Failed to disable Spotify module: " + e.Message;
            }
        }

        public void AddCLICommands(LukeBot lb)
        {
            mLukeBot = lb;

            UserInterface.CLI.AddCommand(Constants.SPOTIFY_MODULE_NAME, UserPermissionLevel.User, (CLIMessageProxy cliProxy, string[] args) =>
            {
                string result = "";
                Parser.Default.ParseArguments<SpotifyLoginSubverb, SpotifyEnableSubverb, SpotifyDisableSubverb>(args)
                    .WithParsed<SpotifyLoginSubverb>((SpotifyLoginSubverb arg) => HandleLoginSubverb(arg, cliProxy, out result))
                    .WithParsed<SpotifyEnableSubverb>((SpotifyEnableSubverb arg) => HandleEnableSubverb(arg, cliProxy, out result))
                    .WithParsed<SpotifyDisableSubverb>((SpotifyDisableSubverb arg) => HandleDisableSubverb(arg, cliProxy, out result))
                    .WithNotParsed((IEnumerable<Error> errs) => CLIUtils.HandleCLIError(errs, Constants.SPOTIFY_MODULE_NAME, out result));
                return result;
            });
        }
    }
}