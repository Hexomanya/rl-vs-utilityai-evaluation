using System;
using System.Collections.Generic;
using UnityEngine;

namespace _General
{
    public abstract class ScriptableEvent<T> : ScriptableObject
    {
        private event Action<T> OnEventRaised;
        
        public void Subscribe(Action<T> listener)
        {
            OnEventRaised += listener;
        }

        public void Unsubscribe(Action<T> listener)
        {
            OnEventRaised -= listener;
        }

        public void Raise(T item)
        {
            OnEventRaised?.Invoke(item);
        }
    }
}
