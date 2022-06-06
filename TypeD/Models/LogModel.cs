using System;
using System.Collections.Generic;
using TypeD.Models.Interfaces;

namespace TypeD.Models
{
    internal class LogModel : ILogModel
    {
        // Properties
        private Dictionary<string, Action<string>> LogOutputs { get; set; }

        // Constructors
        public LogModel()
        {
            LogOutputs = new Dictionary<string, Action<string>>();
        }

        public void Init(IResourceModel resourceModel)
        {
        }

        // Functions
        public void AttachLogOutput(string name, Action<string> action)
        {
            if (!LogOutputs.ContainsKey(name))
                LogOutputs.Add(name, action);
        }

        public void DetachLogOutput(string name)
        {
            if (LogOutputs.ContainsKey(name))
                LogOutputs.Remove(name);
        }

        public void Log(string message)
        {
            foreach(var logoutput in LogOutputs.Values)
            {
                logoutput(message);
            }
        }
    }
}
