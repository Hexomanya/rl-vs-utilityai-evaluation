using System.Collections.Generic;
using UnityEngine;

namespace _General
{
    [CreateAssetMenu(fileName = "SurveyConfig", menuName = "Configs/SurveyConfig")]
    public class SurveyConfig : ScriptableObject
    {
        public string Name;
        public string SurveyLink;
        public List<SurveyConfigEntry> ConfigEntries = new List<SurveyConfigEntry>();
    }
}
