using LukeBot.Common;

namespace LukeBot
{
    public class ServerCLIException: Exception
    {
        public ServerCLIException(string msg, params object[] obj)
            : base(string.Format(msg, obj))
        {
        }
    }
}
