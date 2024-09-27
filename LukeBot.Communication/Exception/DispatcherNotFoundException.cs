using LukeBot.Communication.Common;


namespace LukeBot.Communication
{
    public class DispatcherNotFoundException: LukeBot.Common.Exception
    {
        public DispatcherNotFoundException(string dispatcherName)
            : base(string.Format("Not found dispatcher: {0}", dispatcherName))
        {}
    }
}
