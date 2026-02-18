using System;
using _General;
using KBCore.Refs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace SimpleSkills.Scripts.Ui
{
    [RequireComponent(typeof(SkTileManager))]
    public class TileUiController : ValidatedMonoBehaviour
    {
        [SerializeField, Self] private SkTileManager _tileManager;

        [Header("Canvas")]
        [SerializeField, Child] private Canvas _canvas;
        
        [Header("Sprites")]
        [SerializeField] private Sprite _defaultSprite;
        [SerializeField] private Sprite _playerSprite;
        [SerializeField] private Sprite _elementSprite;

        [Header("Tile Displays")]
        [SerializeField] private GameObject _tileDisplayRoot;
        [SerializeField] private Image _tileBackground;
        [SerializeField] private Image _figureDisplay;
        [SerializeField] private TextMeshProUGUI _positionText;
        [SerializeField] private Image _highlightBackground;

        [Header("Moveable Displays")]
        [SerializeField] private GameObject _statusDisplayRoot;
        [SerializeField] private HealthbarController _healthbarController;
        [SerializeField] private SkillDisplayController _skillDisplayController;
        
        [Header("Colors")]
        [SerializeField] private Color _defaultColor = Color.white;
        [SerializeField] private Color _activeHighlightColor = Color.yellow;
        [SerializeField] private Color _selectedColor = Color.blue;

        private bool _isCanvasDestroyed;
        private bool _isSelected;
        private bool _isCurrent;

        private ITileContainable ContainedEntity { get => _tileManager?.ContainedEntity; }

        private void Start()
        {
            if(StateManager.IsUiUpdateDisabled)
            {
                GameObject.DestroyImmediate(_canvas.gameObject);
                _isCanvasDestroyed = true;
            }
            this.UpdateVisuals();
        }

        public void UpdateVisuals()
        {
            if(_isCanvasDestroyed) return;
            if(StateManager.IsUiUpdateDisabled)
            {
                _statusDisplayRoot.SetActive(false);
                _tileDisplayRoot.SetActive(false);
                return;
            }
            
            if(_figureDisplay is null) return;
            if(_tileBackground?.sprite is null) return;
            
            //TODO: Pack into the classes like color
            _figureDisplay.sprite = this.ContainedEntity switch {
                Resource => _elementSprite,
                ISkAgent => _playerSprite,
                _ => null,
            };

            _figureDisplay.enabled = _figureDisplay.sprite is not null;
            _figureDisplay.color = this.GetFigureDisplayColor();
            _tileBackground.color = this.GetTileBackgroundColor();

            bool isAgent = this.ContainedEntity is ISkAgent;
            _healthbarController.SetHidden(!isAgent);
            _skillDisplayController.SetHidden(!isAgent);
            
            _highlightBackground.enabled = _isCurrent;
            _highlightBackground.color = _isCurrent ? _activeHighlightColor : _defaultColor;
        }
        
        public void SetPositionText(string text)
        {
            if(_positionText is null) return;
            _positionText.text = text;
        }
        
        public void SetIsSelected(bool isSelected)
        {
            _isSelected = isSelected;
            this.UpdateVisuals();
        }

        public void SetIsCurrent(bool isCurrent)
        {
            _isCurrent = isCurrent;
            this.UpdateVisuals();
        }

        private Color GetTileBackgroundColor()
        {
            if(_isSelected) return _selectedColor;
            return this.ContainedEntity?.TileColor ?? _defaultColor;
        }
        
        private Color GetFigureDisplayColor()
        {
            if(_isSelected) return this.ContainedEntity?.TileColor ?? _defaultColor;
            return _defaultColor;
        }
        public void SetLastUsedSkill(SimpleSkill lastUsedSkill)
        {
            _skillDisplayController.UpdateSkill(lastUsedSkill);
        }

        public void UpdateHealthbar()
        {
            if(_isCanvasDestroyed) return;
            if(_tileManager.ContainedEntity is not ISkAgent agent) return;
            _healthbarController.OnHealthChange(agent.Health);
        }
    }
}
