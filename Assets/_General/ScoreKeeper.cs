using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace _General
{
    public enum ScoreType
    {
        Win,
        Lose,
        Draw,
        Death,
        Kill,
    }
    
    public class ScoreKeeper
    {
        private readonly Dictionary<ScoreType, Dictionary<int, int>> _score = new Dictionary<ScoreType, Dictionary<int, int>>();
        
        public List<Faction> FactionInformation { private get; set; }
        
        public void AddScore(ScoreType scoreType, int factionIndex, int addCount = 1)
        {
            Dictionary<int, int> scoresForType = _score.GetValueOrDefault(scoreType, new Dictionary<int, int>());
            int factionScore = scoresForType.GetValueOrDefault(factionIndex, 0);

            factionScore += addCount;
            scoresForType[factionIndex] = factionScore;
            _score[scoreType] = scoresForType.OrderBy(kvp => kvp.Key).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public void Print()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Final scores:");
            
            foreach (KeyValuePair<ScoreType, Dictionary<int, int>> pair in _score)
            {
                foreach ((int factionIndex, int score) in pair.Value)
                {
                    string factionPrefabName = this.FactionInformation[factionIndex].Name;
                    string scoreVerb = ScoreKeeper.GetScoreTypeVerb(pair.Key);

                    // => Faction 1 died 10 times
                    stringBuilder.AppendLine($"Faction {factionIndex} ({factionPrefabName}) has {scoreVerb} {score} times.");
                }
                stringBuilder.AppendLine("---");
            }
            
            Debug.Log(stringBuilder.ToString());
        }

        private static string GetScoreTypeVerb(ScoreType type)
        {
            return type switch {
                ScoreType.Win => "won",
                ScoreType.Lose => "lost",
                ScoreType.Draw => "tied",
                ScoreType.Death => "died",
                ScoreType.Kill => "killed",
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
