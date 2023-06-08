using System.Collections.Generic;
using LukeBot.Common;
using LukeBot.Globals;
using LukeBot.Interface;
using LukeBot.Widget.Common;
using CommandLine;


namespace LukeBot
{
    [Verb("add", HelpText = "Add widget for user")]
    public class WidgetAddCommand
    {
        [Value(0, MetaName = "type", Required = true, HelpText = "Type of widget to add")]
        public WidgetType Type { get; set; }

        [Value(1, MetaName = "name", Default = "", Required = false, HelpText = "User-friendly name of widget")]
        public string Name { get; set; }

        public WidgetAddCommand()
        {
            Type = WidgetType.invalid;
            Name = "";
        }
    }

    [Verb("address", HelpText = "Get widget's address")]
    public class WidgetAddressCommand
    {
        [Value(0, MetaName = "id", Required = true, HelpText = "Widget's ID")]
        public string Id { get; set; }

        public WidgetAddressCommand()
        {
            Id = "";
        }
    }

    [Verb("list", HelpText = "List available widgets")]
    public class WidgetListCommand
    {
    }

    [Verb("info", HelpText = "Get more info on widget")]
    public class WidgetInfoCommand
    {
        [Value(0, MetaName = "id", Required = true, HelpText = "Widget's ID")]
        public string Id { get; set; }

        public WidgetInfoCommand()
        {
            Id = "";
        }
    }

    [Verb("delete", HelpText = "Delete widget")]
    public class WidgetDeleteCommand
    {
        [Value(0, MetaName = "id", Required = true, HelpText = "Widget's ID")]
        public string Id { get; set; }

        public WidgetDeleteCommand()
        {
            Id = "";
        }
    }

    [Verb("enable", HelpText = "Enable Widget support for current user.")]
    public class WidgetEnableCommand
    {
    }

    [Verb("disable", HelpText = "Enable Widget support for current user.")]
    public class WidgetDisableCommand
    {
    }

    internal class WidgetCLIProcessor: ICLIProcessor
    {
        private LukeBot mLukeBot;

        public void HandleAdd(WidgetAddCommand cmd, out string msg)
        {
            string addr;
            try
            {
                string lbUser = mLukeBot.GetCurrentUser().Username;
                addr = GlobalModules.Widget.AddWidget(lbUser, cmd.Type, cmd.Name);
            }
            catch (System.Exception e)
            {
                msg = "Failed to add widget: " + e.Message;
                return;
            }

            msg = "Added new widget at address: " + addr;
        }

        public void HandleAddress(WidgetAddressCommand cmd, out string msg)
        {
            WidgetDesc wd;

            try
            {
                string lbUser = mLukeBot.GetCurrentUser().Username;
                wd = GlobalModules.Widget.GetWidgetInfo(lbUser, cmd.Id);
            }
            catch (System.Exception e)
            {
                msg = "Failed to get widget's address: " + e.Message;
                return;
            }

            msg = wd.Address;
        }

        public void HandleList(WidgetListCommand cmd, out string msg)
        {
            List<WidgetDesc> widgets;

            try
            {
                string lbUser = mLukeBot.GetCurrentUser().Username;
                widgets = GlobalModules.Widget.ListUserWidgets(lbUser);
            }
            catch (System.Exception e)
            {
                msg = "Failed to list widgets: " + e.Message;
                return;
            }

            msg = "Available widgets:";
            foreach (WidgetDesc w in widgets)
            {
                msg += "\n  " + w.Id + " (";
                if (w.Name.Length > 0)
                    msg += w.Name + ", ";
                msg += w.Type.ToString() + ")";
            }
        }

        public void HandleInfo(WidgetInfoCommand cmd, out string msg)
        {
            WidgetDesc wd;

            try
            {
                string lbUser = mLukeBot.GetCurrentUser().Username;
                wd = GlobalModules.Widget.GetWidgetInfo(lbUser, cmd.Id);
            }
            catch (System.Exception e)
            {
                msg = "Failed to get widget info: " + e.Message;
                return;
            }

            msg = "Widget " + cmd.Id + " info:\n" + wd.ToFormattedString();
        }

        public void HandleDelete(WidgetDeleteCommand cmd, out string msg)
        {
            try
            {
                string lbUser = mLukeBot.GetCurrentUser().Username;
                GlobalModules.Widget.DeleteWidget(lbUser, cmd.Id);
            }
            catch (System.Exception e)
            {
                msg = "Failed to delete widget: " + e.Message;
                return;
            }

            msg = "Widget " + cmd.Id + " deleted.";
        }

        public void HandleEnable(WidgetEnableCommand arg, out string msg)
        {
            msg = "";

            try
            {
                mLukeBot.GetCurrentUser().EnableModule(Module.ModuleType.Widget);
                msg = "Enabled module " + Module.ModuleType.Widget;
            }
            catch (System.Exception e)
            {
                msg = "Failed to enable Widget module: " + e.Message;
            }
        }

        public void HandleDisable(WidgetDisableCommand arg, out string msg)
        {
            msg = "";

            try
            {
                mLukeBot.GetCurrentUser().DisableModule(Module.ModuleType.Widget);
                msg = "Disabled module " + Module.ModuleType.Widget;
            }
            catch (System.Exception e)
            {
                msg = "Failed to disable Widget module: " + e.Message;
            }
        }

        public void AddCLICommands(LukeBot lb)
        {
            mLukeBot = lb;

            CLI.Instance.AddCommand(Constants.WIDGET_MODULE_NAME, (string[] args) =>
            {
                string result = "";
                Parser.Default.ParseArguments<WidgetAddCommand, WidgetAddressCommand, WidgetListCommand, WidgetInfoCommand,
                        WidgetDeleteCommand, WidgetEnableCommand, WidgetDisableCommand>(args)
                    .WithParsed<WidgetAddCommand>((WidgetAddCommand arg) => HandleAdd(arg, out result))
                    .WithParsed<WidgetAddressCommand>((WidgetAddressCommand arg) => HandleAddress(arg, out result))
                    .WithParsed<WidgetListCommand>((WidgetListCommand arg) => HandleList(arg, out result))
                    .WithParsed<WidgetInfoCommand>((WidgetInfoCommand arg) => HandleInfo(arg, out result))
                    .WithParsed<WidgetDeleteCommand>((WidgetDeleteCommand arg) => HandleDelete(arg, out result))
                    .WithParsed<WidgetEnableCommand>((WidgetEnableCommand arg) => HandleEnable(arg, out result))
                    .WithParsed<WidgetDisableCommand>((WidgetDisableCommand arg) => HandleDisable(arg, out result))
                    .WithNotParsed((IEnumerable<Error> errs) => CLIUtils.HandleCLIError(errs, Constants.WIDGET_MODULE_NAME, out result));
                return result;
            });
        }
    }
}
