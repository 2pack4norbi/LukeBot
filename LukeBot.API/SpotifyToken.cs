using LukeBot.Config;


namespace LukeBot.API
{
    public class SpotifyToken: Token
    {
        public SpotifyToken(AuthFlow flow, string lbUser)
            : base(
                Constants.SPOTIFY_SERVICE_NAME,
                lbUser,
                flow,
                "https://accounts.spotify.com/authorize",
                "https://accounts.spotify.com/api/token",
                "https://accounts.spotify.com/api/revoke",
                "http://" + Conf.Get<string>(Common.Constants.PROP_STORE_HTTPS_DOMAIN_PROP) + "/callback/spotify"
            )
        {
        }

        ~SpotifyToken()
        {
        }
    };
}
