using System;
using System.Threading;
using LukeBot.Config;
using LukeBot.Logging;


namespace LukeBot.API
{
    public enum AuthFlow
    {
        AuthorizationCode,
        ClientCredentials
    }

    public class Token
    {
        private Flow mFlow = null;
        private Config.Path mTokenPath = null;
        private AuthToken mToken = null;
        private Mutex mMutex = null;
        private string mLBUser = null;

        // Check if token is valid. This can be false when Token is either
        // not loaded, or loaded but past its expiration period.
        private bool IsValid
        {
            get
            {
                if (!Loaded)
                    return false;

                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                return now < mToken.acquiredTimestamp + mToken.expires_in;
            }
        }

        // Check if token is loaded. This ensures it has been loaded from
        // ex. Config, but it still might be an old (expired) token.
        public bool Loaded { get; private set; }
        public string ClientID
        {
            get
            {
                return mFlow.ClientID;
            }
        }

        private void ImportFromConfig()
        {
            mToken = Conf.Get<AuthToken>(mTokenPath);
        }

        private void ExportToConfig()
        {
            if (Conf.Exists(mTokenPath))
                Conf.Remove(mTokenPath);

            // Auth tokens should always be created hidden
            Conf.Add(mTokenPath, Property.Create<AuthToken>(mToken, true));
            Conf.Save();
        }

        public Token(string service, string lbUser, AuthFlow flow, string authURL, string refreshURL, string revokeURL, string callbackURL)
        {
            if (callbackURL.StartsWith("https://localhost"))
            {
                // HACK - localhost doesn't require https and it might break some logins sometimes
                callbackURL = callbackURL.Replace("https://localhost", "http://localhost");
            }

            switch (flow)
            {
            case AuthFlow.AuthorizationCode:
                mFlow = new AuthorizationCodeFlow(service, authURL, refreshURL, revokeURL, callbackURL);
                break;
            case AuthFlow.ClientCredentials:
                mFlow = new ClientCredentialsFlow(service, authURL, refreshURL, revokeURL);
                break;
            default:
                throw new ArgumentOutOfRangeException("Invalid AuthFlow mode: {0}" + flow.ToString());
            }

            mMutex = new Mutex();

            mTokenPath = Config.Path.Start()
                .Push(Common.Constants.PROP_STORE_USER_DOMAIN)
                .Push(lbUser)
                .Push(service)
                .Push(Common.Constants.PROP_STORE_TOKEN_PROP);

            Loaded = false;
            if (Conf.Exists(mTokenPath)) {
                Logger.Log().Debug("Found token at config {0}", mTokenPath);
                ImportFromConfig();
                Loaded = true;
            }

            mLBUser = lbUser;
        }

        ~Token()
        {
        }

        public string Get()
        {
            mMutex.WaitOne();

            if (mToken == null)
            {
                mMutex.ReleaseMutex();
                throw new InvalidTokenException("Token is not acquired");
            }

            string ret = mToken.access_token;
            mMutex.ReleaseMutex();

            return ret;
        }

        public string Request(string scope)
        {
            string ret;

            mMutex.WaitOne();

            // re-check validity, in case other thread already requested a Token for us
            if (IsValid)
            {
                ret = mToken.access_token;
                mMutex.ReleaseMutex();
                return ret;
            }

            mToken = mFlow.Request(mLBUser, scope);
            ExportToConfig();
            Loaded = true;

            ret = mToken.access_token;
            mMutex.ReleaseMutex();

            return ret;
        }

        public void EnsureValid()
        {
            if (!IsValid)
                Refresh();
        }

        public string Refresh()
        {
            string ret;

            mMutex.WaitOne();

            if (mToken == null)
                throw new InvalidTokenException("Token has not been acquired yet");

            // re-check validity, in case other thread already refreshed the Token for us
            if (IsValid)
            {
                ret = mToken.access_token;
                mMutex.ReleaseMutex();
                return ret;
            }

            AuthToken oldToken = mToken;
            mToken = mFlow.Refresh(mToken);

            // preserve refresh token - some services (ex. Spotify) don't provide it in refresh response
            if (mToken.refresh_token == null || mToken.refresh_token.Length == 0)
                mToken.refresh_token = oldToken.refresh_token;

            ExportToConfig();
            Loaded = true;

            ret = mToken.access_token;
            mMutex.ReleaseMutex();

            return ret;
        }

        public void Remove()
        {
            mMutex.WaitOne();

            if (Loaded) {
                Conf.Remove(mTokenPath);
                mFlow.Revoke(mToken);
                mToken = null;
                Loaded = false;
            }

            mMutex.ReleaseMutex();
        }
    }
}
