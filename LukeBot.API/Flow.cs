using System.IO;
using LukeBot.Common;
using LukeBot.Config;

namespace LukeBot.API
{
    abstract class Flow
    {
        private readonly string CLIENT_ID_PROP_NAME = "client_id";
        private readonly string CLIENT_SECRET_PROP_NAME = "client_secret";

        protected string mService;
        protected string mClientID;
        protected string mClientSecret;
        protected string mAuthURL;
        protected string mTokenURL;
        protected string mRevokeURL;

        public string ClientID
        {
            get
            {
                return mClientID;
            }
        }

        private string ReadFromConfig(string type)
        {
            Config.Path configPath = Config.Path.Form(mService, type);
            return Conf.Get<string>(configPath);
        }

        protected Flow(string service, string authURL, string tokenURL, string revokeURL)
        {
            mService = service;
            mAuthURL = authURL;
            mTokenURL = tokenURL;
            mRevokeURL = revokeURL;

            mClientID = ReadFromConfig(CLIENT_ID_PROP_NAME);
            if (mClientID == Common.Constants.DEFAULT_CLIENT_ID_NAME)
            {
                throw new InvalidClientDataException("Client ID for {0} not set in Property Store", mService);
            }

            mClientSecret = ReadFromConfig(CLIENT_SECRET_PROP_NAME);
            if (mClientSecret == Common.Constants.DEFAULT_CLIENT_SECRET_NAME)
            {
                throw new InvalidClientDataException("Client secret for {0} not set in Property Store", mService);
            }
        }

        public abstract AuthToken Request(string lbUser, string scope);
        public abstract AuthToken Refresh(AuthToken token);
        public abstract void Revoke(AuthToken token);
    }
}
