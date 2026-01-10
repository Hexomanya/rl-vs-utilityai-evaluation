using System;
using UnityEngine;

namespace SimpleSkills.Scripts
{
    public static class GameLog
    {
        private const bool PrintToConsole = true;
        public static event Action<string, ISkAgent> OnLogMessage;

        public static void Print(string message, ISkAgent caller = null)
        {
            OnLogMessage?.Invoke(message, caller);

            if(!PrintToConsole) return;
            
            string prefix = caller == null ? "[GameLog]" : $"[Gamelog, {caller.GetName()}]:";
            Debug.Log($"{prefix} {message}");
        }
    }
}
