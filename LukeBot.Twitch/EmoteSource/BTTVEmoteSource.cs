﻿using System.Collections.Generic;
using LukeBot.Common;
using LukeBot.Auth;
using LukeBot.Twitch.Common;


namespace LukeBot.Twitch
{

    class BTTVEmoteSource: IEmoteSource
    {
        public BTTVEmoteSource()
        {
        }

        public void FetchEmoteSet(ref Dictionary<string, Emote> emoteSet)
        {
            throw new System.NotImplementedException();
        }

        public void GetEmoteInfo()
        {
            throw new System.NotImplementedException();
        }
    }

}
