using LukeBot.API;
using LukeBot.Config;
using LukeBot.Common;

namespace propmgr;


public class EmptyStoreTemplate: StoreTemplate
{
    public override void Fill(PropertyStore _)
    {
        // empty - store is left as is
    }
}

public class DefaultStoreTemplate: StoreTemplate
{
    // We want to fill the Store with some basic values used by LukeBot.
    // Most have to be edited by user, but listing them gives a general idea what to provide.
    public override void Fill(PropertyStore store)
    {
        store.Add(LukeBot.Common.Constants.PROP_STORE_SERVER_IP_PROP, Property.Create<string>(LukeBot.Common.Constants.DEFAULT_SERVER_IP));
        store.Add(LukeBot.Common.Constants.PROP_STORE_USE_HTTPS_PROP, Property.Create<string>("false"));
        store.Add(LukeBot.Common.Constants.PROP_STORE_HTTPS_DOMAIN_PROP, Property.Create<string>("my.domain.org"));
        store.Add(LukeBot.Common.Constants.PROP_STORE_HTTPS_EMAIL_PROP, Property.Create<string>("my@email.com"));
        store.Add(LukeBot.Common.Constants.PROP_STORE_USERS_PROP, Property.Create<string[]>(new string[] {}));
        store.Add(LukeBot.Common.Constants.PROP_STORE_RECONNECT_COUNT_PROP, Property.Create<int>(10));

        store.Add(LukeBot.Config.Path.Parse("twitch.api_endpoint"), Property.Create<string>(Twitch.DEFAULT_API_URI));
        store.Add(LukeBot.Config.Path.Parse("twitch.login"), Property.Create<string>(LukeBot.Common.Constants.DEFAULT_LOGIN_NAME));
        store.Add(LukeBot.Config.Path.Parse("twitch.client_id"), Property.Create<string>(LukeBot.Common.Constants.DEFAULT_CLIENT_ID_NAME, true));
        store.Add(LukeBot.Config.Path.Parse("twitch.client_secret"), Property.Create<string>(LukeBot.Common.Constants.DEFAULT_CLIENT_SECRET_NAME, true));

        store.Add(LukeBot.Config.Path.Parse("spotify.api_endpoint"), Property.Create<string>(Spotify.DEFAULT_API_URI));
        store.Add(LukeBot.Config.Path.Parse("spotify.client_id"), Property.Create<string>(LukeBot.Common.Constants.DEFAULT_CLIENT_ID_NAME, true));
        store.Add(LukeBot.Config.Path.Parse("spotify.client_secret"), Property.Create<string>(LukeBot.Common.Constants.DEFAULT_CLIENT_SECRET_NAME, true));
    }
}
