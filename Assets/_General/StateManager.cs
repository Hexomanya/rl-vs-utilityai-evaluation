using System;
using System.Collections.Generic;
using _General.Events;
using _General.Ui;
using KBCore.Refs;
using Unity.MLAgents;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _General
{

    public class StateManager : ValidatedMonoBehaviour
    {
        public static bool IsUiUpdateDisabled = false;
        
        [Header("Survey Mode")]
        [SerializeField] private bool _useSurveyMode;
        [SerializeField] private SurveyConfig _surveyConfig;
        [SerializeField, Child] private SurveyPanelController _surveyPanelController;
        
        [Header("Survey Events")]
        [SerializeField] private SurveyEntryEvent _nextSurveyConfigEvent;
        [SerializeField] private SurveyCommandEvent _commandEvent;
        
        
        [Header("Dev Mode")]
        [SerializeField] private RunConfig _runConfig;

        private readonly ScoreKeeper _scoreKeeper = new ScoreKeeper();
        private readonly List<IGameplayManager> _gameplayManagers = new List<IGameplayManager>();

        private int _currentSurveyIndex;
        
        private void OnEnable()
        {
            Academy.Instance.AutomaticSteppingEnabled = true;
            
            if(!_useSurveyMode) return;
            _commandEvent.Subscribe(this.OnSurveyCommand);
        }
        
        private void OnDisable()
        {
            _scoreKeeper.Print();
            
            if(!_useSurveyMode) return;
            _commandEvent.Unsubscribe(this.OnSurveyCommand);
        }
        
        private void Awake()
        {
            _surveyPanelController.SetHidden(true);
            
            if(_runConfig is null)
            {
                Debug.LogError("No runConfig is set. Can not run!");
                return;
            }
            
            _scoreKeeper.FactionInformation = _runConfig.Factions;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        private void Start()
        {
            if(_useSurveyMode)
            {
                this.InitSurveyMode();
            }
            else
            {
                StateManager.IsUiUpdateDisabled = true;
                this.StartDevMode();
            }
        }

        private void InitSurveyMode()
        {
            if(_surveyConfig.ConfigEntries.Count <= 0)
            {
                Debug.LogError($"Invalid amount of survey entries: {_surveyConfig.ConfigEntries.Count}");
                return;
            }

            foreach (SurveyConfigEntry entry in _surveyConfig.ConfigEntries)
            {
                entry.PlayCount = 0;
            }
            
            _nextSurveyConfigEvent.Raise(_surveyConfig.ConfigEntries[0]);
        }
        
        private void OnSurveyCommand(SurveyCommand command)
        {
            switch (command)
            {
                case SurveyCommand.Replay:
                    _currentSurveyIndex -= 1;
                    this.StartSurveyMode();
                    break;

                case SurveyCommand.PlayNext:
                    this.StartSurveyMode();
                    break;

                case SurveyCommand.End:
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(command), command, null);
            }
        }

        private void StartSurveyMode()
        {
            foreach (IGameplayManager clearedManager in _gameplayManagers)
            {
                clearedManager.OnGameEnded -= this.GameplayManagerOnOnGameEnded;
                if(clearedManager is MonoBehaviour clearedObject) Object.DestroyImmediate(clearedObject.transform.parent.gameObject);
            }
            _gameplayManagers.Clear();
            
            //int width = (int)Mathf.Sqrt(_runConfig.GamesCount);

            RunConfig currentConfig = _surveyConfig.ConfigEntries[_currentSurveyIndex].RunConfig;
            if(currentConfig is null)
            {
                Debug.LogError("Could not retrieve run config!");
                return;
            }
            
            GameObject newRunObject = Object.Instantiate(currentConfig.GamePrefab, Vector3.zero, Quaternion.identity);
            IGameplayManager manager = newRunObject.GetComponentInChildren<IGameplayManager>();

            if(manager is null)
            {
                Debug.LogError("Could not find gameplay manager in object. Prefab must be setup wrong!");
                Object.DestroyImmediate(newRunObject);
                return;
            }

            // Vector2Int gridPos = OneDimUtil.GetPositionUnsafe(0, width);
            // Vector2 offset = manager.GetMultipleGameOffset();
            // Vector3 position = new Vector3(gridPos.x * offset.x, 0, gridPos.y * offset.y);
            // newRunObject.transform.position = position;
            
            _gameplayManagers.Add(manager);
            Debug.Log($"Created {_gameplayManagers.Count} managers!");
            
            Debug.Log("Starting up");
            foreach (IGameplayManager gameplayManager in _gameplayManagers)
            {
                gameplayManager.OnStart(_scoreKeeper, currentConfig);
                gameplayManager.OnGameEnded += this.GameplayManagerOnOnGameEnded;
            }
        }

        private void GameplayManagerOnOnGameEnded(string debugString)
        {
            SurveyConfigEntry currentEntry = _surveyConfig.ConfigEntries[_currentSurveyIndex];
            currentEntry.PlayCount++;

            if(currentEntry.PlayCount >= currentEntry.MinReplayCount)
            {
                _currentSurveyIndex++;
            }
            
            _nextSurveyConfigEvent.Raise(_surveyConfig.ConfigEntries[_currentSurveyIndex]);
        }

        private void StartDevMode()
        {
            _gameplayManagers.Clear();

            int width = (int)Mathf.Sqrt(_runConfig.GamesCount);

            for (int i = 0; i < _runConfig.GamesCount; i++)
            {
                GameObject newRunObject = Object.Instantiate(_runConfig.GamePrefab, Vector3.zero, Quaternion.identity);
                IGameplayManager manager = newRunObject.GetComponentInChildren<IGameplayManager>();

                if(manager is null)
                {
                    Debug.LogError("Could not find gameplay manager in object. Prefab must be setup wrong!");
                    Object.DestroyImmediate(newRunObject);
                    return;
                }

                Vector2Int gridPos = OneDimUtil.GetPositionUnsafe(i, width);
                Vector2 offset = manager.GetMultipleGameOffset();
                Vector3 position = new Vector3(gridPos.x * offset.x, 0, gridPos.y * offset.y);
                newRunObject.transform.position = position;
                
                _gameplayManagers.Add(manager);
            }

            Debug.Log($"Created {_gameplayManagers.Count} managers!");
            
            Debug.Log("Starting up");
            foreach (IGameplayManager gameplayManager in _gameplayManagers)
            {
                gameplayManager.OnStart(_scoreKeeper, _runConfig);
            }
        }
    }
}
