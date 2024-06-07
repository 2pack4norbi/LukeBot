namespace LukeBot.Interface
{
    /**
     * DummyGUI exists as a noop-replacement for other GUIs.
     *
     * UserInterface class will return this object when LukeBot initializes with
     * non-GUI UI and tries to access UserInterface.Graphical property.
     */
    public class DummyGUI: GUI
    {
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

        public void Teardown()
        {
        }
    }
}