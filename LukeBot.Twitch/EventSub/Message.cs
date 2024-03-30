using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using Newtonsoft.Json;

namespace LukeBot.Twitch.EventSub
{
    internal enum MessageType
    {
        none = 0,
        session_welcome,
        session_keepalive,
        session_reconnect,
        notification,
        revocation
    }

    internal enum InternalStatus
    {
        Fine = 0,
        Closed,
        Reconnect
    }



    internal class Metadata
    {
        public string message_id { get; set; }
        public MessageType message_type { get; set; }
        public DateTime message_timestamp { get; set; }
    }



    internal class PayloadSession
    {
        public string id { get; set; }
        public string status { get; set; }
        public DateTime connected_at { get; set; }
        public int? keepalive_timeout_seconds { get; set; }
        public string reconnect_url { get; set; }
    }



    internal class PayloadConditionBase
    {
    }

    internal class PayloadBroadcasterCondition: PayloadConditionBase
    {
        public string broadcaster_user_id { get; set; }
    }

    internal class PayloadBroadcasterModeratorCondition: PayloadConditionBase
    {
        public string broadcaster_user_id { get; set; }
        public string moderator_user_id { get; set; }
    }

    internal class PayloadChannelPointCondition: PayloadConditionBase
    {
        public string broadcaster_user_id { get; set; }
        public string reward_id { get; set; }
    }

    internal class PayloadRaidCondition: PayloadConditionBase
    {
        public string from_broadcaster_user_id { get; set; }
        public string to_broadcaster_user_id { get; set; }
    }

    internal class PayloadUserCondition: PayloadConditionBase
    {
        public string user_id { get; set; }
    }

    internal class PayloadSubscriptionTransport
    {
        public string method { get; set; }
        public string session_id { get; set; }
    }

    internal class PayloadSubscription
    {
        public string id { get; set; }
        public string status { get; set; }
        public string type { get; set; }
        public string version { get; set; }
        public int cost { get; set; }
        public PayloadConditionBase condition { get; set; }
        public PayloadSubscriptionTransport transport { get; set; }
        public DateTime created_at { get; set; }
    }



    internal class PayloadEvent
    {
        public string broadcaster_user_id { get; set; }
        public string broadcaster_user_login { get; set; }
        public string broadcaster_user_name { get; set; }
        public string user_id { get; set; }
        public string user_login { get; set; }
        public string user_name { get; set; }
    }

    internal class PayloadEventSubMessageEmote
    {
        public int begin { get; set; }
        public int end { get; set; }
        public string id { get; set; }
    }

    internal class PayloadEventSubMessage: PayloadEvent
    {
        public string text { get; set; }
        public List<PayloadEventSubMessageEmote> emotes { get; set; }
    }

    internal class PayloadSubEvent: PayloadEvent
    {
        public string tier { get; set; }
        public bool is_gift { get; set; }
    }

    internal class PayloadSubMessageEvent: PayloadEvent
    {
        public string tier { get; set; }
        public PayloadEventSubMessage message { get; set; }
        public int cumulative_months { get; set; }
        public int? streak_months { get; set; } // null if user doesn't share this information
        public int duration_months { get; set; }
    }

    internal class PayloadSubGiftEvent: PayloadEvent
    {
        public string tier { get; set; }
        public int total { get; set; }
        public int? cumulative_total { get; set; } // null if (is_anonymous == true)
        public bool is_anonymous { get; set; }
    }

    internal class PayloadRewardEvent
    {
        public string id { get; set; }
        public string title { get; set; }
        public int cost { get; set; }
        public string prompt { get; set; }
    }

    internal class PayloadChannelPointRedemptionEvent: PayloadEvent
    {
        public string id { get; set; }
        public string user_input { get; set; }
        public string status { get; set; }
        public PayloadRewardEvent reward { get; set; }
        public DateTime redeemed_at { get; set; }
    }

    internal class PayloadCheerEvent: PayloadEvent
    {
        public bool is_anonymous { get; set; }
        public string message { get; set; }
        public int bits { get; set; }
    }



    internal class Payload
    {
        [JsonProperty("session")]
        public PayloadSession Session { get; set; }
        [JsonProperty("subscription")]
        public PayloadSubscription Subscription { get; set; }
        [JsonProperty("event")]
        public PayloadEvent Event { get; set; }
    }

    internal class Message
    {
        [JsonProperty("metadata")]
        public Metadata Metadata { get; set; }
        [JsonProperty("payload")]
        public Payload Payload { get; set; }

        // LukeBot-internal fields
        public bool Success = false;
        public InternalStatus Status = InternalStatus.Fine;
    }
}