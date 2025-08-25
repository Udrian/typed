using System;
using System.Collections.Generic;

namespace TypeD.Models.Interfaces
{
    /// <summary>
    /// Represents a model that provides functionality for managing resources, including adding, retrieving, and
    /// removing key-value pairs or objects.
    /// </summary>
    /// <remarks>This interface defines methods for working with resources in various forms, such as
    /// individual objects, key-value pairs, or collections. It supports  retrieving resources by type and key, as well
    /// as removing resources by key.</remarks>
    public interface IResourceModel : IModel
    {
        /// <summary>
        /// Adds the specified list of values to the collection.
        /// </summary>
        /// <remarks>The method appends all items in the <paramref name="values"/> list to the collection.
        /// If the list is empty, no changes are made to the collection.</remarks>
        /// <param name="values">The list of values to add. Each value in the list must be a non-null object.</param>
        public void Add(List<object> values);
        /// <summary>
        /// Adds the specified object to the collection.
        /// </summary>
        /// <param name="value">The object to add to the collection. Cannot be <see langword="null"/>.</param>
        public void Add(object value);
        /// <summary>
        /// Adds the specified key and value to the collection.
        /// </summary>
        /// <param name="key">The key associated with the value to add. Cannot be <see langword="null"/> or empty.</param>
        /// <param name="value">The value to associate with the specified key. Can be <see langword="null"/>.</param>
        public void Add(string key, object value);
        /// <summary>
        /// Adds a collection of key-value pairs to the current instance.
        /// </summary>
        /// <remarks>If a key in the provided list already exists, its value may be overwritten. Ensure
        /// that the keys are unique within the list to avoid unintended behavior.</remarks>
        /// <param name="keyValues">A list of tuples where each tuple contains a key as a string and a value as an object. The key cannot be
        /// null or empty.</param>
        public void Add(List<Tuple<string, object>> keyValues);
        /// <summary>
        /// Removes the entry associated with the specified key from the collection.
        /// </summary>
        /// <param name="key">The key of the entry to remove. Cannot be <see langword="null"/> or empty.</param>
        public void Remove(string key);
        /// <summary>
        /// Retrieves the value associated with the specified key and converts it to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to which the value should be converted. Must be a reference type.</typeparam>
        /// <param name="key">The key associated with the value to retrieve. Cannot be null or empty.</param>
        /// <returns>The value associated with the specified key, converted to the specified type <typeparamref name="T"/>.
        /// Returns <see langword="null"/> if the key does not exist or if the value cannot be converted to
        /// <typeparamref name="T"/>.</returns>
        public T Get<T>(string key) where T : class;
        /// <summary>
        /// Retrieves an instance of the specified type from the underlying service container.
        /// </summary>
        /// <remarks>This method resolves the requested type from the service container. If the type is
        /// not registered or cannot be resolved, the method returns <see langword="null"/> instead of throwing an
        /// exception.</remarks>
        /// <typeparam name="T">The type of the object to retrieve. Must be a reference type.</typeparam>
        /// <returns>An instance of the specified type <typeparamref name="T"/> if it is available in the container; otherwise,
        /// <see langword="null"/>.</returns>
        public T Get<T>() where T : class;
    }
}
