namespace LukeBot.Interface
{
    /**
     * DummyCLI exists as a noop-replacement for other CLIs.
     *
     * UserInterface class will return this object when LukeBot initializes with
     * non-CLI UI and tries to access UserInterface.CommandLine property.
     */
    public class DummyCLI: CLIBase
    {
        public void AddCommand(string cmd, Command c)
        {
        }

        public void AddCommand(string cmd, CLIBase.CmdDelegate d)
        {
        }

        public bool Ask(string msg)
        {
            return false;
        }

        public void MainLoop()
        {
        }

        public void Message(string msg)
        {
        }

        public string Query(bool maskAnswer, string message)
        {
            return "";
        }

        public void SetPromptPrefix(string prefix)
        {
        }

        public void Teardown()
        {
        }
    }
}