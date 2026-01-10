using System.Collections.Generic;
using System.Linq;
using _General.Custom_Attributes;
using TicTacToe.Scripts;
using UnityEngine;
using Object = UnityEngine.Object;

public class TTTBoardManager : MonoBehaviour
{
    [SerializeField] [Required] private GameObject _tilePrefab;
    [SerializeField] private int _boardWidth = 3;
    [SerializeField] private int _boardHeight = 3;

    private readonly List<TTTTileValueManager> _tiles = new List<TTTTileValueManager>();

    private int TileCount { get => _boardWidth * _boardHeight; }

    public void Reset()
    {
        foreach (TTTTileValueManager tileValueManager in _tiles)
        {
            tileValueManager.Reset();
        }
    }

    public void Initialize()
    {
        if(_tiles.Count > 0)
        {
            Debug.LogWarning("The Board was already initialized! Use Reset instead!");
            this.Reset();
            return;
        }

        for (int i = 0; i < this.TileCount; i++)
        {
            GameObject tileObject = Object.Instantiate(_tilePrefab, gameObject.transform);

            if(!tileObject.TryGetComponent(out TTTTileValueManager valueManager))
            {
                Debug.LogError("The provided Tile-Prefab has no TileValueManager-Component on it!");
                return;
            }

            _tiles.Add(valueManager);
        }
    }

    public bool AssignTileToPlayer(int playerIndex, int tileIndex)
    {
        if(playerIndex is < 0 or >= 2)
        {
            Debug.LogError("PlayerIndex is illegal!");
            return false;
        }

        if(tileIndex < 0 || tileIndex >= _tiles.Count)
        {
            Debug.LogError("TileIndex is illegal!");
            return false;
        }

        if(_tiles[tileIndex].Value != TicTacToePlayer.None)
        {
            Debug.LogError("Tried illegal move! Tile was already claimed!");
            return false;
        }

        _tiles[tileIndex].AssignToPlayer(playerIndex);

        return true;
    }

    public List<int> GetGenericState()
    {
        return _tiles.Select(valueManager => (int)valueManager.Value).ToList();
    }
}
