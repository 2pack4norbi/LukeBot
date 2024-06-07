using System;
using System.Collections.Generic;
using System.Net;
using LukeBot.API;
using LukeBot.Common;
using LukeBot.Communication;
using LukeBot.Logging;
using LukeBot.Module;
using LukeBot.Twitch.EventSub;
using LukeBot.Twitch.Common;
using Widget = LukeBot.Widget;


namespace LukeBot.Twitch
{
    public class TwitchUserModule: IUserModule
    {
        private string mLBUser;
        private string mChannelName;
        private Token mUserToken;
        private API.Twitch.GetUserData mUserData;
        private EventSubClient mEventSub;


        public TwitchUserModule(string lbUser, Token botToken, string channelName)
        {
            mLBUser = lbUser;
            mChannelName = channelName;

            // Each user has its own queued dispatcher to independently handle some events
            Comms.Event.User(mLBUser).AddEventDispatcher(Constants.QueuedDispatcherForUser(mLBUser), EventDispatcherType.Queued);

            API.Twitch.GetUserResponse resp = API.Twitch.GetUser(botToken, mChannelName);
            if (resp.code != HttpStatusCode.OK)
            {
                Logger.Log().Error("Failed to fetch user data from Twitch - received error code {0}", resp.code.ToString());
                throw new APIResponseErrorException(resp.code);
            }
            mUserData = resp.data[0];

            // TODO token's scope should be moved to Config
            string tokenScope = "user:read:email channel:read:redemptions channel:read:subscriptions";
            mUserToken = AuthManager.Instance.GetToken(ServiceType.Twitch, lbUser);

            bool tokenFromFile = mUserToken.Loaded;
            if (!mUserToken.Loaded)
                mUserToken.Request(tokenScope);

            if (!Utils.IsLoginSuccessful(mUserToken))
            {
                throw new InvalidOperationException("Failed to login to Twitch");
            }

            mEventSub = new(mLBUser);
            //mEventSub.Connect(mUserToken, mUserData.id);
        }

        internal API.Twitch.GetUserData GetUserData()
        {
            return mUserData;
        }

        internal Token GetUserToken()
        {
            return mUserToken;
        }

        internal string GetLBUser()
        {
            return mLBUser;
        }

        internal string GetChannelName()
        {
            return mChannelName;
        }

        // IUserModule overrides

        public void Run()
        {
            try
            {
                List<string> events = new();
                events.Add(EventSubClient.SUB_CHANNEL_POINTS_REDEMPTION_ADD);
                events.Add(EventSubClient.SUB_SUBSCRIPTION_GIFT);
                events.Add(EventSubClient.SUB_SUBSCRIPTION_MESSAGE);
                mEventSub.Subscribe(events);
            }
            catch (System.Exception e)
            {
                Logger.Log().Error("Failed to subscribe to EventSub for user {0}: {1}",
                    mLBUser, e.Message);
                Logger.Log().Trace("Stack trace:\n{0}", e.StackTrace);
            }
        }

        public void RequestShutdown()
        {
            if (mEventSub != null) mEventSub.RequestShutdown();
        }

        public void WaitForShutdown()
        {
            if (mEventSub != null) mEventSub.WaitForShutdown();

            Comms.Event.User(mLBUser).RemoveEventDispatcher(Constants.QueuedDispatcherForUser(mLBUser));
        }

        public ModuleType GetModuleType()
        {
            return ModuleType.Twitch;
        }
    }
}
