using System;
using System.Collections.Generic;
using KBCore.Refs;
using SimpleSkills.Scripts.Ui;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SimpleSkills.Scripts
{
    public class PlayerInteractionManager : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] [Scene] private Camera _camera;
        [SerializeField] private LayerMask _tileLayerMask;
        
        [Header("Events")]
        [SerializeField] private WorldPositionEvent _tileClickedWorldPositionEvent;
        [SerializeField] private IntegerEvent _skillActionPerformed;
        
        private InputAction _selectTileAction;
        private List<InputAction> _selectSkillsActions = new List<InputAction>();
        
        private void Awake()
        {
            _selectTileAction = InputSystem.actions.FindAction("SelectTile", true);

            foreach (string actionName in ActionConfig.PlayerSkillActionNames)
            {
                _selectSkillsActions.Add(InputSystem.actions.FindAction(actionName, true));
            }
        }

        private void OnEnable()
        {
            _selectTileAction.performed += this.OnSelectTile;

            foreach (InputAction action in _selectSkillsActions) 
            {
                action.performed += this.OnSelectSkill;
            }
        }
        
        private void OnDisable()
        {
            _selectTileAction.performed -= this.OnSelectTile;
            
            foreach (InputAction action in _selectSkillsActions) 
            {
                action.performed -= this.OnSelectSkill;
            }
        }
        
        private void OnSelectTile(InputAction.CallbackContext context)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = _camera.ScreenPointToRay(mousePos);

            if (! Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _tileLayerMask)) return;
            if(!hit.collider.gameObject.TryGetComponent(out SkTileManager skTileManager)) return;
            
            _tileClickedWorldPositionEvent.Raise(hit.point);
        }
        
        private void OnSelectSkill(InputAction.CallbackContext obj)
        {
            Debug.Log($"Button with action {obj.action.name} performed");
            int index = _selectSkillsActions.FindIndex(action => action.name == obj.action.name);
            if(index == -1) return;
            
            Debug.Log("Raising Event");
            _skillActionPerformed.Raise(index);
        }
    }
}
