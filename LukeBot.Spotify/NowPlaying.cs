using System.Net;
using System.Threading;
using System.Collections.Generic;
using LukeBot.Logging;
using LukeBot.API;
using LukeBot.Spotify.Common;
using LukeBot.Communication;
using LukeBot.Communication.Common;


namespace LukeBot.Spotify
{
    class NowPlaying: IEventPublisher
    {
        private readonly int DEFAULT_EVENT_TIMEOUT = 5 * 1000; // 5 seconds
        private readonly int EXTRA_EVENT_TIMEOUT = 2000; // see FetchData() for details

        private string mLBUser;
        private Token mToken;
        private Thread mThread;
        private Mutex mDataAccessMutex;
        private ManualResetEvent mShutdownEvent;
        private API.Spotify.PlaybackState mCurrentPlaybackState;
        private SpotifyStateUpdateArgs mCurrentStateUpdate;
        private int mEventTimeout;
        private bool mChangeExpected;
        private bool mNoItemWarningEmitted;
        private EventCallback mTrackChangedCallback;
        private EventCallback mStateUpdateCallback;

        public string GetName()
        {
            return "SpotifyNowPlayingModule";
        }

        public List<EventDescriptor> GetEvents()
        {
            List<EventDescriptor> events = new();

            events.Add(new EventDescriptor()
            {
                Name = Spotify.Common.Events.SPOTIFY_STATE_UPDATE,
                Description = "Spotify Player state update. Emitted regularly when playback state is fetched from Spotify servers.",
                Dispatcher = null
            });
            events.Add(new EventDescriptor()
            {
                Name = Spotify.Common.Events.SPOTIFY_TRACK_CHANGED,
                Description = "Spotify Player track update. Emitted when played track is changed to a different one.",
                Dispatcher = null
            });

            return events;
        }

        public NowPlaying(string lbUser, Token token)
        {
            mLBUser = lbUser;
            mToken = token;
            mThread = new Thread(new ThreadStart(ThreadMain));
            mDataAccessMutex = new Mutex();
            mShutdownEvent = new ManualResetEvent(false);
            mEventTimeout = DEFAULT_EVENT_TIMEOUT;
            mChangeExpected = false;
            mNoItemWarningEmitted = false;
            mCurrentPlaybackState = null;
            mCurrentStateUpdate = new SpotifyStateUpdateArgs();

            List<EventCallback> events = Comms.Event.User(mLBUser).RegisterPublisher(this);

            foreach (EventCallback e in events)
            {
                switch (e.eventName)
                {
                case Spotify.Common.Events.SPOTIFY_STATE_UPDATE:
                    mStateUpdateCallback = e;
                    break;
                case Spotify.Common.Events.SPOTIFY_TRACK_CHANGED:
                    mTrackChangedCallback = e;
                    break;
                default:
                    Logger.Log().Warning("Received unknown event type from Event system");
                    break;
                }
            }
        }

        ~NowPlaying()
        {
        }

