using LukeBot.Common;

namespace LukeBot
{
    public class ClientContextException: Exception
    {
        public ClientContextException(string msg, params object[] obj)
            : base(string.Format(msg, obj))
        {
        }
    }
}
