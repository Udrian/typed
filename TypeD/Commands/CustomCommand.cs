using System;
using System.Windows.Input;
using TypeD.Models.Interfaces;

namespace TypeD.Commands
{
    public class CustomCommand : ICommand
    {
        // Models
        public IResourceModel ResourceModel { get; private set; }

        private readonly Predicate<object> canExecute;
        private readonly Action<object> execute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        // Constructors
        public CustomCommand(IResourceModel resourceModel = null) : this(null, null, resourceModel) { }
        public CustomCommand(Action<object> execute, IResourceModel resourceModel = null) : this(execute, null, resourceModel) { }
        public CustomCommand(Action<object> execute, Predicate<object> canExecute, IResourceModel resourceModel = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
            ResourceModel = resourceModel;
        }

        // Functions
        public virtual bool CanExecute(object parameter)
        {
            if (canExecute == null)
            {
                return true;
            }

            return canExecute(parameter);
        }

        public virtual void Execute(object parameter)
        {
            execute(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public abstract class CustomCommand<T> : CustomCommand where T: class
    {
        // Constructors
        public CustomCommand(IResourceModel resourceModel = null) : base(null, null, resourceModel) { }

        // Functions
        public override bool CanExecute(object parameter)
        {
            if (parameter is T)
                return CanExecute(parameter as T);
            return true;
        }

        public virtual bool CanExecute(T parameter)
        {
            return true;
        }

        public override void Execute(object parameter)
        {
            if (parameter is T)
                Execute(parameter as T);
        }

        public abstract void Execute(T parameter);
    }
}
