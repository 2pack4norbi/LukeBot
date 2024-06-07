using System;
using System.Collections.Generic;
using System.Diagnostics;
using LukeBot.Communication;
using LukeBot.Communication.Common.Intercom;
using LukeBot.Config;
using LukeBot.Interface;
using LukeBot.Module;
using LukeBot.Spotify.Common;
using CommonConstants = LukeBot.Common.Constants;


namespace LukeBot.Spotify
{
    public class SpotifyMainModule: IMainModule
    {
        private Dictionary<string, SpotifyUserModule> mModules = new();


        private bool UserModuleLoadPrerequisites(string lbUser)
        {
            Path userSpotifyLoginProp = Path.Start()
                .Push(CommonConstants.PROP_STORE_USER_DOMAIN)
                .Push(lbUser)
                .Push(CommonConstants.SPOTIFY_MODULE_NAME)
                .Push(Constants.PROP_STORE_SPOTIFY_LOGIN_PROP);

            string login;
            if (Conf.TryGet<string>(userSpotifyLoginProp, out login))
                return true; // quietly exit - login is already there, prerequisites are met

            // User login does not exist - query for it
            login = UserInterface.Instance.Query(false, "Spotify login for user " + lbUser);
            if (login.Length == 0)
            {
                UserInterface.Instance.Message("No login provided\n");
                return false;
            }

            Conf.Add(userSpotifyLoginProp, Property.Create<string>(login));
            return true;
        }

        private IUserModule UserModuleLoader(string lbUser)
        {
            SpotifyUserModule m = new SpotifyUserModule(lbUser);
            mModules.Add(lbUser, m);
            return m;
        }

        private void UserModuleUnloader(IUserModule module)
        {
            SpotifyUserModule um = module as SpotifyUserModule;
            mModules.Remove(um.LBUser);
        }


        // Intercom interface

        private ResponseBase Intercom_ResponseAllocator(MessageBase msg)
        {
            switch (msg.Message)
            {
            case Messages.ADD_SONG_TO_QUEUE: return new AddSongToQueueResponse();
            }

            Debug.Assert(false, "Message should be validated by now - should not happen");
            return new ResponseBase();
        }

        private void Intercom_AddSongToQueueDelegate(MessageBase mb, ref ResponseBase rb)
        {
            AddSongToQueueMsg message = (AddSongToQueueMsg)mb;
            AddSongToQueueResponse response = (AddSongToQueueResponse)rb;

            try
            {
                API.Spotify.Track t = mModules[message.User].AddSongToQueue(message.URL);
                response.Artist = t.artists[0].name;
                response.Title = t.name;
                response.SignalSuccess();
            }
            catch (Exception e)
            {
                response.SignalError(string.Format("{0}", e.Message));
            }
        }


        // Publics

        public SpotifyMainModule()
        {
            Comms.Communication.Register(CommonConstants.SPOTIFY_MODULE_NAME);

            EndpointInfo epInfo = new EndpointInfo(Endpoints.SPOTIFY_MAIN_MODULE, Intercom_ResponseAllocator);
            epInfo.AddMessage(Messages.ADD_SONG_TO_QUEUE, Intercom_AddSongToQueueDelegate);
            Comms.Intercom.Register(epInfo);
        }

        public UserModuleDescriptor GetUserModuleDescriptor()
        {
            UserModuleDescriptor umd = new UserModuleDescriptor();
            umd.Type = ModuleType.Spotify;
            umd.LoadPrerequisite = UserModuleLoadPrerequisites;
            umd.Loader = UserModuleLoader;
            umd.Unloader = UserModuleUnloader;
            return umd;
        }

        public void UpdateLoginForUser(string lbUser, string newLogin)
        {
            // TODO
            throw new NotImplementedException("Updating login for Spotify modules not yet implemented");
        }
    }
}