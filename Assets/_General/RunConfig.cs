using System.Collections.Generic;
using ArtificialEnemy;
using UnityEngine;

namespace _General
{
    [CreateAssetMenu(fileName = "RunConfig", menuName = "Configs/RunConfig", order = 1)]
    public class RunConfig : ScriptableObject
    {
        [Header("Simulation Settings")]
        [SerializeField] public int GamesCount = 1;
        [SerializeField] public bool RandomizePlayerOrder = true;
        [SerializeField] public float RoundDelay = 1f;
        [SerializeField] public int MaxRounds = 10;
        [SerializeField] public bool IsTestRun = false;
        
        [Header("Not Used Settings")]
        [SerializeField] public int MaxRetries = 0;
        [SerializeField][Range(0,1)] public float RoundRewardMultiplier = 0.075f;
        [SerializeField] public float MinRewardPercentage = 0.4f;
        
        [Header("Game Settings")]
        [SerializeField] public GameObject GamePrefab;
        [SerializeField] public int TestMapIndex = -1;
        [SerializeField] public List<Faction> Factions;
        //[SerializeField] public int AgentsPerFaction = 1;
        [SerializeField] public List<string> AgentNames = new List<string> {
            "Isabel",
            "Thorsten",
            "Elara",
            "Gareth",
            "Lyanna",
            "Aldric",
            "Seraphina",
            "Kieran",
            "Morgana",
            "Cedric",
            "Aria",
            "Thane",
            "Vivienne",
            "Roderick",
            "Isadora",
            "Magnus",
            "Evangeline",
            "Tristan",
            "Cordelia",
            "Valerian",
        };
    }

    [System.Serializable]
    public class Faction
    {
        [SerializeField] public string Name;
        [SerializeField] public List<GameObject> TeamSetup = new List<GameObject>();
        //public GameObject AgentPrefab;
    }
}
