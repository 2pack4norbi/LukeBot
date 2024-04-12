using System;
using LukeBot.Communication.Common;


namespace LukeBot.Spotify.Common
{
    public class Events
    {
        public const string SPOTIFY_STATE_UPDATE = "SpotifyStateUpdate";
        public const string SPOTIFY_TRACK_CHANGED = "SpotifyTrackChanged";
    }

    public class SpotifyStateUpdateArgs: EventArgsBase
    {
        public PlayerState State { get; private set; }
        public float Progress { get; private set; }

        public SpotifyStateUpdateArgs()
            : this(PlayerState.Unloaded, 0.0f)
        {
        }

        public SpotifyStateUpdateArgs(PlayerState state, float progress)
            : base(Events.SPOTIFY_STATE_UPDATE)
        {
            State = state;
            Progress = progress;
        }

        public static bool operator ==(SpotifyStateUpdateArgs a, SpotifyStateUpdateArgs b)
        {
            return ReferenceEquals(a, b) || !ReferenceEquals(a, null) && a.Equals(b);
        }

        public static bool operator !=(SpotifyStateUpdateArgs a, SpotifyStateUpdateArgs b)
        {
            return !(a == b);
        }

        public override bool Equals(Object o)
        {
            SpotifyStateUpdateArgs other = o as SpotifyStateUpdateArgs;
            return !object.ReferenceEquals(o, null) &&
                (State == other.State && Progress == other.Progress);
        }

        public override int GetHashCode()
        {
            return State.GetHashCode() ^ Progress.GetHashCode();
        }
    }

    public class SpotifyTrackChangedArgs: EventArgsBase
    {
        public string Artists { get; private set; }
        public string Title { get; private set; }
        public string Label { get; private set; }
        public float Duration { get; private set; }
        public string Image { get; private set; }

        public SpotifyTrackChangedArgs(string artists, string title, string label, float duration, string image)
            : base(Events.SPOTIFY_TRACK_CHANGED)
        {
            Artists = artists;
            Title = title;
            Label = label;
            Duration = duration;
            Image = image;
        }

        public override string ToString()
        {
            return String.Format("{0} - {1} ({2})", Artists, Title, Duration);
        }
    };
}
