using System;
using System.Collections.Generic;
using LukeBot.Logging;
using LukeBot.Communication.Common;


namespace LukeBot.Twitch.Common
{
    public class Events
    {
        public const string TWITCH_CHAT_MESSAGE = "TwitchChatMessage";
        public const string TWITCH_CHAT_CLEAR_MESSAGE = "TwitchChatClearMessage";
        public const string TWITCH_CHAT_CLEAR_USER = "TwitchChatClearUser";

        public const string TWITCH_SUBSCRIPTION = "TwitchSubscription";

        public const string TWITCH_CHANNEL_POINTS_REDEMPTION = "TwitchChannelPointsRedemption";
    }


    // Chat

    public class TwitchChatMessageArgs: EventArgsBase
    {
        public string MessageID { get; private set; }
        public string UserID { get; set; }
        public string Color { get; set; }
        public List<MessageEmote> Emotes { get; private set; }
        public string Nick { get; set; }
        public string DisplayName { get; set; }
        public string Message { get; set; }

        public TwitchChatMessageArgs(string msgID)
            : base(Events.TWITCH_CHAT_MESSAGE)
        {
            MessageID = msgID;
            UserID = "";
            Color = "#dddddd";
            Emotes = new List<MessageEmote>();
            Nick = "";
            DisplayName = "";
            Message = "";
        }

        private string GetEmoteName(string msg, string range)
        {
            int dash = range.IndexOf('-');
            int from = Int32.Parse(range.Substring(0, dash));
            int count = Int32.Parse(range.Substring(dash + 1)) - from + 1;
            return msg.Substring(from, count);
        }

        public void ParseEmotesString(string msg, string emotesStr)
        {
            if (emotesStr.Length == 0)
                return;

            string[] emotes = emotesStr.Split('/');
            foreach (string e in emotes)
            {
                int separatorIdx = e.IndexOf(':');
                string ranges = e.Substring(separatorIdx + 1);
                int firstRangeIdx = ranges.IndexOf(',');
                string name;
                if (firstRangeIdx == -1)
                    name = GetEmoteName(msg, ranges);
                else
                    name = GetEmoteName(msg, ranges.Substring(0, firstRangeIdx));

                Emotes.Add(new MessageEmote(EmoteSource.Twitch, name, e.Substring(0, separatorIdx), 32, 32, e.Substring(separatorIdx + 1)));
            }
        }

        public void AddExternalEmotes(List<MessageEmote> emotes)
        {
            List<MessageEmote> filteredEmotes = new List<MessageEmote>(emotes.Count);
            foreach (MessageEmote e in emotes)
            {
                if (Emotes.Exists(x => x.Name == e.Name))
                {
                    Logger.Log().Debug("Removing external emote {0} from message, duplicated by sub emotes", e.Name);
                    continue;
                }

                filteredEmotes.Add(e);
            }

            Emotes.AddRange(filteredEmotes);
        }
    }

    public class TwitchChatMessageClearArgs: EventArgsBase
    {
        public string Message { get; private set; }
        public string MessageID { get; set; }

        public TwitchChatMessageClearArgs(string message)
            : base(Events.TWITCH_CHAT_CLEAR_MESSAGE)
        {
            Message = message;
            MessageID = "";
        }
    }

    public class TwitchChatUserClearArgs: EventArgsBase
    {
        public string Nick { get; private set; }

        public TwitchChatUserClearArgs(string nick)
            : base(Events.TWITCH_CHAT_CLEAR_USER)
        {
            Nick = nick;
        }
    }

    public enum TwitchSubscriptionType
    {
        New = 0,
        Resub,
        Gift
    }

    public class TwitchSubscriptionDetails
    {
        public TwitchSubscriptionType Type { get; private set; }
        public int Tier { get; protected set; }

        public TwitchSubscriptionDetails()
            : this(1)
        {
        }

        public TwitchSubscriptionDetails(int tier)
            : this(TwitchSubscriptionType.New, tier)
        {
        }

        internal TwitchSubscriptionDetails(TwitchSubscriptionType type, int tier)
        {
            Type = type;
            Tier = tier;
        }

        public virtual void FillStringArgs(IEnumerable<(string a, string v)> args)
        {
            int tier = 1;

            foreach ((string a, string v) a in args)
            {
                switch (a.a)
                {
                case "Tier": tier = Int32.Parse(a.v); break;
                }
            }
        }
    }

    public class TwitchResubscriptionDetails: TwitchSubscriptionDetails
    {
        public int Cumulative { get; private set; }
        public int Streak { get; private set; } // 0 if not shared
        public int Duration { get; private set; }
        // TODO resub message

        public TwitchResubscriptionDetails()
            : this(1, 1, 1, 1)
        {
        }

        public TwitchResubscriptionDetails(int tier, int cumulative, int streak, int duration)
            : base(TwitchSubscriptionType.Resub, tier)
        {
            Cumulative = cumulative;
            Streak = streak;
            Duration = duration;
        }

        public override void FillStringArgs(IEnumerable<(string a, string v)> args)
        {
            int tier = 1;
            int cumulative = 3;
            int streak = 3;
            int duration = 1;

            foreach ((string a, string v) a in args)
            {
                switch (a.a)
                {
                case "Tier": tier = Int32.Parse(a.v); break;
                case "Cumulative": cumulative = Int32.Parse(a.v); break;
                case "Streak": streak = Int32.Parse(a.v); break;
                case "Duration": duration = Int32.Parse(a.v); break;
                }
            }

            Tier = tier;
            Cumulative = cumulative;
            Streak = streak;
            Duration = duration;
        }
    }

    public class TwitchGiftSubscriptionDetails: TwitchSubscriptionDetails
    {
        public int RecipentCount { get; private set; }

        public TwitchGiftSubscriptionDetails()
            : this(1, 1)
        {
        }

        public TwitchGiftSubscriptionDetails(int tier, int recipentCount)
            : base(TwitchSubscriptionType.Gift, tier)
        {
            RecipentCount = recipentCount;
        }

        public override void FillStringArgs(IEnumerable<(string a, string v)> args)
        {
            int tier = 1;
            int recipents = 10;

            foreach ((string a, string v) a in args)
            {
                switch (a.a)
                {
                case "Tier": tier = Int32.Parse(a.v); break;
                case "Recipents": recipents = Int32.Parse(a.v); break;
                }
            }

            Tier = tier;
            RecipentCount = recipents;
        }
    }

    public class TwitchSubscriptionArgs: EventArgsBase
    {
        public string User { get; private set; }
        public string DisplayName { get; private set; }
        public TwitchSubscriptionDetails Details { get; private set; }

        public TwitchSubscriptionArgs(string user, string displayName, TwitchSubscriptionDetails details)
            : base(Events.TWITCH_SUBSCRIPTION)
        {
            User = user;
            DisplayName = displayName;
            Details = details;
        }
    }


    // Channel Points

    public class TwitchChannelPointsRedemptionArgs: EventArgsBase
    {
        public string User { get; private set; }
        public string DisplayName { get; private set; }
        public string Title { get; private set; }

        public TwitchChannelPointsRedemptionArgs(string user, string displayName, string title)
            : base(Events.TWITCH_CHANNEL_POINTS_REDEMPTION)
        {
            User = user;
            DisplayName = displayName;
            Title = title;
        }
    }
}