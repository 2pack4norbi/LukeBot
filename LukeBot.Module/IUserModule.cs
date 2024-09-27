namespace LukeBot.Module
{
    public interface IUserModule
    {
        public void Run();
        public void RequestShutdown(); // TODO replace with single call Shutdown()
        public void WaitForShutdown(); // TODO ^
        public ModuleType GetModuleType();
    }
}
