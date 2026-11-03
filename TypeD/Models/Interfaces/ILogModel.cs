using System;

namespace TypeD.Models.Interfaces
{
    /// <summary>Model that distributes log messages to attached outputs.</summary>
    public interface ILogModel : IModel
    {
        /// <summary>Attaches a named log output.</summary>
        /// <param name="name">The unique name of the output.</param>
        /// <param name="action">The action that receives log messages.</param>
        public void AttachLogOutput(string name, Action<string> action);
        /// <summary>Detaches a named log output.</summary>
        /// <param name="name">The name of the output.</param>
        public void DetachLogOutput(string name);
        /// <summary>Writes a message to all attached outputs.</summary>
        /// <param name="message">The message.</param>
        public void Log(string message);
    }
}
