using UnityEngine;
using System.Collections.Generic;

namespace App.Common.Initialize
{
    public class ReferenceHolder
    {
        private Dictionary<System.Type, IInitializable> initializablesDictionary = new Dictionary<System.Type, IInitializable>();
        public void RegisterInitializable(IInitializable initializable)
        {
            if (!initializablesDictionary.ContainsKey(initializable.GetType()))
            {
                initializablesDictionary.Add(initializable.GetType(), initializable);
            }
            else
            {
                Debug.LogWarning($"Initializable of type {initializable.GetType().Name} is already registered.");
            }
        }
        public T GetInitializable<T>() where T : IInitializable
        {
            if (initializablesDictionary.TryGetValue(typeof(T), out var initializable))
            {
                return (T)initializable;
            }
            Debug.LogWarning($"Initializable of type {typeof(T).Name} not found.");
            return default;
        }

        public void ClearReferences()
        {
            initializablesDictionary.Clear();
        }
    }
}
