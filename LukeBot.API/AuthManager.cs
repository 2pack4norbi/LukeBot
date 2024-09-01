using System;
using System.Threading;
using System.Collections.Generic;
using LukeBot.Common;
using LukeBot.Communication;
using LukeBot.Config;
using LukeBot.Logging;
using LukeBot.Communication.Common;


namespace LukeBot.API
{
    public class Events
    {
        public const string AUTHMGR_OPEN_BROWSER = "AuthmgrOpenBrowser";
    }

    public class OpenBrowserURLArgs: EventArgsBase
    {
        public string LukeBotUser { get; set; }
        public string URL { get; set; }

        public OpenBrowserURLArgs(string lbUser, string url)
            : base(Events.AUTHMGR_OPEN_BROWSER)
        {
            LukeBotUser = lbUser;
            URL = url;
        }
    }

    public class AuthManager: IEventPublisher
    {
        private static readonly Lazy<AuthManager> mInstance =
            new Lazy<AuthManager>(() => new AuthManager());
        public static AuthManager Instance { get { return mInstance.Value; } }

        Dictionary<Path, Token> mTokens = new();
        Mutex mMutex = new();
        EventCallback mOpenBrowserURLDelegate;

        private Token NewTokenForService(ServiceType service, string lbUser)
        {
            switch (service)
            {
            case ServiceType.Twitch: return new TwitchToken(AuthFlow.AuthorizationCode, lbUser);
            case ServiceType.Spotify: return new SpotifyToken(AuthFlow.AuthorizationCode, lbUser);
            default:
                throw new ArgumentOutOfRangeException();
            }
        }

        private Path FormTokenDictionaryKey(ServiceType service, string lbUser)
        {
            return Path.Start()
                .Push(Common.Constants.PROP_STORE_USER_DOMAIN)
                .Push(lbUser)
                .Push(service.ToString().ToLower())
                .Push(Common.Constants.PROP_STORE_TOKEN_PROP);
        }

        private AuthManager()
        {
            List<EventCallback> callbacks = Comms.Event.Global().RegisterPublisher(this);
            mOpenBrowserURLDelegate = callbacks[0];
        }

        public string GetName()
        {
            return "AuthManager";
        }

        public List<EventDescriptor> GetEvents()
        {
            List<EventDescriptor> events = new();

            events.Add(new EventDescriptor()
            {
                Name = Events.AUTHMGR_OPEN_BROWSER,
                Description = "Internal - requests to open a browser window for AuthMgr.",
                Dispatcher = null
            });

            return events;
        }


        public Token GetToken(ServiceType service, string lbUser)
        {
            Path tokenKey = FormTokenDictionaryKey(service, lbUser);

            mMutex.WaitOne();

            Token ret;
            if (!mTokens.TryGetValue(tokenKey, out ret))
            {
                ret = NewTokenForService(service, lbUser);
                mTokens[tokenKey] = ret;
            }

            mMutex.ReleaseMutex();
            return ret;
        }

        public void InvalidateToken(ServiceType service, string lbUser)
        {
            Path tokenKey = FormTokenDictionaryKey(service, lbUser);

            mMutex.WaitOne();

            Token t;
            if (mTokens.TryGetValue(tokenKey, out t))
            {
                t.Remove();
                mTokens.Remove(tokenKey);
            }

            mMutex.ReleaseMutex();
        }

        internal void OpenBrowserURL(string lbUser, string URL)
        {
            OpenBrowserURLArgs a = new(lbUser, URL);
            mOpenBrowserURLDelegate.PublishEvent(a);
        }
    }
}
