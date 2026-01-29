using System;
using System.Diagnostics;
using _General.Events;
using SimpleSkills.Scripts;
using TMPro;
using UnityEngine;
using Debug = UnityEngine.Debug;

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
        [SerializeField] private TextMeshProUGUI _resultText;

        [Header("Events")]
        [SerializeField] private GameResultEvent _gameResultEvent;
        [SerializeField] private SurveyEntryEvent _nextSurveyConfigEvent;
        [SerializeField] private SurveyCommandEvent _commandEvent;

        private GameResult _lastGameResult;
        private SurveyConfigEntry _currentSurveyEntry;
        private SurveyConfigEntry _nextSurveyEntry;
        
        private void OnEnable()
        {
            _nextSurveyConfigEvent.Subscribe(this.OnNextSurveyTriggered);
            _gameResultEvent.Subscribe(this.OnGameResult);
        }
        
        private void OnDisable()
        {
            _nextSurveyConfigEvent.Unsubscribe(this.OnNextSurveyTriggered);
            _gameResultEvent.Unsubscribe(this.OnGameResult);
        }

        private void OnNextSurveyTriggered(SurveyConfigEntry nextSurvey)
        {
            _nextSurveyEntry = (nextSurvey == _currentSurveyEntry) ? null : nextSurvey; //if they are the same we need replay
            
            bool hasCurrentSurvey = _currentSurveyEntry != null;
            bool hasNextSurvey = _nextSurveyEntry != null;
            
            Debug.Log($"hasCurrentSurvey: {hasCurrentSurvey}, hasNextSurvey: {hasNextSurvey}");

            _replayPanel.SetActive(hasCurrentSurvey);
            _nextPanel.SetActive(hasNextSurvey);

            if(hasCurrentSurvey) this.UpdateText(_replayText, _currentSurveyEntry);
            if(hasNextSurvey) this.UpdateText(_nextText, _nextSurveyEntry);

            this.UpdateResultText(hasCurrentSurvey);

            if(hasCurrentSurvey || hasNextSurvey)
            {
                this.SetHidden(false);
            }
            else
            {
                Debug.LogError("Both _currentSurveyEntry and _nextSurveyEntry are null. This should not happen!");
            }
        }

        private void UpdateResultText(bool hasCurrentSurvey)
        {
            if (!hasCurrentSurvey)
            {
                this.HideResultText();
                return;
            }

            _resultText.text = _lastGameResult switch
            {
                GameResult.Win => "You won!",
                GameResult.Loose => "You lost!",
                GameResult.Draw => "It is a draw!",
                _ => null,
            };

            if (string.IsNullOrEmpty(_resultText.text))
            {
                this.HideResultText();
            }
            else
            {
                _resultText.gameObject.SetActive(true);
            }
        }

        private void HideResultText()
        {
            _resultText.text = "";
            _resultText.gameObject.SetActive(false);
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
        
        private void OnGameResult(GameResult result)
        {
            _lastGameResult = result;
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
