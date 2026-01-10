using System;
using TicTacToe.Scripts;
using UnityEngine;

public class TTTTileValueManager : MonoBehaviour
{
    [SerializeField] private GameObject _circleIcon;
    [SerializeField] private GameObject _crossIcon;

    public TicTacToePlayer Value { get; private set; } = TicTacToePlayer.None;

    private void Awake()
    {
        if(_circleIcon is null || _crossIcon is null)
        {
            Debug.LogError("A TileValueManager is not setup correctly!");
        }

        _circleIcon?.SetActive(false);
        _crossIcon?.SetActive(false);
    }

    public void Reset()
    {
        this.Value = TicTacToePlayer.None;
        _circleIcon.SetActive(false);
        _crossIcon.SetActive(false);
    }

    public void AssignToPlayer(int playerIndex)
    {
        if(!Enum.IsDefined(typeof(TicTacToePlayer), playerIndex))
        {
            throw new ArgumentOutOfRangeException($"The player index does not allow for the number {playerIndex}.");
        }

        TicTacToePlayer player = (TicTacToePlayer)playerIndex;
        this.Value = player;

        switch (player)
        {
            case TicTacToePlayer.Circle:
                _circleIcon.SetActive(true);
                _crossIcon.SetActive(false);
                break;

            case TicTacToePlayer.Cross:
                _circleIcon.SetActive(false);
                _crossIcon.SetActive(true);
                break;

            case TicTacToePlayer.None:
                _circleIcon.SetActive(false);
                _crossIcon.SetActive(false);
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
