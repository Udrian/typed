using System;

namespace TypeD.Models.Interfaces
{
    public interface ILogModel : IModel
    {
        public void AttachLogOutput(string name, Action<string> action);
        public void DetachLogOutput(string name);
        public void Log(string message);
    }
}
