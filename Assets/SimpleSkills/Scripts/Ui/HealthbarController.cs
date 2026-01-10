using System;
using _General;
using KBCore.Refs;
using TMPro;
using UnityEngine;

namespace SimpleSkills.Scripts.Ui
{
    public class HealthbarController : DisplayController
    {
        [SerializeField] private GameObject _rootContainer;
        [SerializeField] private TextMeshProUGUI _healthTextDisplay;
        [SerializeField] private RectTransform _healthBarRect;

        private float _startWidth = 256f;
        
        private void Start()
        {
            //_startWidth = _healthBarRect.rect.width;
        }

        public void OnHealthChange(Attribute<int> health)
        {
            float percentage = health.Value / (float)health.MaxValue;
            float newWidth = _startWidth * percentage;
            
            Debug.Log($"Updating healthbar. Startwidth: {_startWidth} Percantage: {percentage}; width: {newWidth}");
            
            _healthBarRect.sizeDelta = new Vector2(newWidth, _healthBarRect.sizeDelta.y);
        }

        public override void SetHidden(bool isHidden)
        {
            _rootContainer.SetActive(!isHidden);
        }
    }
}
