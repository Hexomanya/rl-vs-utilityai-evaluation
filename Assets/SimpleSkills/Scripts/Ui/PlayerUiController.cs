using System;
using _General;
using KBCore.Refs;
using TMPro;
using UnityEngine;

namespace SimpleSkills.Scripts.Ui
{
    public class PlayerUiController : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] [Self] private PlayerInteractionManager _playerInteractionManager;
        
        [Header("Displays")]
        [SerializeField] private TextMeshProUGUI _nameDisplay;
        [SerializeField] private TextMeshProUGUI _statusTextDisplay;
        [SerializeField] private TextMeshProUGUI _skillDisplay;
        
        [Header("Events")]
        [SerializeField] private SkAgentEvent _currentAgentChangedEvent;
        
        private void OnEnable()
        {
            _currentAgentChangedEvent.Subscribe(this.OnPlayerChange);
        }

        private void OnDisable()
        {
            _currentAgentChangedEvent.Unsubscribe(this.OnPlayerChange);
        }

        private void OnPlayerChange(ISkAgent currentAgent)
        {
            if(StateManager.IsUiUpdateDisabled) return;
            
            if(currentAgent is not null)
            {
                this.UpdateDisplays(currentAgent);

                _playerInteractionManager.enabled = currentAgent is PlayerAgent;
            } 
            else
            {
                this.DisableDisplays();
                _playerInteractionManager.enabled = false;
            }
        }

        private void UpdateDisplays(ISkAgent agent)
        {
            _nameDisplay.enabled = true;
            _statusTextDisplay.enabled = true;
            _skillDisplay.enabled = true;

            _nameDisplay.text = agent.GetName();
            _statusTextDisplay.text = $"AP:{agent.ActionPoints} | HP:{agent.Health.Value}";

            string skillText = "";
            for (int i = 0; i < agent.Skills.Count; i++)
            {
                SimpleSkill skill = agent.Skills[i];
                if(skill is null)
                {
                    Debug.LogError($"Skill on agent {agent.GetName()} with index {i} is null!");
                    return;
                }
                
                string colorCode = agent.ActionPoints < skill.ActionPointCost ? "606060" : "02f750";
                skillText += $"<color=#{colorCode}>{i}: {skill.Name}</color>\n";
            }

            _skillDisplay.text = skillText;
        }

        private void DisableDisplays()
        {
            _nameDisplay.enabled = false;
            _statusTextDisplay.enabled = false;
            _skillDisplay.enabled = false;
        }
    }
}
