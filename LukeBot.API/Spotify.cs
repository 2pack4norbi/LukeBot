using System;
using System.Net;
using System.Collections.Generic;
using LukeBot.Common;
using System.Net.Http;

namespace LukeBot.API
{
    public class Spotify
    {
        private const string REQUEST_URI_BASE = "https://api.spotify.com/v1";
        private const string REQUEST_CURRENT_USER_PROFILE = REQUEST_URI_BASE + "/me" ;
        private const string REQUEST_PLAYER_STATE = REQUEST_CURRENT_USER_PROFILE + "/player";
        private const string REQUEST_QUEUE = REQUEST_PLAYER_STATE + "/queue";
        private const string REQUEST_ALBUM = REQUEST_URI_BASE + "/albums/"; // needs ID at the end!
        private const string REQUEST_TRACK = REQUEST_URI_BASE + "/tracks/"; // needs ID at the end!


        public class AlbumCopyright
        {
            public string text { get; set; }
            public string type { get; set; }

            public AlbumCopyright()
            {
            }

            public AlbumCopyright(string text, string type)
            {
                this.text = text;
                this.type = type;
            }
        };

        public class Album: Response
        {
            public List<AlbumCopyright> copyrights { get; set; }

            public Album()
            {
                copyrights = new List<AlbumCopyright>();
            }
        }

        public class Artist
        {
            public string name { get; set; }

            public Artist()
            {
                name = "";
            }
        };

        public class PlaybackStateAlbum
        {
            public string href { get; set; }
            public string id { get; set; }
            public List<AlbumCopyright> copyrights { get; set; }

            public PlaybackStateAlbum()
            {
                href = "";
                id = "";
                copyrights = new List<AlbumCopyright>();
            }
        };

        public class PlaybackStateItem
        {
            public List<Artist> artists { get; set; }
            public PlaybackStateAlbum album { get; set; }
            public int duration_ms { get; set; }
            public string id { get; set; }
            public string name { get; set; }

            public PlaybackStateItem()
            {
                artists = new List<Artist>();
                album = new PlaybackStateAlbum();
                duration_ms = 0;
                id = "";
                name = "";
            }
        };

        public class Track: Response
        {
            public List<Artist> artists { get; set; }
            public string name { get; set; }

            public Track()
            {
                artists = new List<Artist>();
                name = "";
            }
        }

        public class PlaybackState: Response
        {
            public PlaybackStateItem item { get; set; }
            public bool is_playing { get; set; }
            public int? progress_ms { get; set; }

            public override string ToString()
            {
                string artists = item.artists[0].name;
                for (int i = 1; i < item.artists.Count; ++i)
                {
                    artists += ", ";
                    artists += item.artists[i].name;
                }
                return String.Format("{0} - {1} ({2}/{3})", artists, item.name,
                                        progress_ms / 1000.0f, item.duration_ms / 1000.0f);
            }
        };

        public class UserProfile: Response
        {
            public string email { get; set; }
        };


        public static UserProfile GetCurrentUserProfile(Token token)
        {
            return Request.Get<UserProfile>(REQUEST_CURRENT_USER_PROFILE, token);
        }

        public static Album GetAlbum(Token token, string albumID)
        {
            return Request.Get<Album>(REQUEST_ALBUM + albumID, token);
        }

        public static Track GetTrack(Token token, string trackID)
        {
            return Request.Get<Track>(REQUEST_TRACK + trackID, token);
        }

        public static PlaybackState GetPlaybackState(Token token)
        {
            return Request.Get<PlaybackState>(REQUEST_PLAYER_STATE, token);
        }

        public static HttpResponseMessage AddItemToPlaybackQueue(Token token, string itemID)
        {
            Dictionary<string, string> queries = new()
            {
                { "uri", "spotify:track:" + itemID }
            };

            return Request.Post(REQUEST_QUEUE, token, queries);
        }
    }
}