using System.Windows.Input;
using TypeD.Models.Interfaces;
using TypeD.ViewModel;

namespace TypeD.Commands
{
    /// <summary>
    /// Base ICommand implementation that runs delegates.
    /// </summary>
    public class CustomCommand : ICommand
    {
        // Models
        /// <summary>Gets the resource model.</summary>
        public IResourceModel ResourceModel { get { return ViewModelBase.ResourceModel; } }

        private readonly Predicate<object> canExecute;
        private readonly Action<object> execute;

        /// <summary>Not used; the command's executability is queried on demand.</summary>
        public event EventHandler CanExecuteChanged
        {
            add {  }
            remove {  }
        }

        // Constructors
        /// <summary>Initializes a new instance of the <see cref="CustomCommand"/> class without delegates.</summary>
        public CustomCommand() : this(null, null) { }
        /// <summary>Initializes a new instance of the <see cref="CustomCommand"/> class that is always executable.</summary>
        /// <param name="execute">The action to run.</param>
        public CustomCommand(Action<object> execute) : this(execute, null) { }
        /// <summary>Initializes a new instance of the <see cref="CustomCommand"/> class.</summary>
        /// <param name="execute">The action to run.</param>
        /// <param name="canExecute">The condition that decides whether the command can run.</param>
        public CustomCommand(Action<object> execute, Predicate<object> canExecute)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        // Functions
        /// <inheritdoc/>
        public virtual bool CanExecute(object parameter)
        {
            if (canExecute == null)
            {
                return true;
            }

            return canExecute(parameter);
        }

        /// <inheritdoc/>
        public virtual void Execute(object parameter)
        {
            execute(parameter);
        }
    }

    /// <summary>
    /// Base class for commands whose parameter is of a specific type.
    /// </summary>
    /// <typeparam name="T">The parameter type.</typeparam>
    public abstract class CustomCommand<T> : CustomCommand where T: class
    {
        // Constructors
        /// <summary>Initializes a new instance of the <see cref="CustomCommand{T}"/> class.</summary>
        /// <param name="resourceModel">The resource model (unused).</param>
        public CustomCommand(IResourceModel resourceModel = null) : base(null, null) { }

        // Functions
        /// <inheritdoc/>
        public override bool CanExecute(object parameter)
        {
            return parameter is T typedParameter && CanExecute(typedParameter);
        }

        /// <summary>Determines whether the command can run with a typed parameter.</summary>
        /// <param name="parameter">The typed parameter.</param>
        /// <returns><c>true</c> by default.</returns>
        public virtual bool CanExecute(T parameter)
        {
            return true;
        }

        /// <inheritdoc/>
        public override void Execute(object parameter)
        {
            if (parameter is T)
                Execute(parameter as T);
        }

        /// <summary>Runs the command with a typed parameter.</summary>
        /// <param name="parameter">The typed parameter.</param>
        public abstract void Execute(T parameter);
    }
}
