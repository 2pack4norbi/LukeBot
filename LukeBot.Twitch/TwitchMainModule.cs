using System;
using System.Collections.Generic;
using LukeBot.API;
using LukeBot.Communication;
using LukeBot.Config;
using LukeBot.Interface;
using LukeBot.Logging;
using LukeBot.Module;
using LukeBot.Twitch.Common;

using CommonConstants = LukeBot.Common.Constants;
using CommonUtils = LukeBot.Common.Utils;
using Command = LukeBot.Twitch.Common.Command;
using Intercom = LukeBot.Communication.Common.Intercom;


namespace LukeBot.Twitch
{
    public class TwitchMainModule: IMainModule
    {
        private string mBotLogin;
        private Token mToken;
        private TwitchIRC mIRC;
        private API.Twitch.GetUserResponse mBotData;
        private Dictionary<string, TwitchUserModule> mUsers;


        // Config interactions //

        private Path GetCommandCollectionPropertyName(string lbUser)
        {
            return Path.Start()
                .Push(CommonConstants.PROP_STORE_USER_DOMAIN)
                .Push(lbUser)
                .Push(CommonConstants.TWITCH_MODULE_NAME)
                .Push(Constants.PROP_TWITCH_COMMANDS);
        }

        private string GetTwitchChannel(string lbUser)
        {
            return Conf.Get<string>(Path.Start()
                .Push(CommonConstants.PROP_STORE_USER_DOMAIN)
                .Push(lbUser)
                .Push(CommonConstants.TWITCH_MODULE_NAME)
                .Push(Constants.PROP_TWITCH_USER_LOGIN)
            );
        }

        private void UpdateCommandInConfig(string lbUser, string commandName)
        {
            Path cmdCollectionProp = GetCommandCollectionPropertyName(lbUser);

            Command::Descriptor[] commands = Conf.Get<Command::Descriptor[]>(cmdCollectionProp);

            int idx = Array.FindIndex<Command::Descriptor>(commands, (Command::Descriptor d) => d.Name == commandName);
            commands[idx] = GetCommandDescriptor(lbUser, commandName);
            Conf.Modify<Command::Descriptor[]>(cmdCollectionProp, commands);
        }

        private void LoadCommandsFromConfig(string lbUser)
        {
            Path cmdCollectionProp = GetCommandCollectionPropertyName(lbUser);

            Command::Descriptor[] commands;
            if (!Conf.TryGet<Command::Descriptor[]>(cmdCollectionProp, out commands))
                return; // quiet exit, assume user does not have any commands for Twitch chat

            string twitchChannel = GetTwitchChannel(lbUser);
            foreach (Command::Descriptor cmd in commands)
            {
                mIRC.AddCommandToChannel(twitchChannel, cmd.Name, AllocateCommand(lbUser, cmd));
            }
        }

        private void SaveCommandToConfig(string lbUser, string name, Command.ICommand cmd)
        {
            Command::Descriptor desc = cmd.ToDescriptor();

            Path cmdCollectionProp = GetCommandCollectionPropertyName(lbUser);
            ConfUtil.ArrayAppend(cmdCollectionProp, desc, new Command::DescriptorComparer());
        }

        private void RemoveCommandFromConfig(string lbUser, string name)
        {
            Path cmdCollectionProp = GetCommandCollectionPropertyName(lbUser);
            ConfUtil.ArrayRemove<Command::Descriptor>(cmdCollectionProp, (Command::Descriptor d) => d.Name != name);
        }


        // Intercom interactions //

        private void IntercomAddCommandDelegate(Intercom::MessageBase msg, ref Intercom::ResponseBase resp)
        {
            AddCommandIntercomMsg m = (AddCommandIntercomMsg)msg;

            try
            {
                AddCommandToChannel(m.User, m.Name, AllocateCommand(m.User, m.Name, m.Type, m.Param));
            }
            catch (System.Exception e)
            {
                resp.SignalError(e.Message);
                return;
            }

            resp.SignalSuccess();
        }

        private void IntercomEditCommandDelegate(Intercom::MessageBase msg, ref Intercom::ResponseBase resp)
        {
            EditCommandIntercomMsg m = (EditCommandIntercomMsg)msg;

            try
            {
                EditCommandFromChannel(m.User, m.Name, m.Param);
            }
            catch (System.Exception e)
            {
                resp.SignalError(e.Message);
                return;
            }

            resp.SignalSuccess();
        }

        private void IntercomDeleteCommandDelegate(Intercom::MessageBase msg, ref Intercom::ResponseBase resp)
        {
            DeleteCommandIntercomMsg m = (DeleteCommandIntercomMsg)msg;

            try
            {
                DeleteCommandFromChannel(m.User, m.Name);
            }
            catch (System.Exception e)
            {
                resp.SignalError(e.Message);
                return;
            }

            resp.SignalSuccess();
        }


