using System.Collections.Generic;
using LukeBot.Module;
using LukeBot.Interface;
using CommandLine;

namespace LukeBot
{
    [Verb("list", HelpText = "List enabled modules for current user")]
    public class ModuleListCommand
    {
    }

    [Verb("enable", HelpText = "Enable a module for current user")]
    public class ModuleEnableCommand
    {
        [Value(0, MetaName = "type", Required = false, Default = "", HelpText = "Type of module to enable.")]
        public string Type { get; set; }

        public ModuleEnableCommand()
        {
            Type = "";
        }
    }
    [Verb("disable", HelpText = "Disable a module for current user")]
    public class ModuleDisableCommand
    {
        [Value(0, MetaName = "type", Required = false, Default = "", HelpText = "Type of module to disable.")]
        public string Type { get; set; }

        public ModuleDisableCommand()
        {
            Type = "";
        }
    }

    internal class ModuleCLIProcessor: ICLIProcessor
    {
        private const string COMMAND_NAME = "module";
        private LukeBot mLukeBot;

        void HandleListCommand(ModuleListCommand args, out string msg)
        {
            try
            {
                msg = "Enabled modules:";

                List<ModuleType> modules = mLukeBot.GetCurrentUser().GetEnabledModules();
                foreach (ModuleType m in modules)
                {
                    msg += "\n  " + m.ToConfString();
                }
            }
            catch (System.Exception e)
            {
                msg = "Failed to get enabled modules: " + e.Message;
            }
        }

        void HandleEnableCommand(ModuleEnableCommand args, out string msg)
        {
            try
            {
                ModuleType type = args.Type.GetModuleTypeEnum();
                mLukeBot.GetCurrentUser().EnableModule(type);
                msg = "Enabled module " + type.ToString();
            }
            catch (System.Exception e)
            {
                msg = "Failed to enable module " + args.Type + ": " + e.Message;
            }
        }

        void HandleDisableCommand(ModuleDisableCommand args, out string msg)
        {
            try
            {
                ModuleType type = args.Type.GetModuleTypeEnum();
                mLukeBot.GetCurrentUser().DisableModule(type);
                msg = "Disabled module " + type.ToString();
            }
            catch (System.Exception e)
            {
                msg = "Failed to disable module " + args.Type + ": " + e.Message;
            }
        }

        public void AddCLICommands(LukeBot lb)
        {
            mLukeBot = lb;

            UserInterface.CLI.AddCommand(COMMAND_NAME, (string[] args) =>
            {
                string result = "";
                Parser.Default.ParseArguments<ModuleListCommand, ModuleEnableCommand, ModuleDisableCommand>(args)
                    .WithParsed<ModuleListCommand>((ModuleListCommand args) => HandleListCommand(args, out result))
                    .WithParsed<ModuleEnableCommand>((ModuleEnableCommand args) => HandleEnableCommand(args, out result))
                    .WithParsed<ModuleDisableCommand>((ModuleDisableCommand args) => HandleDisableCommand(args, out result))
                    .WithNotParsed((IEnumerable<Error> errs) => CLIUtils.HandleCLIError(errs, COMMAND_NAME, out result));
                return result;
            });
        }
    }
}