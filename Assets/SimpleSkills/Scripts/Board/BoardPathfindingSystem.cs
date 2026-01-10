using System.Collections.Generic;
using KBCore.Refs;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace SimpleSkills
{
    
    
    [RequireComponent(typeof(SkBoardManager))]
    public class BoardPathfindingSystem : MonoBehaviour
    {
        [SerializeField, Self] private SkBoardManager _boardManager;
        
        private JobHandle _aStarHandle;
        private NativeList<int2> _resultPath;
        private NativeReference<bool> _pathFound;
        
        // public NativeReference<int> _iterationCount;
        // public NativeReference<int> _debugMessage;
        
        public readonly WalkabilityMap WalkabilityMap = new WalkabilityMap();

        private bool _jobLock;
        private readonly List<PathfindingRequest> _requestQueue = new List<PathfindingRequest>();
        private PathfindingRequest _currentRequest;
        
        private void Update()
        {
            if(!_aStarHandle.IsCompleted || _jobLock) return;
            if(_currentRequest is null || _currentRequest.IsDone) return;
            
            this.OnAStarFinished();
        }
        
        private void StartAStartPathfinder(PathfindingRequest request)
        {
            if(_currentRequest != null)
            {
                Debug.LogWarning("Tried to start pathfinder when he was not done. This should go into the queue!");
                return;
            }
            
            _jobLock = true;
            _currentRequest = request;
            
            if(!_aStarHandle.IsCompleted || !_boardManager.IsInBounds(_currentRequest.OriginPosition) || !_boardManager.IsInBounds(_currentRequest.TargetPosition))
            {
                Debug.LogError("Previous job not completed!");
                _currentRequest = null;
                _jobLock = false;
                return;
            }
            
            this.WalkabilityMap.ReadyUse(_boardManager.Tiles);
            
            // Map has been disposed already
            if(!this.WalkabilityMap.Value.IsCreated)
            {
                Debug.LogError("Walkability map not created after ReadyUse!");
                return;
            }
            
            //Debug.Log("Allocated");
            _resultPath = new NativeList<int2>(Allocator.TempJob);
            _pathFound = new NativeReference<bool>(false, Allocator.TempJob);
            
            // _iterationCount = new NativeReference<int>(0, Allocator.TempJob);
            // _debugMessage = new NativeReference<int>(0, Allocator.TempJob);
            //
            // _debugMessage.Value = -1;

            //WalkabilityMap.PrintWalkabilityMap(_boardManager.Width, _boardManager.Height);
            
            AStarPathFinder job = new AStarPathFinder
            {
                startPosition = new int2(_currentRequest.OriginPosition.x, _currentRequest.OriginPosition.y),
                targetPosition = new int2(_currentRequest.TargetPosition.x, _currentRequest.TargetPosition.y),
                boardSize = new int2(_boardManager.Width, _boardManager.Height),
                moveNextToTarget = _currentRequest.MoveNextToTarget,
                walkabilityMap = this.WalkabilityMap.Value,
                resultPath = _resultPath,
                pathFound = _pathFound,
            };
            
            _aStarHandle = job.Schedule();
            _jobLock = false;
        }

        private void OnAStarFinished()
        {
            _aStarHandle.Complete();
            
            if(_currentRequest is null)
            {
                Debug.LogWarning("There is not current request, but something called finished!");
                return;
            }

            if(_currentRequest.IsDone)
            {
                Debug.Log("Is already done!");
                return;
            }

            if(!_resultPath.IsCreated)
            {
                Debug.LogWarning("Tried to access deallocated path.");
                return;
            }
            
            // Debug.Log($"Iterations where: {_iterationCount.Value}");
            // Debug.Log($"Debug Message is: {_debugMessage.Value}");
            
            bool wasFound = _resultPath.Length > 0;
            _currentRequest.OnIsDone(_resultPath, wasFound, false);
            _currentRequest = null;
            
            _pathFound.Dispose();
            _resultPath.Dispose();

            // _debugMessage.Dispose();
            // _iterationCount.Dispose();
            
            if(_requestQueue.Count <= 0) return;
            PathfindingRequest request = _requestQueue[0];
            _requestQueue.RemoveAt(0);
            
            Debug.Log($"Starting next queued request. Queue size: {_requestQueue.Count}");
            this.StartAStartPathfinder(request);
        }
        
        public void QueuePathfindingRequest(PathfindingRequest request)
        {
            if(_currentRequest is null)
            {
                this.StartAStartPathfinder(request);
                return;
            }
            
            _requestQueue.Add(request);
            //Debug.Log($"Added request to pathfinding request (At position {_requestQueue.Count - 1})");
        }


        public void Clear()
        {
            if(_requestQueue.Count > 0)
            {
                Debug.Log($"Cleared {_requestQueue.Count} jobs from pathfinding queue.");
                _requestQueue.Clear();
            }
            
            this.ForceStop();
        }
        
        private void OnDestroy()
        {
            this.ForceStop();
            this.WalkabilityMap.Dispose();
        }

        private void ForceStop()
        {
            _aStarHandle.Complete();
            
            if(_currentRequest != null)
            {
                //Debug.LogWarning("Request was forced to stop!");
                _currentRequest.OnIsDone(_resultPath, false, true);
                _currentRequest = null;
            }
            
            if (_resultPath.IsCreated) _resultPath.Dispose();
            if (_pathFound.IsCreated) _pathFound.Dispose();
            // if (_debugMessage.IsCreated) _debugMessage.Dispose();
            // if (_iterationCount.IsCreated) _iterationCount.Dispose();
        }
    }
}
