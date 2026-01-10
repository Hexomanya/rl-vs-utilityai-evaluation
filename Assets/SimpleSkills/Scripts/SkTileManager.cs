using System;
using KBCore.Refs;
using SimpleSkills.Scripts.Ui;
using UnityEngine;


namespace SimpleSkills
{
    [RequireComponent(typeof(TileUiController))]
    public class SkTileManager : ValidatedMonoBehaviour
    {
        [SerializeField, Self] private TileUiController _tileUiController;
        
        private ITileContainable _containedEntity;
        
        public ITileContainable ContainedEntity { get => _containedEntity; set => this.SetContainedEntity(value); }
        public TileUiController UiController { get => _tileUiController; }
        
        public Vector2Int DebugBoardPosition { get; set; }

        private void Start()
        {
            _tileUiController.UpdateVisuals();
        }

        public bool IsFree()
        {
            return _containedEntity == null;
        }

        public bool IsWalkable()
        {
            return !this.ContainsObstruction() || this.IsFree(); //TODO: IsFree is redundant?
        }
        
        public void Clear()
        {
            this.ContainedEntity = null; //Updates UI
        }
        
        private void SetContainedEntity(ITileContainable containable)
        {
            if(containable == _containedEntity)
            {
                if(containable == null) return;
                Debug.LogWarning("Containable was already in tile. Skipping...");
                return;
            }

            if(containable != null && _containedEntity != null)
            {
                Debug.LogError("Tried to set a tile that already contained something else! This should be checked sooner!");
                return;
            }
            
            _containedEntity = containable;
            _tileUiController.UpdateVisuals();
        }
        
        public bool ContainsObstruction()
        {
            return _containedEntity is not null && _containedEntity.IsObstruction;
        }
        
        public bool TryGetAgent(out ISkAgent agent)
        {
            agent = _containedEntity as ISkAgent;
            return agent is not null;
        }

        public bool ContainsResourceOfType(Type elementType)
        {
            return _containedEntity is Resource resource && resource.ContainedElement.GetType() == elementType;
        }
    }
}