        // User Module Descriptor delegates //

        private bool UserModuleLoadPrerequisites(string lbUser)
        {
            Path userTwitchLoginProp = Path.Start()
                .Push(CommonConstants.PROP_STORE_USER_DOMAIN)
                .Push(lbUser)
                .Push(CommonConstants.TWITCH_MODULE_NAME)
                .Push(Constants.PROP_TWITCH_USER_LOGIN);

            string login;
            if (Conf.TryGet<string>(userTwitchLoginProp, out login))
                return true; // quietly exit - login is already there, prerequisites are met

            // User login does not exist - query for it
            login = UserInterface.Instance.Query(false, "Twitch login for user " + lbUser);
            if (login.Length == 0)
            {
                UserInterface.Instance.Message("No login provided\n");
                return false;
            }

            Conf.Add(userTwitchLoginProp, Property.Create<string>(login));
            return true;
        }

        private IUserModule UserModuleLoader(string lbUser)
        {
            return JoinChannel(lbUser);
        }

        private void UserModuleUnloader(IUserModule module)
        {
            PartChannel(module as TwitchUserModule);
        }


        // Public methods //

        public TwitchMainModule()
        {
            Comms.Communication.Register(CommonConstants.TWITCH_MODULE_NAME);

            mBotLogin = Conf.Get<string>(Path.Start()
                .Push(CommonConstants.TWITCH_MODULE_NAME)
                .Push(Constants.PROP_TWITCH_USER_LOGIN)
            );

            if (mBotLogin == CommonConstants.DEFAULT_LOGIN_NAME)
            {
                throw new PropertyFileInvalidException("Bot's Twitch login has not been provided in Property Store");
            }

            string tokenScope = "chat:read chat:edit user:read:email"; // TODO should also be from Config...
            mToken = AuthManager.Instance.GetToken(ServiceType.Twitch, mBotLogin);

            bool tokenFromFile = mToken.Loaded;
            if (!mToken.Loaded)
                mToken.Request(tokenScope);

            if (!Utils.IsLoginSuccessful(mToken))
            {
                throw new InvalidOperationException("Failed to login to Twitch");
            }

            mBotData = API.Twitch.GetUser(mToken);
            mIRC = new TwitchIRC(mBotLogin, mToken);
            mUsers = new Dictionary<string, TwitchUserModule>();

            Intercom::EndpointInfo epInfo = new Intercom::EndpointInfo(Endpoints.TWITCH_MAIN_MODULE);
            epInfo.AddMessage(Messages.ADD_COMMAND, IntercomAddCommandDelegate);
            epInfo.AddMessage(Messages.EDIT_COMMAND, IntercomEditCommandDelegate);
            epInfo.AddMessage(Messages.DELETE_COMMAND, IntercomDeleteCommandDelegate);
            Comms.Intercom.Register(epInfo);
        }

        public TwitchUserModule JoinChannel(string lbUser)
        {
            string channel = Conf.Get<string>(Path.Start()
                .Push(CommonConstants.PROP_STORE_USER_DOMAIN)
                .Push(lbUser)
                .Push(CommonConstants.TWITCH_MODULE_NAME)
                .Push(Constants.PROP_TWITCH_USER_LOGIN)
            );

            if (mUsers.ContainsKey(channel))
            {
                throw new ChannelAlreadyJoinedException(channel);
            }

            Logger.Log().Debug("Joining channel {0}", channel);

            TwitchUserModule user = new TwitchUserModule(lbUser, mToken, channel);

            try
            {
                // TODO this user token should not be here. In general, this IRC channel
                // should be probably rewritten and IRCChannel should be owned/managed by
                // TwitchUserModule. Come back to this someday and make this right.
                mIRC.JoinChannel(lbUser, user.GetUserData(), user.GetUserToken());

                LoadCommandsFromConfig(lbUser);

                mUsers.Add(channel, user);
            }
            catch (System.Exception)
            {
                // get rid of our just created user module, as we might've already started something
                user.RequestShutdown();
                user.WaitForShutdown();
                user = null;
                throw;
            }

            Logger.Log().Secure("Joined channel twitch ID: {0}", user.GetUserData().id);
            return user;
        }

        public void PartChannel(TwitchUserModule module)
        {
            Logger.Log().Debug("Parting channel {0}", module.GetChannelName());

            mIRC.PartChannel(module.GetUserData());

            mUsers.Remove(module.GetChannelName());

            Logger.Log().Secure("Parted channel twitch ID: {0}", module.GetUserData().id);
        }

