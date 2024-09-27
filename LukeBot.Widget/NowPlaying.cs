using LukeBot.Communication;
using LukeBot.Communication.Common;
using LukeBot.Spotify.Common;
using LukeBot.Widget.Common;


namespace LukeBot.Widget
{
    /**
     * Widget reflecting currently played track on Spotify service.
     *
     * Reacts to events:
     *  - SpotifyStateUpdate
     *  - SpotifyTrackChanged
     */
    public class NowPlaying: IWidget
    {
        SpotifyStateUpdateArgs mState;
        SpotifyTrackChangedArgs mCurrentTrack;

        private void OnStateUpdate(object o, EventArgsBase args)
        {
            SpotifyStateUpdateArgs a = (SpotifyStateUpdateArgs)args;
            mState = a;
            SendToWS(a);
        }

        private void OnTrackChanged(object o, EventArgsBase args)
        {
            SpotifyTrackChangedArgs a = (SpotifyTrackChangedArgs)args;
            mCurrentTrack = a;
            SendToWS(a);
        }

        protected override void OnConnected()
        {
            if (mState != null && mState.State != PlayerState.Unloaded)
            {
                // Push a state update to "pre-refresh" the widget
                OnTrackChanged(null, mCurrentTrack);
                OnStateUpdate(null, mState);
            }
        }

        protected override void OnLoad()
        {
            Comms.Event.User(mLBUser).Event(Events.SPOTIFY_STATE_UPDATE).Endpoint += OnStateUpdate;
            Comms.Event.User(mLBUser).Event(Events.SPOTIFY_TRACK_CHANGED).Endpoint += OnTrackChanged;
        }

        protected override void OnUnload()
        {
            Comms.Event.User(mLBUser).Event(Events.SPOTIFY_STATE_UPDATE).Endpoint -= OnStateUpdate;
            Comms.Event.User(mLBUser).Event(Events.SPOTIFY_TRACK_CHANGED).Endpoint -= OnTrackChanged;
        }

        public NowPlaying(string lbUser, string id, string name)
            : base(lbUser, "LukeBot.Widget/Widgets/NowPlaying.html", id, name)
        {
            mState = null;
            mCurrentTrack = null;
        }

        public override WidgetType GetWidgetType()
        {
            return WidgetType.nowplaying;
        }

        ~NowPlaying()
        {
        }
    }
}
