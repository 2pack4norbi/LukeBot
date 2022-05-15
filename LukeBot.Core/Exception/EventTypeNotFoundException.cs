﻿namespace LukeBot.Core
{
    public class EventTypeNotFoundException: System.Exception
    {
        public EventTypeNotFoundException(string fmt, params object[] args): base(string.Format(fmt, args)) {}
    }
}
