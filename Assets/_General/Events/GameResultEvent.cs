using _General;
using UnityEngine;

namespace SimpleSkills.Scripts
{
    [CreateAssetMenu(fileName = "GameResultEvent", menuName = "Events/GameResultEvent")]
    public class GameResultEvent : ScriptableEvent<GameResult> {}
}
