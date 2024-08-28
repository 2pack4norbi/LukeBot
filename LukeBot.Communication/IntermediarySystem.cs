using System;
using System.Collections.Generic;


namespace LukeBot.Communication
{
    public sealed class IntermediarySystem
    {
        private Dictionary<string, Intermediary> mServices = new Dictionary<string, Intermediary>();

        public IntermediarySystem()
        {
        }

        public void Register(string service)
        {
            mServices.Add(service, new Intermediary());
        }

        public Intermediary GetIntermediary(string service)
        {
            return mServices[service];
        }
    }
}
