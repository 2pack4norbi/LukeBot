using LukeBot.Communication.Common;


namespace LukeBot.Communication
{
    public class EventNotFoundException: LukeBot.Common.Exception
    {
        public EventNotFoundException(string eventName)
            : base(string.Format("Not found event: {0}", eventName))
        {}
    }
}
