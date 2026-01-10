using System.Collections.Generic;

namespace _General
{
    public struct GameState
    {
        /* Board is Index like this:
         * | 0 | 1 | 2 |
         * | 3 | 4 | 5 |
         * | 6 | 7 | 8 |
         * -----------------------
         * State looks like this (-1 = empty, 0 = playerA, 1 = playerB):
         * |  1 | -1 |  1 |
         * | -1 |  0 | -1 |
         * | -1 | -1 |  0 |
         * -----------------------
         * Possible actions for playerA:
         * [1,3,5,6,7]
         */

        public List<int> State;
        public List<int> PossibleActions;
    }
}
