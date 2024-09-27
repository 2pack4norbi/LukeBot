using LukeBot.Communication;
using LukeBot.Communication.Common;
using LukeBot.Twitch.Common;
using LukeBot.Widget.Common;


namespace LukeBot.Widget
{
    /**
     * Widget used to display messages from Twitch Chat.
     *
     * Reacts to following events:
     *  - TwitchChatMessage - new message sent on Twitch chat
     *  - TwitchChatClearUser - request to remove messages from selected user
     *  - TwitchChatClearMessage - request to remove a specific message
     */
    public class Chat: IWidget
    {
        private void OnMessage(object o, EventArgsBase args)
        {
            SendToWS((TwitchChatMessageArgs)args);
        }

        private void OnClearChat(object o, EventArgsBase args)
        {
            SendToWS((TwitchChatUserClearArgs)args);
        }

        private void OnClearMsg(object o, EventArgsBase args)
        {
            SendToWS((TwitchChatMessageClearArgs)args);
        }

        protected override void OnConnected()
        {
            // noop
        }

        protected override void OnLoad()
        {
            Comms.Event.User(mLBUser).Event(Events.TWITCH_CHAT_MESSAGE).Endpoint += OnMessage;
            Comms.Event.User(mLBUser).Event(Events.TWITCH_CHAT_CLEAR_USER).Endpoint += OnClearChat;
            Comms.Event.User(mLBUser).Event(Events.TWITCH_CHAT_CLEAR_MESSAGE).Endpoint += OnClearMsg;
        }

        protected override void OnUnload()
        {
            Comms.Event.User(mLBUser).Event(Events.TWITCH_CHAT_MESSAGE).Endpoint -= OnMessage;
            Comms.Event.User(mLBUser).Event(Events.TWITCH_CHAT_CLEAR_USER).Endpoint -= OnClearChat;
            Comms.Event.User(mLBUser).Event(Events.TWITCH_CHAT_CLEAR_MESSAGE).Endpoint -= OnClearMsg;
        }

        public Chat(string lbUser, string id, string name)
            : base(lbUser, "LukeBot.Widget/Widgets/Chat.html", id, name)
        {
        }

        public override WidgetType GetWidgetType()
        {
            return WidgetType.chat;
        }

        ~Chat()
        {
        }
    }
}
