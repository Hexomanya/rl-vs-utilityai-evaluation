using Unity.MLAgents.Sensors;
using UnityEngine;

namespace SimpleSkills
{
    public abstract class GridObservator : ISensor
    {
        private readonly string _name;
        protected readonly int _channelCount;
        protected readonly int _height;
        protected readonly int _width;
        protected float[,,] _gridData;

        protected GridObservator(string name, int channelCount, int height, int width) {
            _name = name;
            _channelCount = channelCount;
            _height = height;
            _width = width;

            _gridData = new float[_channelCount, _height, _width];
        }

        protected abstract ObservationSpec CreateObservationSpec();
        
        public void Update() { }
        
        public ObservationSpec GetObservationSpec() { return this.CreateObservationSpec(); }
        
        public byte[] GetCompressedObservation() { return null; }
        
        public CompressionSpec GetCompressionSpec() { return CompressionSpec.Default(); }

        public string GetName() { return _name; }
        
        public int Write(ObservationWriter writer)
        {
            int index = 0;

            for (int c = 0; c < _channelCount; c++)
            {
                for (int y = 0; y < _height; y++)
                {
                    for (int x = 0; x < _width; x++)
                    {
                        writer[index] = _gridData[c, y, x];
                        index++;
                    }
                }
            }
            return index;
        }
        
        public void Reset()
        {
            _gridData = new float[_channelCount, _height, _width];
        }
        
        public void SetGrids(float[,,] grids)
        {
            if(!this.DoSizesMatch(grids))
            {
                return;
            }
            _gridData = grids;
        }

        public void SetGrid(int channelIndex, float[,] grid)
        {
            if(channelIndex >= _channelCount)
            {
                Debug.LogError("Channel Index was out of bounds.");
                return;
            }

            if(grid.GetLength(0) != _height || grid.GetLength(1) != _width)
            {
                Debug.LogError("Grid Dimensions did not match!");
                return;
            }

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    _gridData[channelIndex, y, x] = grid[y, x];
                }
            }
        }

        private bool DoSizesMatch(float[,,] inputGrid)
        {
            bool doMatch = true;

            if(inputGrid.GetLength(0) != _channelCount)
            {
                doMatch = false;
                Debug.LogError("Channel-Count did not match on observation Grid!");
            }

            if(inputGrid.GetLength(1) != _height)
            {
                doMatch = false;
                Debug.LogError("Height did not match on observation Grid!");
            }

            if(inputGrid.GetLength(2) != _width)
            {
                doMatch = false;
                Debug.LogError("Width did not match on observation Grid!");
            }

            return doMatch;
        }
    }
}
