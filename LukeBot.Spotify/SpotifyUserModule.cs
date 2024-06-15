using System;
using System.IO;
using System.Net;
using LukeBot.Logging;
using LukeBot.API;
using LukeBot.Config;
using LukeBot.Module;
using CommonConstants = LukeBot.Common.Constants;
using System.Net.Http;

namespace LukeBot.Spotify
{
    public class SpotifyUserModule: IUserModule
    {
        internal string LBUser { get; private set; }
        private string mSpotifyUsername;
        private Token mToken;
        private API.Spotify.UserProfile mProfile;
        private NowPlaying mNowPlaying;
        private NowPlayingTextFile mNowPlayingTextFile;


        private bool CheckIfLoginSuccessful()
        {
            mProfile = API.Spotify.GetCurrentUserProfile(mToken);
            if (mProfile.code == HttpStatusCode.OK)
            {
                Logger.Log().Debug("Spotify login successful");
                return true;
            }
            else if (mProfile.code == HttpStatusCode.Unauthorized)
            {
                Logger.Log().Error("Failed to login to Spotify - Unauthorized");
                return false;
            }
            else
                throw new LoginFailedException("Failed to login to Spotify service: " + mProfile.code.ToString());
        }

        private void Login()
        {
            string scope = "user-read-currently-playing user-read-playback-state user-modify-playback-state user-read-email";
            mToken = AuthManager.Instance.GetToken(ServiceType.Spotify, LBUser);

            if (!mToken.Loaded)
                mToken.Request(scope);

            if (!CheckIfLoginSuccessful())
            {
                throw new InvalidOperationException("Failed to login to Spotify");
            }
        }


        // Public methods //

        public SpotifyUserModule(string lbUser)
        {
            LBUser = lbUser;

            string storagePath = "Outputs/" + CommonConstants.SPOTIFY_MODULE_NAME + "/" + LBUser;
            Directory.CreateDirectory(storagePath);

            mSpotifyUsername = Conf.Get<string>(
                Config.Path.Start()
                    .Push(CommonConstants.PROP_STORE_USER_DOMAIN)
                    .Push(LBUser)
                    .Push(CommonConstants.SPOTIFY_MODULE_NAME)
                    .Push(CommonConstants.PROP_STORE_LOGIN_PROP)
            );

            Login();

            mNowPlaying = new NowPlaying(LBUser, mToken);
            mNowPlayingTextFile = new NowPlayingTextFile(
                LBUser,
                "Outputs/" + CommonConstants.SPOTIFY_MODULE_NAME + "/" + LBUser + "/nowplaying_artist.txt",
                "Outputs/" + CommonConstants.SPOTIFY_MODULE_NAME + "/" + LBUser +  "/nowplaying_title.txt"
            );
        }

        ~SpotifyUserModule()
        {
            mNowPlayingTextFile = null;
            mNowPlaying = null;
        }

        // returns formatted artist-title if added successfuly; throws on errors
        public API.Spotify.Track AddSongToQueue(string url)
        {
            Logger.Log().Debug("Adding {0} to play queue", url);

            Uri uri;
            try
            {
                uri = new Uri(url);
            }
            catch (Exception)
            {
                throw new InvalidSpotifyURLException(url);
            }

            // some error checking just in case
            // example URL: https://open.spotify.com/track/2aWm2jIf91nByHThBYNppw?si=add63868785b4a36
            if (!uri.Host.Equals("open.spotify.com"))
            {
                throw new InvalidSpotifyURLException(url);
            }

            if (uri.Segments.Length != 3 || !uri.Segments[1].Equals("track/"))
            {
                throw new InvalidSpotifyURLException(url);
            }

            string trackID = uri.Segments[2];
            API.Spotify.Track track = API.Spotify.GetTrack(mToken, trackID);
            if (track.code != HttpStatusCode.OK)
            {
                Logger.Log().Error("Failed to get Track from Spotify: {0}", track.code);
                throw new SpotifyQueueAddFailedException(track.code);
            }

            Response resp = API.Spotify.AddItemToPlaybackQueue(mToken, trackID);
            if (!resp.IsSuccess)
            {
                Logger.Log().Error("Failed to add Track to queue: {0}", resp.code);
                throw new SpotifyQueueAddFailedException(resp.code);
            }

            Logger.Log().Debug("Added {0} - {1} to play queue successfully", track.artists[0].name, track.name);
            return track;
        }

        public void Run()
        {
            mNowPlaying.Run();
        }

        public void RequestShutdown()
        {
            mNowPlaying.RequestShutdown();
            mNowPlayingTextFile.Cleanup();
        }

        public void WaitForShutdown()
        {
            mNowPlaying.Wait();
        }

        public ModuleType GetModuleType()
        {
            return ModuleType.Spotify;
        }
    }
}
