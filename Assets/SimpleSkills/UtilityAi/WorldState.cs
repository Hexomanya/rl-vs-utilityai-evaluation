using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleSkills.UtilityAi
{
    [System.Serializable]
    public class WorldState
    {
        private Dictionary<WorldDataKey, object> _data = new Dictionary<WorldDataKey, object>();

        public T GetData<T>(WorldDataKey key, T defaultValue = default(T))
        {
            if(!_data.TryGetValue(key, out object value))
            {
                Debug.LogWarning($"Could not retrieve data for key {key}. You have to set it! Returning default!");
                return defaultValue;
            }
            
            if(value is T typedValue) return typedValue;

            if(typeof(T) == typeof(int) && value is float or double)
            {
                int newValue = Convert.ToInt32(value);
                if(newValue is T safeInt) return safeInt;
            }

            Debug.LogWarning($"WorldState returned default value. This will lead to unexpected ai behaviour! Value type is {value.GetType()}, requested type is {typeof(T)}");
            return defaultValue;
        }

        public void SetData<T>(WorldDataKey key, T value)
        {
            if(_data.TryGetValue(key, out object retrievedValue) && retrievedValue is not T)
            {
                Debug.LogWarning($"Tried to set value with key {key}, but the stored valued is from another type. This is not supported!");
                return;
            }

            
            _data[key] = value;
        }
    }
}
