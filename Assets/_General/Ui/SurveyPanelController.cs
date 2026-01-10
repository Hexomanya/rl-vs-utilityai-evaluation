using System;
using _General.Events;
using TMPro;
using UnityEngine;

namespace _General.Ui
{
    public class SurveyPanelController : MonoBehaviour
    {
        [Header("Ui Configuration")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private GameObject _replayPanel;
        [SerializeField] private GameObject _nextPanel;
        [SerializeField] private TextMeshProUGUI _replayText;
        [SerializeField] private TextMeshProUGUI _nextText;
        
        [Header("Events")]
        [SerializeField] private SurveyEntryEvent _nextSurveyConfigEvent;
        [SerializeField] private SurveyCommandEvent _commandEvent;

        private SurveyConfigEntry _currentSurveyEntry;
        private SurveyConfigEntry _nextSurveyEntry;
        
        private void OnEnable()
        {
            _nextSurveyConfigEvent.Subscribe(this.OnNextSurveyTriggered);
        }

        private void OnDisable()
        {
            _nextSurveyConfigEvent.Unsubscribe(this.OnNextSurveyTriggered);
        }

        private void OnNextSurveyTriggered(SurveyConfigEntry nextSurvey)
        {
            _nextSurveyEntry = (nextSurvey == _currentSurveyEntry) ? null : nextSurvey; //if they are the same we need replay
            
            bool hasCurrentSurvey = _currentSurveyEntry != null;
            bool hasNextSurvey = _nextSurveyEntry != null;

            _replayPanel.SetActive(hasCurrentSurvey);
            _nextPanel.SetActive(hasNextSurvey);

            if(hasCurrentSurvey) this.UpdateText(_replayText, _currentSurveyEntry);
            if(hasNextSurvey) this.UpdateText(_nextText, _nextSurveyEntry);

            if(hasCurrentSurvey || hasNextSurvey)
            {
                this.SetHidden(false);
            }
            else
            {
                Debug.LogError("Both _currentSurveyEntry and _nextSurveyEntry are null. This should not happen!");
            }
        }

        private void UpdateText(TextMeshProUGUI textObject, SurveyConfigEntry configEntry)
        {
            string text = $"Next Survey\nCodename: {configEntry.ObfuscatedName}";
            textObject.text = text;
        }

        public void SetHidden(bool isHidden)
        {
            _panelRoot.SetActive(!isHidden);
        }
        
        public void OnReplayCurrent()
        {
            _nextSurveyEntry = null;
            _commandEvent.Raise(SurveyCommand.Replay);
            this.SetHidden(true);
        }

        public void OnPlayNext()
        {
            if(_nextSurveyEntry == null)
            {
                Debug.LogWarning("OnPlayNext was called, but there is no next Survey!");
            }
            
            _currentSurveyEntry = _nextSurveyEntry;
            _commandEvent.Raise(SurveyCommand.PlayNext);
            this.SetHidden(true);
        }
    }
}
