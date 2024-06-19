namespace LukeBot
{
    internal class EchoCommand: Command
    {
        public string Execute(string[] args)
        {
            return string.Join(' ', args);
        }
    }
}