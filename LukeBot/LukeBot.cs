﻿using LukeBot.Interface;
using LukeBot.Common;
using LukeBot.Config;
using LukeBot.Logging;
using LukeBot.Globals;
using LukeBot.Communication;
using System.Collections.Generic;
using System;
using System.Linq;


namespace LukeBot
{
    internal class LukeBot
    {
        private Dictionary<string, UserContext> mUsers = new();
        private UserContext mCurrentUser = null;
        private List<ICLIProcessor> mCommandProcessors = new List<ICLIProcessor>{
            new UserCLIProcessor(),
            new ModuleCLIProcessor(),
            new SpotifyCLIProcessor(),
            new TwitchCLIProcessor(),
            new WidgetCLIProcessor(),
        };

        void OnCancelKeyPress(object sender, ConsoleCancelEventArgs args)
        {
            // UI is not handled here; it captures Ctrl+C on its own
            Logger.Log().Info("Ctrl+C handled: Requested shutdown");
            Shutdown();
        }

        public LukeBot()
        {
        }

        ~LukeBot()
        {
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
            CLI.Instance.Teardown();

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
            if (mUsers.ContainsKey(lbUsername) || lbUsername == Constants.LUKEBOT_USER_ID)
                throw new UsernameNotAvailableException(lbUsername);

            Comms.Event.AddUser(lbUsername);

            UserContext uc = new UserContext(lbUsername);
            uc.RunModules();

            mUsers.Add(lbUsername, uc);
        }

        public void AddUser(string lbUsername)
        {
            CreateAndRunUser(lbUsername);
            AddUserToConfig(lbUsername);
        }

        public void RemoveUser(string lbUsername)
        {
            UserContext u = mUsers[lbUsername];
            u.RequestModuleShutdown();
            u.WaitForModulesShutdown();

            // deselect current user if it is the one we remove
            if (mCurrentUser != null && mCurrentUser.Username == lbUsername)
                SelectUser("");

            RemoveUserFromConfig(lbUsername);
            mUsers.Remove(lbUsername);
            Comms.Event.RemoveUser(lbUsername);
        }

        public void SelectUser(string lbUsername)
        {
            if (lbUsername.Length == 0)
            {
                mCurrentUser = null;
                CLI.Instance.SaveSelectedUser("");
                return;
            }

            mCurrentUser = mUsers[lbUsername];
            CLI.Instance.SaveSelectedUser(mCurrentUser.Username);
        }

        public List<string> GetUsernames()
        {
            return mUsers.Keys.ToList<string>();
        }

        public UserContext GetCurrentUser()
        {
            if (mCurrentUser == null)
                throw new NoUserSelectedException();

            return mCurrentUser;
        }

        public void Run(ProgramOptions opts)
        {
            Console.CancelKeyPress += OnCancelKeyPress;

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

                LoadUsers();

                // We'll get stuck here until the end
                Logger.Log().Info("Giving control to CLI");
                AddCLICommands();
                CLI.Instance.MainLoop();
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
