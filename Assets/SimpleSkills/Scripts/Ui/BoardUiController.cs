using System;
using _General;
using KBCore.Refs;
using UnityEngine;

namespace SimpleSkills.Scripts.Ui
{
    public class BoardUiController : ValidatedMonoBehaviour
    {
        
        [Header("Object Connections")]
        [SerializeField, Self] private SkBoardManager _boardManager;

        [Header("Events")]
        [SerializeField] private WorldPositionEvent _positionClickedEvent;
        [SerializeField] private SkAgentEvent _currentAgentChangedEvent;
        [SerializeField] private SkAgentEvent _agentTookActionEvent;
        [SerializeField] private SkillEvent _lastUsedSkillChangedEvent;
        
        // State
        private TileUiController _lastClickedController;
        private TileUiController _lastCurrentController;
        private ISkAgent _currentAgent;
        
        private void OnEnable()
        {
            _positionClickedEvent.Subscribe(this.OnPositionClicked);
            _currentAgentChangedEvent.Subscribe(this.OnCurrentAgentChanged);
            _agentTookActionEvent.Subscribe(this.OnAgentTookAction);
            _lastUsedSkillChangedEvent.Subscribe(this.OnLastUsedSkillChanged);
        }
        
        private void OnDisable()
        {
            _positionClickedEvent.Unsubscribe(this.OnPositionClicked);
            _currentAgentChangedEvent.Unsubscribe(this.OnCurrentAgentChanged);
            _agentTookActionEvent.Unsubscribe(this.OnAgentTookAction);
            _lastUsedSkillChangedEvent.Unsubscribe(this.OnLastUsedSkillChanged);
        }
        
        private void OnPositionClicked(Vector3 worldPosition)
        {
            if(StateManager.IsUiUpdateDisabled) return;
            Vector2Int boardPosition = _boardManager.WorldToBoardPosition(worldPosition);
            SkTileManager clickedTile = _boardManager.GetTileAt(boardPosition);
    
            BoardUiController.UpdateTileController(
                clickedTile?.UiController, 
                ref _lastClickedController, 
                controller => controller?.SetIsSelected(true),
                controller => controller?.SetIsSelected(false)
            );
        }

        private void OnCurrentAgentChanged(ISkAgent agent)
        {
            if(StateManager.IsUiUpdateDisabled) return;
            if(!StateManager.IsInSurveyMode) return;
            
            //Debug.Log($"CurrentAgent changed to {agent.Position}");
            _currentAgent = agent;
            SkTileManager currentTile = _boardManager.GetTileAt(agent.Position);
    
            BoardUiController.UpdateTileController(
                currentTile?.UiController, 
                ref _lastCurrentController, 
                controller => controller?.SetIsCurrent(true),
                controller => controller?.SetIsCurrent(false)
            );
        }

        private static void UpdateTileController(
            TileUiController newController, 
            ref TileUiController lastController,
            Action<TileUiController> activateAction,
            Action<TileUiController> deactivateAction)
        {
            if (newController is null || newController == lastController) return;

            deactivateAction?.Invoke(lastController);
            lastController = newController;
            activateAction(lastController);
        }
        
        private void OnLastUsedSkillChanged(SimpleSkill lastUsedSkill)
        {
            if(StateManager.IsUiUpdateDisabled) return;
            if(!StateManager.IsInSurveyMode) return;
            
            if(_currentAgent is null)
            {
                Debug.LogWarning("Current agent is null!");
                return;
            }
            
            SkTileManager currentTile = _boardManager.GetTileAt(_currentAgent.Position);
            currentTile?.UiController?.SetLastUsedSkill(lastUsedSkill);
        }

        private void OnAgentTookAction(ISkAgent activeAgent)
        {
            if(StateManager.IsUiUpdateDisabled) return;
            if(!StateManager.IsInSurveyMode) return;
            
            //THIS IS VERY MUCH NOT OPTIMAL!
            foreach (SkTileManager tile in _boardManager.Tiles)
            {
                if(tile.ContainedEntity is not ISkAgent agent) continue;
                
                SkTileManager currentTile = _boardManager.GetTileAt(agent.Position);
                currentTile?.UiController?.UpdateHealthbar();
            }
        }
    }
}
