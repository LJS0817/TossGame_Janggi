using System;
using System.Collections.Generic;

namespace Janggi.Core
{
    /// <summary>
    /// 플레이어(또는 AI)의 자원(코스트), 손패(4장), 턴 행동 상태를 관리합니다.
    /// </summary>
    public class PlayerState
    {
        public const int MaxCost = 10;
        public const int MaxFieldCost = 20; // 33 코스트의 약 60% 필드 전력 상한
        public const int HandSize = 4;
        public const int TurnCostGain = 2;
        public const int DiscardCost = 1;
        public const int InitialCost = 0;

        // 코스트 역비례 밸런스형 가중치 (총합 100)
        // 졸: 35%, 마: 25%, 상: 20%, 포: 12%, 차: 8%
        private static readonly (PieceType type, int weight)[] SummonablePieceWeights = new (PieceType, int)[]
        {
            (PieceType.Pawn,     35),
            (PieceType.Horse,    25),
            (PieceType.Elephant, 20),
            (PieceType.Cannon,   12),
            (PieceType.Chariot,   8)
        };

        private readonly Random _random = new Random();

        public PlayerSide Side { get; private set; }
        public int CurrentCost { get; private set; }
        public List<PieceType> Hand { get; private set; }
        public bool HasSummonedThisTurn { get; set; }

        public PlayerState(PlayerSide side)
        {
            Side = side;
            Hand = new List<PieceType>(HandSize);
            Initialize();
        }

        /// <summary>
        /// 초기 상태(시작 코스트 2, 손패 4장 드로우)로 설정합니다.
        /// </summary>
        public void Initialize()
        {
            CurrentCost = InitialCost;
            HasSummonedThisTurn = false;
            Hand.Clear();
            for (int i = 0; i < HandSize; i++)
            {
                Hand.Add(GetRandomSummonablePiece());
            }
        }

        /// <summary>
        /// 턴 시작 시 코스트를 회복(+2, 최대 10)하고 턴 상태를 초기화합니다.
        /// </summary>
        public void StartTurn()
        {
            AddCost(TurnCostGain);
            HasSummonedThisTurn = false;
        }

        /// <summary>
        /// 코스트를 추가합니다 (최대 10 상한).
        /// </summary>
        public void AddCost(int amount)
        {
            CurrentCost = Math.Min(MaxCost, CurrentCost + amount);
        }

        /// <summary>
        /// 코스트를 차감합니다.
        /// </summary>
        public bool SpendCost(int amount)
        {
            if (CurrentCost < amount) return false;
            CurrentCost -= amount;
            return true;
        }

        /// <summary>
        /// 해당 기물을 소환할 수 있는 자원과 턴 조건(턴당 1회)이 되는지 확인합니다.
        /// </summary>
        public bool CanSummon(PieceType type)
        {
            if (HasSummonedThisTurn) return false;
            return CurrentCost >= type.GetCost();
        }

        /// <summary>
        /// 보유 코스트 및 필드 총 코스트 상한(20)을 고려하여 소환 가능한지 확인합니다.
        /// </summary>
        public bool CanSummon(Board board, PieceType type)
        {
            if (!CanSummon(type)) return false;
            if (board != null && board.GetTotalPieceCost(Side) + type.GetCost() > MaxFieldCost)
                return false;
            return true;
        }

        /// <summary>
        /// 손패의 카드를 사용하여 소환을 완료하고 새 카드를 즉시 보충합니다.
        /// </summary>
        public bool ConsumeCardForSummon(int handIndex)
        {
            if (handIndex < 0 || handIndex >= Hand.Count) return false;

            var pieceType = Hand[handIndex];
            int cost = pieceType.GetCost();

            if (!CanSummon(pieceType)) return false;

            SpendCost(cost);
            HasSummonedThisTurn = true;

            // 즉시 새 카드로 교체 보충
            Hand[handIndex] = GetRandomSummonablePiece();
            return true;
        }

        /// <summary>
        /// 1 코스트를 지불하여 손패 1장을 버리고 즉시 새 카드를 뽑습니다 (턴당 제한 없음).
        /// </summary>
        public bool DiscardCard(int handIndex)
        {
            if (handIndex < 0 || handIndex >= Hand.Count) return false;
            if (CurrentCost < DiscardCost) return false;

            SpendCost(DiscardCost);
            Hand[handIndex] = GetRandomSummonablePiece();
            return true;
        }

        /// <summary>
        /// 코스트 역비례 가중치(졸 35%, 마 25%, 상 20%, 포 12%, 차 8%)에 따라 새 기물을 무작위로 추첨합니다.
        /// </summary>
        private PieceType GetRandomSummonablePiece()
        {
            int roll = _random.Next(100); // 0 ~ 99
            int cumulative = 0;

            foreach (var item in SummonablePieceWeights)
            {
                cumulative += item.weight;
                if (roll < cumulative)
                {
                    return item.type;
                }
            }

            return PieceType.Pawn;
        }
    }
}
