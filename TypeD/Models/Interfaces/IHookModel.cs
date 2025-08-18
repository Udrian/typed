using System;
using TypeD.Models.Data;

namespace TypeD.Models.Interfaces
{
    /// <summary>
    /// Defines a contract for managing and triggering hooks, allowing dynamic registration, removal, and execution of
    /// actions associated with hooks.
    /// </summary>
    /// <remarks>This interface provides methods to add, remove, and trigger hooks, supporting both generic
    /// and non-generic hooks.  Hooks are identified by a string key or a specific type derived from <see cref="Hook"]. 
    /// Actions associated with hooks can be executed with optional parameters or strongly-typed hook
    /// instances.</remarks>
    public interface IHookModel : IModel
    {
        /// <summary>
        /// Removes all registered hooks from the current instance.
        /// </summary>
        /// <remarks>This method clears any previously added hooks, leaving the instance in a state with
        /// no hooks. Use this method when you need to reset the instance or ensure no hooks are active.</remarks>
        public void ClearHooks();
        /// <summary>
        /// Registers a hook with the specified name and associated action to be executed when the hook is triggered.
        /// </summary>
        /// <remarks>Hooks provide a mechanism for extending functionality by associating custom actions
        /// with specific events or triggers.</remarks>
        /// <param name="hook">The name of the hook to register. Cannot be null or empty.</param>
        /// <param name="action">The action to execute when the hook is triggered. The action receives an object parameter containing
        /// hook-specific data. Cannot be null.</param>
        public void AddHook(string hook, Action<object> action);
        /// <summary>
        /// Registers a hook of the specified type and associated action to be executed when the hook is triggered.
        /// </summary>
        /// <remarks>Hooks provide a mechanism for extending functionality by associating custom actions
        /// with specific events or triggers.</remarks>
        /// <typeparam name="T">The type of the hook to add. Must inherit from <see cref="Hook"/> and have a parameterless constructor.</typeparam>
        /// <param name="action">The action to execute when the hook is triggered.</param>
        public void AddHook<T>(Action<T> action) where T : Hook, new();
        /// <summary>
        /// Removes a previously registered hook by its identifier.
        /// </summary>
        /// <remarks>Use this method to unregister a hook that was previously added. If the specified hook
        /// does not exist,  the method performs no action.</remarks>
        /// <param name="hook">The identifier of the hook to remove. Cannot be null or empty.</param>
        public void RemoveHook(string hook);
        /// <summary>
        /// Removes a previously registered hook and its associated action.
        /// </summary>
        /// <remarks>Use this method to unregister a specific action from a hook. If the specified hook or
        /// action does not exist, the method has no effect.</remarks>
        /// <param name="hook">The name of the hook to remove. This cannot be null or empty.</param>
        /// <param name="action">The action associated with the hook to remove. This cannot be null.</param>
        public void RemoveHook(string hook, Action<object> action);
        /// <summary>
        /// Removes all previously registered hook of the specified type.
        /// </summary>
        /// <remarks>If no hook of the specified type is registered, this method has no effect.</remarks>
        /// <typeparam name="T">The type of the hook to remove. Must be a class that derives from <see cref="Hook"/> and has a parameterless
        /// constructor.</typeparam>
        public void RemoveHook<T>() where T : Hook, new();
        /// <summary>
        /// Removes a previously registered hook of the specified type.
        /// </summary>
        /// <remarks>If the specified hook type or action is not found, the method has no effect.</remarks>
        /// <typeparam name="T">The type of the hook to remove. Must be a class that derives from <see cref="Hook"/> and has a parameterless
        /// constructor.</typeparam>
        /// <param name="action">The action associated with the hook to remove. This must match the action that was used when the hook was
        /// registered.</param>
        public void RemoveHook<T>(Action<T> action) where T : Hook, new();
        /// <summary>
        /// Executes a specified hook with the provided parameter.
        /// </summary>
        /// <remarks>The behavior of the method depends on the implementation of the specified hook.
        /// Ensure that the hook name is valid and recognized by the system.</remarks>
        /// <param name="hook">The name of the hook to execute. Cannot be null or empty.</param>
        /// <param name="param">An object containing the data or context to pass to the hook. Can be null if the hook does not require a
        /// parameter.</param>
        public void Shoot(string hook, object param);
        /// <summary>
        /// Executes the specified hook and returns the result.
        /// </summary>
        /// <typeparam name="T">The type of the hook to execute. Must inherit from <see cref="Hook"/> and have a parameterless constructor.</typeparam>
        /// <param name="hook">The hook instance to execute. Cannot be null.</param>
        /// <returns>The result of executing the specified hook.</returns>
        public T Shoot<T>(T hook) where T : Hook, new();
        /// <summary>
        /// Creates and returns a new instance of the specified hook type.
        /// </summary>
        /// <typeparam name="T">The type of hook to create. Must be a class that derives from <see cref="Hook"/> and has a parameterless
        /// constructor.</typeparam>
        /// <returns>A new instance of the specified hook type <typeparamref name="T"/>.</returns>
        public T Shoot<T>() where T : Hook, new();
    }
}
