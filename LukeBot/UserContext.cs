using System;
using System.Collections.Generic;
using LukeBot.Common;
using LukeBot.Config;
using LukeBot.Globals;
using LukeBot.Interface;
using LukeBot.Logging;
using LukeBot.Module;


namespace LukeBot
{
    class UserContext
    {
        public string Username { get; private set; }

        private const string PROP_STORE_MODULES_DOMAIN = "modules";
        private const string PROP_STORE_WIDGETS_DOMAIN = "widgets";
        private const string PROP_STORE_ACCOUNT_DOMAIN = "account";
        private const string PROP_STORE_PASSWORD = "password";

        private Dictionary<ModuleType, IUserModule> mModules = new();
        private PasswordData mPasswordData = null;

        // user data management
        private void UpdateUserDataInConfig()
        {
            Path passwordDataPath = Path.Start()
                .Push(Constants.PROP_STORE_USER_DOMAIN)
                .Push(Username)
                .Push(PROP_STORE_ACCOUNT_DOMAIN)
                .Push(PROP_STORE_PASSWORD);

            if (!Conf.Exists<PasswordData>(passwordDataPath))
                Conf.Add(passwordDataPath, Property.Create<PasswordData>(mPasswordData));
            else
                Conf.Modify<PasswordData>(passwordDataPath, mPasswordData);

            Conf.Save();
        }

        private void LoadUserDataFromConfig()
        {
            Path passwordDataPath = Path.Start()
                .Push(Constants.PROP_STORE_USER_DOMAIN)
                .Push(Username)
                .Push(PROP_STORE_ACCOUNT_DOMAIN)
                .Push(PROP_STORE_PASSWORD);

            if (!Conf.TryGet<PasswordData>(passwordDataPath, out mPasswordData))
            {
                // no password, issue a warning
                Logger.Log().Warning("User " + Username + " has no password set! Remember to set your password.");
                mPasswordData = null;
            }
        }

        // module-config management
        private void AddModuleToConfig(string module)
        {
            Path modulesProp = Path.Start()
                .Push(Constants.PROP_STORE_USER_DOMAIN)
                .Push(Username)
                .Push(PROP_STORE_MODULES_DOMAIN);

            ConfUtil.ArrayAppend(modulesProp, module);
        }

        private void LoadModulesFromConfig()
        {
            Path modulesProp = Path.Start()
                .Push(Constants.PROP_STORE_USER_DOMAIN)
                .Push(Username)
                .Push(PROP_STORE_MODULES_DOMAIN);

            string[] modules;
            if (!Conf.TryGet<string[]>(modulesProp, out modules))
            {
                // Couldn't find the config entry, meaning there is no enabled modules.
                // Not considered an error.
                modules = new string[0];
            }

            foreach (string m in modules)
            {
                try
                {
                    // here we ignore the returned module and do not start it
                    // RunModules() will be called later and will kickstart it for us
                    LoadModule(m.GetModuleTypeEnum());
                }
                catch (System.Exception e)
                {
                    Logger.Log().Error("Failed to initialize module {0} for user {1}: {2}",
                        m, Username, e.Message);
                    Logger.Log().Error("Module {0} for user {1} will be skipped on this load.",
                        m, Username);
                    Logger.Log().Trace("Stack trace:\n{0}", e.StackTrace);
                }
            }
        }

        private void RemoveModuleFromConfig(string module)
        {
            Path modulesProp = Path.Start()
                .Push(Constants.PROP_STORE_USER_DOMAIN)
                .Push(Username)
                .Push(PROP_STORE_MODULES_DOMAIN);

            ConfUtil.ArrayRemove(modulesProp, module);
        }

        private IUserModule LoadModule(ModuleType type)
        {
            IUserModule m = GlobalModules.UserModuleManager.Create(type, Username);
            mModules.Add(type, m);
            return m;
        }

        private void UnloadModule(ModuleType type)
        {
            IUserModule m = mModules[type];

            GlobalModules.UserModuleManager.Unload(m);
            m.RequestShutdown();
            m.WaitForShutdown();

            mModules.Remove(type);
        }


        public UserContext(string user)
        {
            Username = user;

            LoadUserDataFromConfig();

            Logger.Log().Info("Loading required modules for user {0}", Username);
            LoadModulesFromConfig();

            Logger.Log().Info("Loaded LukeBot user {0}", Username);
        }

        public void EnableModule(ModuleType module)
        {
            if (mModules.ContainsKey(module))
            {
                throw new ModuleEnabledException(module, Username);
            }

            IUserModule m = LoadModule(module);
            AddModuleToConfig(module.ToConfString());

            m.Run();
        }

        public void DisableModule(ModuleType module)
        {
            if (!mModules.ContainsKey(module))
            {
                throw new ModuleDisabledException(module, Username);
            }

            UnloadModule(module);
            RemoveModuleFromConfig(module.ToConfString());
        }

        public List<ModuleType> GetEnabledModules()
        {
            List<ModuleType> enabledModules = new(mModules.Keys.Count);
            foreach (ModuleType m in mModules.Keys)
                enabledModules.Add(m);
            return enabledModules;
        }

        // Set a new password based on a received hash. This path should
        // be taken only by remote connections (aka. via ServerCLI)
        public void SetPassword(byte[] passwordHash)
        {
            mPasswordData = PasswordData.Create(passwordHash);
            UpdateUserDataInConfig();
        }

        // Set a new password based on plaintext. This path should
        // be ONLY taken locally (ex. via BasicCLI)
        public void SetPassword(string newPassword)
        {
            mPasswordData = PasswordData.Create(newPassword);
            UpdateUserDataInConfig();
        }

        // Validate if a password string is correct. Use ONLY locally.
        public bool ValidatePassword(string password)
        {
            if (password.Length == 0 && mPasswordData == null)
                return true;

            return mPasswordData.Equals(password);
        }

        // Validates if password is correct. For remote connections only.
        public bool ValidatePassword(byte[] passwordHash)
        {
            if (mPasswordData == null)
            {
                // no password data - reject login
                Logger.Log().Warning("Attempted to validate non-existing password for user {0}", Username);
                return false;
            }

            return mPasswordData.Equals(passwordHash);
        }

        public void RunModules()
        {
            Logger.Log().Info("Running LukeBot modules for user {0}", Username);
            foreach (IUserModule m in mModules.Values)
                m.Run();
        }

        public void RequestModuleShutdown()
        {
            foreach (IUserModule m in mModules.Values)
                m.RequestShutdown();
        }

        public void WaitForModulesShutdown()
        {
            foreach (IUserModule m in mModules.Values)
                m.WaitForShutdown();
        }
    }
}
