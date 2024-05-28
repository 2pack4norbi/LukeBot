using LukeBot.Logging;
using LukeBot.Widget.Common;


namespace LukeBot.Widget
{
    internal class EmptyWidgetConfiguration: WidgetConfiguration
    {
        public EmptyWidgetConfiguration()
            : base(Constants.EMPTY_WIDGET_CONFIGURATION_NAME)
        {
        }

        public override void DeserializeConfiguration(string configString)
        {
        }

        public override void ValidateUpdate(string field, string value)
        {
        }

        public override void Update(string field, string value)
        {
        }

        public override string ToFormattedString()
        {
            return "";
        }
    }
}