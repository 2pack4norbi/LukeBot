using LukeBot.Common;

namespace LukeBot
{
    public class InterfaceNotInitializedException: Exception
    {
        public InterfaceNotInitializedException()
            : base("Interface is not initialized")
        {
        }
    }
}