        async void FetchData()
        {
            API.Spotify.PlaybackState state;

            try
            {
                state = API.Spotify.GetPlaybackState(mToken);
            }
            catch (System.Exception e)
            {
                Logger.Log().Error("Caught exception while fetching playback state: {0}", e.Message);
                Logger.Log().Debug("Stack trace:\n{0}", e.StackTrace);
                return;
            }

            if (state.code != HttpStatusCode.OK)
            {
                if (state.code != HttpStatusCode.NoContent)
                {
                    Logger.Log().Error("Failed to fetch Now Playing playback state: {0}", state.code);
                    if (Logger.IsLogLevelEnabled(LogLevel.Secure))
                    {
                        string msg = await state.responseData.httpMessage.Content.ReadAsStringAsync();
                        Logger.Log().Secure("Received message: {0}", msg);
                    }
                }

                mEventTimeout = DEFAULT_EVENT_TIMEOUT;
                mChangeExpected = false;

                return;
            }

            if (state.item == null || state.progress_ms == null)
            {
                if (!mNoItemWarningEmitted)
                    Logger.Log().Warning("No track item received (null item or progress_ms). Private session might be enabled.");

                mEventTimeout = DEFAULT_EVENT_TIMEOUT;
                mChangeExpected = false;
                mNoItemWarningEmitted = true;
                return;
            }

            mDataAccessMutex.WaitOne();

            mNoItemWarningEmitted = false;

            // Track change
            if ((mCurrentPlaybackState == null) || (state.item.id != mCurrentPlaybackState.item.id))
            {
                // Spotify doesn't provide copyright holder info (aka label info) with
                // currently played track API call. For that reason we will fetch the info
                // separately from album details and copy it to our "data" object for
                // further reference. Shallow copy should be ok.
                API.Spotify.Album album = API.Spotify.GetAlbum(mToken, state.item.album.id);
                state.item.album.copyrights = album.copyrights;

                mCurrentPlaybackState = state;
                mChangeExpected = false;
                Logger.Log().Debug("{0}", mCurrentPlaybackState.ToString());
                mTrackChangedCallback.PublishEvent(Utils.DataItemToTrackChangedArgs(mCurrentPlaybackState.item));
            }

            // State read - must reach for fetched "state" to get correct playback info
            SpotifyStateUpdateArgs stateUpdate = Utils.DataToStateUpdateArgs(state);

            // Update internal logic according to state
            if (stateUpdate.State == PlayerState.Playing)
            {
                if (mChangeExpected)
                {
                    // if mChangeExpected is set here, that means server must've lagged a bit
                    // and new song is still not updated. Rush the next update just in case.
                    mEventTimeout = 1000;
                }
                else
                {
                    int trackLeftMs = mCurrentPlaybackState.item.duration_ms - (int)mCurrentPlaybackState.progress_ms;
                    if (trackLeftMs < DEFAULT_EVENT_TIMEOUT)
                    {
                        // We are close to switch to new track.
                        // To make the switch more "instantenous" we could wait only for as long
                        // as it takes for our track to go to finish.
                        // Add extra timeout to let server fetch new data
                        mEventTimeout = trackLeftMs + EXTRA_EVENT_TIMEOUT;
                        mChangeExpected = true;
                    }
                    else
                    {
                        // Default timeout, track is still somewhere in the middle
                        mEventTimeout = DEFAULT_EVENT_TIMEOUT;
                        mChangeExpected = false;
                    }
                }
            }
            else
            {
                // Track unloaded or stopped; timeout default
                mEventTimeout = DEFAULT_EVENT_TIMEOUT;
                mChangeExpected = false;
            }

            if (stateUpdate != mCurrentStateUpdate)
            {
                mCurrentStateUpdate = stateUpdate;
                mStateUpdateCallback.PublishEvent(stateUpdate);
            }

            mDataAccessMutex.ReleaseMutex();
        }

        void ThreadMain()
        {
            // mShutdownEvent will serve as our "timer"
            while (true)
            {
                if (mShutdownEvent.WaitOne(mEventTimeout))
                {
                    Logger.Log().Debug("Shutdown event triggered - closing");
                    break;
                }

                // no signal means no shutdown requested - continue as normal
                try
                {
                    FetchData();
                }
                catch (System.Exception e)
                {
                    Logger.Log().Warning("Caught exception while fetching data: {0}\n{1}", e.Message, e.StackTrace);
                    Logger.Log().Warning("Ignoring and continuing anyway...");
                }
            }
        }

        public void Run()
        {
            mThread.Start();
        }

        public void RequestShutdown()
        {
            mShutdownEvent.Set();
        }

        public void Wait()
        {
            mThread.Join();
        }

        public API.Spotify.PlaybackState GetPlaybackState()
        {
            mDataAccessMutex.WaitOne();
            API.Spotify.PlaybackState data = mCurrentPlaybackState;
            mDataAccessMutex.ReleaseMutex();
            return data;
        }
    }
}
