﻿using System;
using System.Net;
using LukeBot.Common;
using LukeBot.Auth;
using LukeBot.Core;


namespace LukeBot.Twitch
{
    public class TwitchModule: IModule
    {
        private Token mToken;
        private Token mUserToken;
        private TwitchIRC mIRC;
        private PubSub mPubSub;
        private API.GetUserResponse mBotData;
        private API.GetUserResponse mUserData;
        private ChatWidget mChatWidget;
        private AlertsWidget mAlertsWidget;

        private bool IsLoginSuccessful(Token token)
        {
            API.GetUserResponse data = API.GetUser(token);
            if (data.code == HttpStatusCode.OK)
            {
                Logger.Log().Debug("Twitch login successful");
                return true;
            }
            else if (data.code == HttpStatusCode.Unauthorized)
            {
                Logger.Log().Error("Failed to login to Twitch - Unauthorized");
                return false;
            }
            else
                throw new LoginFailedException("Failed to login to Twitch: " + mBotData.code.ToString());
        }

        public TwitchModule()
        {
            Systems.Communication.Register(Constants.SERVICE_NAME);

            string tokenScope = "chat:read chat:edit user:read:email";
            mToken = AuthManager.Instance.GetToken(ServiceType.Twitch, "lukebot");

            bool tokenFromFile = mToken.Loaded;
            if (!mToken.Loaded)
                mToken.Request(tokenScope);

            if (!IsLoginSuccessful(mToken))
            {
                if (tokenFromFile)
                {
                    mToken.Refresh();
                    if (!IsLoginSuccessful(mToken))
                    {
                        mToken.Remove();
                        throw new InvalidOperationException(
                            "Failed to refresh Twitch OAuth Token. Token has been removed, restart to re-login and request a fresh OAuth token"
                        );
                    }
                }
                else
                    throw new InvalidOperationException("Failed to login to Twitch");
            }

            mBotData = API.GetUser(mToken);

            mIRC = new TwitchIRC(mToken);
            mChatWidget = new ChatWidget();
        }

        // TEMPORARY
        public void JoinChannel(string channel)
        {
            Logger.Log().Debug("Joining channel {0}", channel);
            mUserData = API.GetUser(mToken, channel);

            mIRC.JoinChannel(mUserData);

            string tokenScope = "user:read:email channel:read:redemptions";
            mUserToken = AuthManager.Instance.GetToken(ServiceType.Twitch, "lookey");

            bool tokenFromFile = mUserToken.Loaded;
            if (!mUserToken.Loaded)
                mUserToken.Request(tokenScope);

            if (!IsLoginSuccessful(mUserToken))
            {
                if (tokenFromFile)
                {
                    mUserToken.Refresh();
                    if (!IsLoginSuccessful(mUserToken))
                    {
                        mUserToken.Remove();
                        throw new InvalidOperationException(
                            "Failed to refresh Twitch OAuth Token. Token has been removed, restart to re-login and request a fresh OAuth token"
                        );
                    }
                }
                else
                    throw new InvalidOperationException("Failed to login to Twitch");
            }

            mPubSub = new PubSub(mUserToken);
            mPubSub.Listen(mUserData);

            mAlertsWidget = new AlertsWidget();

            Logger.Log().Secure("Joined channel twitch ID: {0}", mUserData.data[0].id);
        }

        // TEMPORARY
        public void AddCommandToChannel(string channel, string commandName, Command.ICommand command)
        {
            mIRC.AddCommandToChannel(channel, commandName, command);
        }

        // TEMPORARY
        public void AwaitIRCLoggedIn(int timeoutMs)
        {
            mIRC.AwaitLoggedIn(timeoutMs);
        }


        // IModule overrides

        public void Init()
        {
        }

        public void Run()
        {
            mIRC.Run();
        }

        public void RequestShutdown()
        {
            if (mIRC != null) mIRC.RequestShutdown();
            if (mPubSub != null) mPubSub.RequestShutdown();
            if (mChatWidget != null) mChatWidget.RequestShutdown();
            if (mAlertsWidget != null) mAlertsWidget.RequestShutdown();
        }

        public void WaitForShutdown()
        {
            if (mIRC != null) mIRC.WaitForShutdown();
            if (mPubSub != null) mPubSub.WaitForShutdown();
            if (mChatWidget != null) mChatWidget.WaitForShutdown();
            if (mAlertsWidget != null) mAlertsWidget.WaitForShutdown();
        }
    }
}
