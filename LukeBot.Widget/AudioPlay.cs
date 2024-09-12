using System;
using System.Collections.Generic;
using LukeBot.Communication;
using LukeBot.Communication.Common;
using LukeBot.Logging;
using LukeBot.Twitch.Common;
using LukeBot.Widget.Common;
using Newtonsoft.Json;


namespace LukeBot.Widget
{
    /**
     * Widget responsible for playing an audio file available to it from the bot. That's
     * basically it, there is no more functionality.
     *
     * TODO consider adding some sort of "stop playing" event or whatever
     */
    public class AudioPlay: IWidget
    {
        private class AudioPlayInterrupt: EventArgsBase
        {
            public AudioPlayInterrupt()
                : base("AudioPlayInterrupt")
            {
            }
        }

        private class AudioPlayStartPlayback: EventArgsBase
        {
            public string File { get; set; }

            public AudioPlayStartPlayback(string file)
                : base("AudioPlayStartPlayback")
            {
                File = file;
            }
        }

        /*private class AudioPlayWidgetConfig: WidgetConfiguration
        {
            public List<string> Files { get; set; }

            public AudioPlayWidgetConfig()
                : base("AudioPlayWidgetConfig")
            {
                Files = new();
            }

            public override void DeserializeConfiguration(string configString)
            {
                AudioPlayWidgetConfig config = JsonConvert.DeserializeObject<AudioPlayWidgetConfig>(configString);

                Files = config.Files;
            }

            public override void ValidateUpdate(string field, string value)
            {
                switch (field)
                {
                case "Files":
                {
                    // TODO
                    break;
                }
                default:
                    Logger.Log().Warning("Unrecognized AudioPlay Widget config field: {0}", field);
                    break;
                }
            }

            public override void Update(string field, string value)
            {
                switch (field)
                {
                case "Files":
                {
                    Files = new();
                    string[] vals = value.Split(',');
                    // TODO
                    break;
                }
                }
            }

            public override string ToFormattedString()
            {
                string result = "";
                foreach (string f in Files)
                {
                    result += f;
                    result += ',';
                }
                result.Remove(result.Length - 1);
                return "  Files: " + result;
            }
        }*/

        private void AwaitEventCompletion()
        {
            if (!Connected)
                return;

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

        private void OnChannelPoints(object o, EventArgsBase args)
        {
            TwitchChannelPointsRedemptionArgs a = args as TwitchChannelPointsRedemptionArgs;
            if (a == null ||
                a.Title != "Break the silence") // TODO should be configurable
                return; // quiet exit, not of our concern

            // TODO should come from config
            string[] files = [
                "/content/bad_to_the_bone.ogg",
                "/content/gnome_reverb.ogg",
                "/content/megalovania.ogg",
                "/content/metal_pipe_sfx.ogg"
            ];

            Random rng = new Random();
            int fileIdx = rng.Next() % files.Length;
            AudioPlayStartPlayback playback = new(files[fileIdx]);
            SendToWS(playback);
            AwaitEventCompletion();
            // TODO should have its own event for playback stopping?
        }

        private void OnEventInterrupt(object o, EventArgsBase args)
        {
            SendToWS(new AudioPlayInterrupt());
            AwaitEventCompletion();
        }

        protected override void OnConnected()
        {
            //SendToWS(mConfiguration);
            //AwaitEventCompletion();
        }

        protected override void OnConfigurationUpdate()
        {
            // TODO
            //SendToWS(mConfiguration);
            //AwaitEventCompletion();
        }

        public AudioPlay(string lbUser, string id, string name)
            : base(lbUser, "LukeBot.Widget/Widgets/AudioPlay.html", id, name)
        {
            EventCollection collection = Comms.Event.User(mLBUser);

            // TODO this should all be configurable. This widget should be able to:
            //  - Accept events from multiple sources
            //  - Provide its own events so that it is controllable in some way
            collection.Event(Events.TWITCH_CHANNEL_POINTS_REDEMPTION).Endpoint += OnChannelPoints;
            collection.Event(Events.TWITCH_CHANNEL_POINTS_REDEMPTION).InterruptEndpoint += OnEventInterrupt;
        }

        public override WidgetType GetWidgetType()
        {
            return WidgetType.audioplay;
        }

        ~AudioPlay()
        {
        }
    }
}
