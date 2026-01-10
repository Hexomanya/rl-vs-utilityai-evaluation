using System;
using _General;
using UnityEngine;

namespace SimpleSkills
{
    [Serializable]
    public class ValueMap
    {
        // We want to option to use the values as a DictionaryKey, so we multiply then round it to do that safely
        public const int VALUE_MULTIPLIER = 1000;
        
        private readonly Bounds2Int _bounds;
        private readonly Bounds2Int _parentBounds;
        public int?[] Values;

        public ValueMap(Bounds2Int bounds, Bounds2Int parentBounds)
        {
            _bounds = bounds;
            _parentBounds = parentBounds;
            
            this.Values = new int?[_bounds.Width * _bounds.Height];
        }

        public int GetPositionIndexWithParentBounds(Vector2Int position)
        {
            Vector2Int relativePos = position - this.GetUpperLeftCornerPosition();
            return OneDimUtil.GetIndex(relativePos.x, relativePos.y, _bounds.Width);
        }
        
        public Vector2Int IndexToBoardPosition(int index)
        {
            return this.GetUpperLeftCornerPosition() + OneDimUtil.GetPosition(index, this._bounds.Width, this._bounds.Height);
        }

        public int BoardPositionToIndex(Vector2Int boardPosition)
        {
            return OneDimUtil.GetIndex(boardPosition - this.GetUpperLeftCornerPosition(), _bounds.Width);
        }

        private Vector2Int GetUpperLeftCornerPosition()
        {
            return OneDimUtil.GetPosition(_bounds.StartIndex, _parentBounds.Width, _parentBounds.Height);
        }
        
        public void SetValue(Vector2Int currentPosition, int? value)
        {
            int index = this.BoardPositionToIndex(currentPosition);

            if(index < 0 || index >= this.Values.Length)
            {
                Debug.LogWarning("Index out of bounds!");
                return;
            }

            this.Values[index] = value;
        }
    }
}
