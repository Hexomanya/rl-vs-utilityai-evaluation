using System;
using System.Collections.Generic;
using UnityEngine;

namespace _General
{
    [Serializable]
    public class SurveyConfigEntry
    {
        [SerializeField] private RunConfig _runConfig;
        [SerializeField] private int _minReplayCount;
        [SerializeField] private string _realName;
        [SerializeField] private string _obfuscatedName;
        
        public RunConfig RunConfig { get => _runConfig; }
        public int MinReplayCount { get => _minReplayCount; }
        public string RealName { get => _realName; }
        public string ObfuscatedName { get => _obfuscatedName; }
        
        [field: NonSerialized] public int PlayCount { get; set; }
    }
}
