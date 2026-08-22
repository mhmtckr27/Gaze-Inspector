using System;
using System.Collections.Generic;

namespace GrayWolf.Gaze
{
    /// <summary>
    /// A very slim ContextContainer: type‑keyed storage + Reset/Dispose.
    /// </summary>
    internal sealed class ContextContainer : IDisposable
    {
        // Backing storage: maps each ContextItem type → its instance
        private readonly Dictionary<Type, ContextItem> _items = new();

        public bool Has<T>() where T : ContextItem
        {
            return _items.ContainsKey(typeof(T));
        }
        
        /// <summary>
        /// Retrieves a T of class <c>ContextItem</c> if it was previously created without it being disposed.
        /// </summary>
        /// <typeparam name="T">Is the class which you are trying to fetch. T has to inherit from <c>ContextItem</c></typeparam>
        /// <returns>The value created previously using <![CDATA[Create<T>]]> .</returns>
        /// <exception cref="InvalidOperationException">This is thrown if the value isn't previously created.</exception>
        private T Get<T>() where T : ContextItem
        {
            if (!_items.TryGetValue(typeof(T), out var inst))
                throw new InvalidOperationException($"Context item {typeof(T).Name} not found.");
            return (T)inst;
        }

        public bool TryGet<T>(out T item) where T : ContextItem
        {
            item = null;
            
            if (Has<T>())
            {
                item = Get<T>();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Creates the value of type T.
        /// </summary>
        /// <typeparam name="T">Is the class which you are trying to fetch. T has to inherit from <c>ContextItem</c></typeparam>
        /// <returns>The value of type T created inside the <c>ContextContainer</c>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if you try to create the value of type T again after it is already created.</exception>
        public T Create<T>() where T : ContextItem, new()
        {
            var key = typeof(T);
            if (_items.ContainsKey(key))
                throw new InvalidOperationException($"Context item {key.Name} already exists.");
            var inst = new T();
            _items[key] = inst;
            return inst;
        }

        /// <summary>
        /// Gets the value of type T if the value is previously created otherwise create it.
        /// </summary>
        /// <typeparam name="T">Is the class which you are trying to fetch. T has to inherit from <c>ContextItem</c></typeparam>
        /// <returns>Returns the value of type T which is created or retrieved.</returns>
        public T GetOrCreate<T>() where T : ContextItem, new()
            => _items.TryGetValue(typeof(T), out var inst) ? (T)inst : Create<T>();
       
        public void Set<T>(T item) where T : ContextItem
        {
            _items[typeof(T)] = item;
        }
        
        /// <summary>
        /// Reset and remove the created values.
        /// </summary>
        public void Dispose()
        {
            foreach (var inst in _items.Values)
                inst.Reset();
        }
    }
}