        public void AddCommandToChannel(string lbUser, string commandName, Command.ICommand command)
        {
            string twitchChannel = GetTwitchChannel(lbUser);
            mIRC.AddCommandToChannel(twitchChannel, commandName, command);
            SaveCommandToConfig(lbUser, commandName, command);
        }

        public Twitch.Command.ICommand AllocateCommand(string lbUser, Command::Descriptor d)
        {
            Twitch.Command.ICommand cmd = null;

            switch (d.Type)
            {
            case Command::Type.print: cmd = new Command.Print(d); break;
            case Command::Type.shoutout: cmd = new Command.Shoutout(d); break;
            case Command::Type.addcom: cmd = new Command.AddCommand(d, lbUser); break;
            case Command::Type.editcom: cmd = new Command.EditCommand(d, lbUser); break;
            case Command::Type.delcom: cmd = new Command.DeleteCommand(d, lbUser); break;
            case Command::Type.counter: cmd = new Command.Counter(d); break;
            case Command::Type.songrequest: cmd = new Command.SongRequest(d, lbUser); break;
            default: return null;
            }

            cmd.SetUpdateConfigDelegate((string name) => UpdateCommandInConfig(lbUser, name));
            return cmd;
        }

        public Twitch.Command.ICommand AllocateCommand(string lbUser, string name, Command::Type type, string value)
        {
            return AllocateCommand(lbUser, new Command::Descriptor(name, type, value));
        }

        public void AwaitIRCLoggedIn(int timeoutMs)
        {
            mIRC.AwaitLoggedIn(timeoutMs);
        }

        public void DeleteCommandFromChannel(string lbUser, string commandName)
        {
            string twitchChannel = GetTwitchChannel(lbUser);
            mIRC.DeleteCommandFromChannel(twitchChannel, commandName);
            RemoveCommandFromConfig(lbUser, commandName);
        }

        public void EditCommandFromChannel(string lbUser, string commandName, string newValue)
        {
            string twitchChannel = GetTwitchChannel(lbUser);
            mIRC.EditCommandFromChannel(twitchChannel, commandName, newValue);
            UpdateCommandInConfig(lbUser, commandName);
        }

        public List<Command::Descriptor> GetCommandDescriptors(string lbUser)
        {
            string twitchChannel = GetTwitchChannel(lbUser);
            return mIRC.GetCommandDescriptors(twitchChannel);
        }

        public Command::Descriptor GetCommandDescriptor(string lbUser, string name)
        {
            string twitchChannel = GetTwitchChannel(lbUser);
            return mIRC.GetCommandDescriptor(twitchChannel, name);
        }

        public void AllowPrivilegeInCommand(string lbUser, string name, Command::User privilege)
        {
            string twitchChannel = GetTwitchChannel(lbUser);
            mIRC.AllowPrivilegeInCommand(twitchChannel, name, privilege);
            UpdateCommandInConfig(twitchChannel, name);
        }

        public void DenyPrivilegeInCommand(string lbUser, string name, Command::User privilege)
        {
            string twitchChannel = GetTwitchChannel(lbUser);
            mIRC.DenyPrivilegeInCommand(twitchChannel, name, privilege);
            UpdateCommandInConfig(twitchChannel, name);
        }

        public void SetCommandEnabled(string lbUser, string name, bool enabled)
        {
            string twitchChannel = GetTwitchChannel(lbUser);
            mIRC.SetCommandEnabled(twitchChannel, name, enabled);
            UpdateCommandInConfig(twitchChannel, name);
        }

        public void RefreshEmotesForUser(string lbUser)
        {
            string twitchCHannel = GetTwitchChannel(lbUser);
            mIRC.RefreshEmotes(twitchCHannel);
        }

        public void UpdateLoginForUser(string lbUser, string newLogin)
        {
            // TODO:
            // - Part from current channel
            // - Add a new channel
            throw new NotImplementedException("Updating login for Twitch modules not yet implemented");
        }

        public UserModuleDescriptor GetUserModuleDescriptor()
        {
            UserModuleDescriptor umd = new UserModuleDescriptor();
            umd.Type = ModuleType.Twitch;
            umd.LoadPrerequisite = UserModuleLoadPrerequisites;
            umd.Loader = UserModuleLoader;
            umd.Unloader = UserModuleUnloader;
            return umd;
        }

        public void Run()
        {
            mIRC.Run();
        }

        public void RequestShutdown()
        {
            foreach (TwitchUserModule m in mUsers.Values)
                m.RequestShutdown();

            if (mIRC != null) mIRC.RequestShutdown();
        }

        public void WaitForShutdown()
        {
            foreach (TwitchUserModule m in mUsers.Values)
                m.WaitForShutdown();

            if (mIRC != null) mIRC.WaitForShutdown();
        }
    }
}