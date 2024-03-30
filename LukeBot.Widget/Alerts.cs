using LukeBot.Communication;
using LukeBot.Communication.Common;
using LukeBot.Logging;
using LukeBot.Twitch.Common;
using LukeBot.Widget.Common;


namespace LukeBot.Widget
{
    /**
     * Widget responsible for everything that could be considered an "Alert".
     *
     * Currently supported events
     *  - TwitchSubscription
     *
     * Widget assumes events come from a Queued Dispatcher and are processed
     * one at a time. Once an event arrives, Widget will block execution until
     * the JS side responds back with a WidgetEventCompletionResponse object.
     */
    public class Alerts: IWidget
    {
        private class AlertInterrupt : EventArgsBase
        {
            public AlertInterrupt()
                : base("AlertInterrupt")
            {
            }
        }

        private class AlertWidgetConfig : EventArgsBase
        {
            public string Alignment { get; set; }

            public AlertWidgetConfig()
                : base("AlertWidgetConfig")
            {
                Alignment = "right";
            }
        }

        private void AwaitEventCompletion()
        {
            WidgetEventCompletionResponse resp = RecvFromWS<WidgetEventCompletionResponse>();
            if (resp == null)
            {
                Logger.Log().Warning("Widget's response was null - possibly connection was broken or is not connected");
                return;
            }

            if (resp.Status != 0)
            {
                Logger.Log().Warning("Widget failed to complete the event: {0}", resp.Reason);
            }
            else
            {
                Logger.Log().Debug("Widget completed event");
            }
        }

        private void OnSimpleEvent<T>(object o, EventArgsBase args)
            where T : EventArgsBase
        {
            T a = args as T;
            SendToWS(a);
            AwaitEventCompletion();
        }

        private void OnSubscriptionEvent(object o, EventArgsBase args)
        {
            TwitchSubscriptionArgs a = args as TwitchSubscriptionArgs;

            switch (a.Details.Type)
            {
            case TwitchSubscriptionType.New:
                Logger.Log().Debug("New sub from: {0} ({1}), tier {2}", a.User, a.DisplayName, a.Details.Tier);
                break;
            case TwitchSubscriptionType.Resub:
                TwitchResubscriptionDetails resub = a.Details as TwitchResubscriptionDetails;
                Logger.Log().Debug("Resub from: {0} ({1}), tier {2}, cumulative {3}, streak {4}, duration {5} msg {6}",
                    a.User, a.DisplayName, resub.Tier, resub.Cumulative, resub.Streak, resub.Duration, resub.Message);
                break;
            case TwitchSubscriptionType.Gift:
                TwitchGiftSubscriptionDetails gift = a.Details as TwitchGiftSubscriptionDetails;
                Logger.Log().Debug("Gift from: {0} ({1}), tier {2}, count {3}",
                    a.User, a.DisplayName, gift.Tier, gift.RecipentCount);
                break;
            }

            SendToWS(a);

            AwaitEventCompletion();
        }

        private void OnEventInterrupt(object o, EventArgsBase args)
        {
            SendToWS(new AlertInterrupt());
        }

        protected override void OnConnected()
        {
            AlertWidgetConfig config = new AlertWidgetConfig();
            // TODO hacky, make it work properly and implement widget config system
            WidgetDesc desc = GetDesc();
            if (desc.Name != null && desc.Name.EndsWith("_Left"))
                config.Alignment = "left";
            SendToWS(config);

            AwaitEventCompletion();
        }

        public Alerts(string lbUser, string id, string name)
            : base("LukeBot.Widget/Widgets/Alerts.html", id, name)
        {
            EventCollection collection = Comms.Event.User(lbUser);

            collection.Event(Events.TWITCH_CHANNEL_POINTS_REDEMPTION).Endpoint += OnSimpleEvent<TwitchChannelPointsRedemptionArgs>;
            collection.Event(Events.TWITCH_CHANNEL_POINTS_REDEMPTION).InterruptEndpoint += OnEventInterrupt;

            collection.Event(Events.TWITCH_CHEER).Endpoint += OnSimpleEvent<TwitchCheerArgs>;
            collection.Event(Events.TWITCH_CHEER).InterruptEndpoint += OnEventInterrupt;

            collection.Event(Events.TWITCH_SUBSCRIPTION).Endpoint += OnSubscriptionEvent;
            collection.Event(Events.TWITCH_SUBSCRIPTION).InterruptEndpoint += OnEventInterrupt;
        }

        public override WidgetType GetWidgetType()
        {
            return WidgetType.alerts;
        }

        ~Alerts()
        {
        }
    }
}
