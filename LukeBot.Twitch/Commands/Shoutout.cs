using LukeBot.API;
using LukeBot.Config;
using LukeBot.Logging;
using Command = LukeBot.Twitch.Common.Command;
using CommonConstants = LukeBot.Common.Constants;


namespace LukeBot.Twitch.Command
{
    public class Shoutout: ICommand
    {
        private string mBotLogin;

        public Shoutout(Command::Descriptor d)
            : base(d)
        {
            mBotLogin = Conf.Get<string>(Path.Start()
                .Push(CommonConstants.TWITCH_MODULE_NAME)
                .Push(CommonConstants.PROP_STORE_LOGIN_PROP)
            );

            if (mBotLogin == CommonConstants.DEFAULT_LOGIN_NAME)
            {
                throw new PropertyFileInvalidException("Bot's Twitch login has not been provided in Property Store");
            }
        }

        public override void Edit(string newValue)
        {
            // empty - no parameters that affect message contents
        }

        public override string Execute(Command::User callerPrivilege, string[] args)
        {
            if (args.Length < 2)
            {
                return "Name to shoutout not provided! Pls provide one :(";
            }

            API.Twitch.GetUserData userData;
            API.Twitch.GetChannelInformationData channelData;

            try
            {
                Token t = AuthManager.Instance.GetToken(ServiceType.Twitch, mBotLogin);
                API.Twitch.GetUserResponse userDataResponse = API.Twitch.GetUser(t, args[1]);
                if (userDataResponse.data == null || userDataResponse.data.Count == 0)
                {
                    throw new System.IndexOutOfRangeException("User data came back empty/invalid");
                }

                userData = userDataResponse.data[0];

                API.Twitch.GetChannelInformationResponse channelDataResponse = API.Twitch.GetChannelInformation(t, userDataResponse.data[0].id);
                if (channelDataResponse.data == null || channelDataResponse.data.Count == 0)
                {
                    throw new System.IndexOutOfRangeException("Channel data came back empty/invalid");
                }

                channelData = channelDataResponse.data[0];
            }
            catch (System.Exception e)
            {
                Logger.Log().Warning("Failed to execute Shoutout command: {0}", e.Message);
                return string.Format("Shoutout command failed: {0}", e.Message);
            }

            return string.Format("Make sure to check out {0} at https://twitch.tv/{1} ! They were last seen streaming {2}.",
                                 channelData.broadcaster_name, userData.login, channelData.game_name);
        }

        public override Command::Descriptor ToDescriptor()
        {
            return new Command::Descriptor(mName, Command::Type.shoutout, mPrivilegeLevel, mEnabled, "");
        }
    }
}