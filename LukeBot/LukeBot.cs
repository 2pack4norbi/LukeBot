using LukeBot.Interface;
using LukeBot.Common;
using LukeBot.Config;
using LukeBot.Logging;
using LukeBot.Globals;
using LukeBot.Communication;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading;


namespace LukeBot
{
    internal class LukeBot: IUserManager
    {
        private Dictionary<string, UserContext> mUsers = new();
        private object mUsersLock = new();
        private List<ICLIProcessor> mCommandProcessors = new List<ICLIProcessor>{
            new EventCLIProcessor(),
            new ModuleCLIProcessor(),
            new TestCLIProcessor(),
            new UserCLIProcessor(),

            // module CLI commands
            // TODO this should be added by modules themselves on Main Module initialization
            new SpotifyCLIProcessor(),
            new TwitchCLIProcessor(),
            new WidgetCLIProcessor(),
        };

        public LukeBot()
        {
        }

        ~LukeBot()
        {
        }

        // IUserManager implementations

        public UserPermissionLevel AuthenticateUser(string user, byte[] pwdHash, out string reason)
        {
            UserContext ctx;

            lock (mUsersLock)
            {
                if (!mUsers.TryGetValue(user, out ctx))
                {
                    reason = "User not found";
                    return UserPermissionLevel.None;
                }
            }

            if (!ctx.ValidatePassword(pwdHash))
            {
                reason = "Invalid password";
                return UserPermissionLevel.None;
            }

            reason = "";
            return ctx.GetPermissionLevel();
        }

        public bool ChangeUserPassword(string user, byte[] currentPwdHash, byte[] newPwdHash, out string reason)
        {
            if (AuthenticateUser(user, currentPwdHash, out reason) == UserPermissionLevel.None)
                return false;

            lock (mUsersLock)
            {
                mUsers[user].SetPassword(newPwdHash);
            }

            reason = "";
            return true;
        }


        void LoadUsers()
        {
            Path usersProp = Path.Start()
                .Push(Constants.LUKEBOT_USER_ID)
                .Push(Constants.PROP_STORE_USERS_PROP);

            if (!Conf.Exists(usersProp))
            {
                Logger.Log().Info("No users found");
                return;
            }

            string[] users = Conf.Get<string[]>(usersProp);

            if (users.Length == 0)
            {
                Logger.Log().Info("Users array is empty");
                return;
            }

            foreach (string user in users)
            {
                Logger.Log().Info("Loading LukeBot user " + user);
                CreateAndRunUser(user);
            }
        }

        void UnloadUsers()
        {
            Logger.Log().Info("Unloading users...");

            foreach (UserContext u in mUsers.Values)
            {
                u.RequestModuleShutdown();
            }

            foreach (UserContext u in mUsers.Values)
            {
                u.WaitForModulesShutdown();
            }

            mUsers.Clear();
        }

        void AddUserToConfig(string name)
        {
            Path propName = Path.Start()
                .Push(Constants.LUKEBOT_USER_ID)
                .Push(Constants.PROP_STORE_USERS_PROP);

            ConfUtil.ArrayAppend(propName, name);
        }

        void RemoveUserFromConfig(string name)
        {
            if (!mUsers.ContainsKey(name))
            {
                throw new ArgumentException("User " + name + " does not exist.");
            }

            Path propName = Path.Start()
                .Push(Constants.LUKEBOT_USER_ID)
                .Push(Constants.PROP_STORE_USERS_PROP);

            ConfUtil.ArrayRemove(propName, name);

            // also clear entire branch of user-related settings
            Path userConfDomain = Path.Start()
                .Push(Constants.PROP_STORE_USER_DOMAIN)
                .Push(name);

            if (Conf.Exists(userConfDomain))
                Conf.Remove(userConfDomain);
        }


        private void AddCLICommands()
        {
            foreach (ICLIProcessor cp in mCommandProcessors)
            {
                cp.AddCLICommands(this);
            }
        }

        private void Shutdown()
        {
            UserInterface.Teardown();

            UnloadUsers();

            Logger.Log().Info("Stopping Global Modules...");
            GlobalModules.Stop();

            Logger.Log().Info("Stopping web endpoint...");
            Endpoint.Endpoint.StopThread();

            Logger.Log().Info("Core systems teardown...");
            GlobalModules.Teardown();
            Comms.Teardown();
            Conf.Teardown();
        }

        private void CreateAndRunUser(string lbUsername)
        {
            lock (mUsersLock)
            {
                if (mUsers.ContainsKey(lbUsername) || lbUsername == Constants.LUKEBOT_USER_ID)
                    throw new UsernameNotAvailableException(lbUsername);

                Comms.Event.AddUser(lbUsername);

                UserContext uc = new UserContext(lbUsername);
                uc.RunModules();

                mUsers.Add(lbUsername, uc);
            }
        }

        public void AddUser(string lbUsername)
        {
            CreateAndRunUser(lbUsername);
            AddUserToConfig(lbUsername);
        }

        public void RemoveUser(string lbUsername)
        {
            lock (mUsersLock)
            {
                UserContext u = mUsers[lbUsername];
                u.RequestModuleShutdown();
                u.WaitForModulesShutdown();

                RemoveUserFromConfig(lbUsername);
                mUsers.Remove(lbUsername);
                Comms.Event.RemoveUser(lbUsername);
            }
        }

        public List<string> GetUsernames()
        {
            lock (mUsersLock)
            {
                return mUsers.Keys.ToList<string>();
            }
        }

        // returns true if @p username exists, or is empty; false otherwise
        public bool IsUsernameValid(string username)
        {
            lock (mUsersLock)
            {
                return (username.Length == 0) || mUsers.ContainsKey(username);
            }
        }

        public UserContext GetUser(string username)
        {
            lock (mUsersLock)
            {
                return mUsers[username];
            }
        }

        public void Run(ProgramOptions opts)
        {
            try
            {
                Logger.Log().Info("LukeBot v0.0.1 starting");

                Logger.Log().Info("Loading configuration...");
                Conf.Initialize(opts.StoreDir);

                Logger.Log().Info("Initializing Core Comms...");
                Comms.Initialize();

                Logger.Log().Info("Starting web endpoint...");
                Endpoint.Endpoint.StartThread();

                Logger.Log().Info("Initializing Global Modules...");
                GlobalModules.Initialize();

                GlobalModules.Run();

                InterfaceType uiType = opts.CLI;
                Logger.Log().Info("Initializing UI {0}...", uiType.ToString());
                UserInterface.Initialize(uiType, this);

                LoadUsers();

                Logger.Log().Info("Giving control to UI");
                AddCLICommands();
                UserInterface.CLI.MainLoop();
            }
            catch (Common.Exception e)
            {
                e.Print(LogLevel.Error);
            }
            catch (System.Exception e)
            {
                Logger.Log().Error("Exception caught: {0}", e.Message);
                Logger.Log().Error("Backtrace:\n{0}", e.StackTrace);
            }

            Shutdown();
        }
    }
}
