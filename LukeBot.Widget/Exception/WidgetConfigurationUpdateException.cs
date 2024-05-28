using LukeBot.Common;

namespace LukeBot.Widget
{
    public class WidgetConfigurationUpdateException: Exception
    {
        public WidgetConfigurationUpdateException(string fmt, params object[] args)
            : base(string.Format(fmt, args)) {}
    }
}
