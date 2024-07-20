using System;
using System.Collections.Generic;
using System.Linq;
using LukeBot.Common;
using LukeBot.Config;
using LukeBot.Globals;
using LukeBot.Interface;
using LukeBot.Module;
using CommandLine;


namespace LukeBot
{
    [Verb("message", HelpText = "Send a test message")]
    public class TestMessageSubverb
    {
    }

    [Verb("ask", HelpText = "Send a test yes/no question")]
    public class TestAskSubverb
    {
    }

    [Verb("query", HelpText = "Send a test query")]
    public class TestQuerySubverb
    {
        [Option('m', "--masked", HelpText = "Flag if the answer should be masked on the terminal")]
        public bool? masked { get; set; }

        public TestQuerySubverb()
        {
        }
    }

    internal class TestCLIProcessor: ICLIProcessor
    {
        private void HandleMessageSubverb(TestMessageSubverb arg, CLIMessageProxy CLI, out string result)
        {
            CLI.Message("This is a test message");

            result = "Test message sent via CLI Proxy - it should be visible above.";
        }

        private void HandleAskSubverb(TestAskSubverb arg, CLIMessageProxy CLI, out string result)
        {
            bool ret = CLI.Ask("This is a test question. Do you agree it is a test question? ");

            result = "You responded " + (ret ? "yes" : "no");
        }

        private void HandleQuerySubverb(TestQuerySubverb arg, CLIMessageProxy CLI, out string result)
        {
            string ret = CLI.Query((arg.masked != null) ? (bool)arg.masked : false, "This is a test query. What would you like to say? Answer here");

            result = "You answered: " + ret;
        }

        public void AddCLICommands(LukeBot lb)
        {
            UserInterface.CLI.AddCommand("test", UserPermissionLevel.Admin, (CLIMessageProxy cliProxy, string[] args) =>
            {
                string result = "";
                Parser.Default.ParseArguments<TestMessageSubverb, TestAskSubverb, TestQuerySubverb>(args)
                    .WithParsed<TestMessageSubverb>((TestMessageSubverb arg) => HandleMessageSubverb(arg, cliProxy, out result))
                    .WithParsed<TestAskSubverb>((TestAskSubverb arg) => HandleAskSubverb(arg, cliProxy, out result))
                    .WithParsed<TestQuerySubverb>((TestQuerySubverb arg) => HandleQuerySubverb(arg, cliProxy, out result))
                    .WithNotParsed((IEnumerable<Error> errs) => CLIUtils.HandleCLIError(errs, "test", out result));
                return result;
            });
        }
    }
}