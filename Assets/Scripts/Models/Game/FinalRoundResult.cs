using System;

namespace MathHighLow.Models
{
    [Serializable]
    public sealed class FinalRoundResult
    {
        public BlindExpression PlayerExpression;
        public BlindExpression AIExpression;
        public int SharedNumber;
        public float PlayerValue;
        public float AIValue;
        public float PlayerDistance;
        public float AIDistance;
        public int PlayerClosestTarget;
        public int AIClosestTarget;
        public bool PlayerValid;
        public bool AIValid;
        public string Winner;
        public long PlayerBet;
        public long AIBet;
        public long TotalPot;
        // 이전 UI·저장 호환용이며 TotalPot과 같은 값이다.
        public long Bet;
        public bool WasAllIn;
        public bool AIWasAllIn;
        public bool WasFolded;
        public bool IsFinalShowdown;
    }
}
