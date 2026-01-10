using System.Collections.Generic;
using System.Linq;

namespace TicTacToe.Scripts
{
    public static class TicTacToeUtils
    {
        private static readonly int[][] WinningCombinations = {
            new[] { 0, 1, 2 }, // Top row
            new[] { 3, 4, 5 }, // Middle row
            new[] { 6, 7, 8 }, // Bottom row
            new[] { 0, 3, 6 }, // Left column
            new[] { 1, 4, 7 }, // Middle column
            new[] { 2, 5, 8 }, // Right column
            new[] { 0, 4, 8 }, // Main diagonal
            new[] { 2, 4, 6 }, // Anti-diagonal
        };

        public static int CheckWinner(List<int> board)
        {
            foreach (int[] combination in WinningCombinations)
            {
                int a = board[combination[0]];
                int b = board[combination[1]];
                int c = board[combination[2]];

                if(a != -1 && a == b && b == c)
                {
                    return a; // Return the winning player (0 or 1)
                }
            }

            return -1; // No winner yet
        }

        public static bool AreThereFreeTiles(List<int> board)
        {
            return board.Any(num => num == -1);
        }
    }
}
