namespace LukeBot.Interface
{
    public interface InterfaceBase
    {
        /**
         * Send a non-interactive message to user's interface
         */
        void Message(string msg);

        /**
         * Ask a user a yes/no question. Should return true if answered "yes"
         * or "y", false otherwise.
         */
        bool Ask(string msg);

        /**
         * Query a user for an answer. If the answer is supposed to be sensitive,
         * @p maskAnswer will be set to true.
         */
        string Query(bool maskAnswer, string message);

        void MainLoop();
        void Teardown();
    }
}
