using System.Net;
using LukeBot.Spotify.Common;
using LukeBot.Communication.Common;
using LukeBot.API;


namespace LukeBot.Spotify
{
    public class Utils
    {
        public static SpotifyMusicTrackChangedArgs DataItemToTrackChangedArgs(API.Spotify.PlaybackStateItem item)
        {
            float duration = item.duration_ms / 1000.0f; // convert to seconds
            string title = item.name;
            string artists = item.artists[0].name;
            for (int i = 1; i < item.artists.Count; ++i)
            {
                artists += ", ";
                artists += item.artists[i].name;
            }

            string label = "";
            bool labelFilled = false;
            // try looking for publisher (type "P")
            foreach (API.Spotify.AlbumCopyright c in item.album.copyrights)
            {
                if (c.type == "P")
                {
                    label = c.text;
                    labelFilled = true;
                    break;
                }
            }

            // if failed - try looking for copyright (type "C")
            if (!labelFilled)
            {
                foreach (API.Spotify.AlbumCopyright c in item.album.copyrights)
                {
                    if (c.type == "C")
                    {
                        label = c.text;
                        labelFilled = true;
                        break;
                    }
                }
            }

            // if we succeeded earlier, reformat the extracted copyright info
            if (labelFilled)
                label = '[' + label + ']';
            else
                label = " ";

            SpotifyMusicTrackChangedArgs ret = new SpotifyMusicTrackChangedArgs(artists, title, label, duration);

            return ret;
        }

        public static SpotifyMusicStateUpdateArgs DataToStateUpdateArgs(API.Spotify.PlaybackState data)
        {
            float progress;
            PlayerState state;

            if (data.code == HttpStatusCode.NoContent || data.progress_ms == null)
            {
                progress = 0.0f;
                state = PlayerState.Unloaded;
            }
            else
            {
                progress = (float)data.progress_ms / 1000.0f; // conversion from ms to s
                state = data.is_playing ? PlayerState.Playing : PlayerState.Stopped;
            }

            SpotifyMusicStateUpdateArgs ret = new SpotifyMusicStateUpdateArgs(state, progress);
            return ret;
        }
    }
}