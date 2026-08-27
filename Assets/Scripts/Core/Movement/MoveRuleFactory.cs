using System.Collections.Generic;

namespace Janggi.Core.Movement
{
    /// <summary>
    /// PieceType에 따라 적절한 IMoveRule 구현체를 반환하는 팩토리.
    /// </summary>
    public static class MoveRuleFactory
    {
        private static readonly Dictionary<PieceType, IMoveRule> _rules
            = new Dictionary<PieceType, IMoveRule>
        {
            { PieceType.King,     new KingMoveRule() },
            { PieceType.Advisor,  new AdvisorMoveRule() },
            { PieceType.Elephant, new ElephantMoveRule() },
            { PieceType.Horse,    new HorseMoveRule() },
            { PieceType.Chariot,  new ChariotMoveRule() },
            { PieceType.Cannon,   new CannonMoveRule() },
            { PieceType.Pawn,     new PawnMoveRule() },
        };

        /// <summary>
        /// 지정된 기물 종류에 대한 이동 규칙을 반환합니다.
        /// </summary>
        public static IMoveRule GetMoveRule(PieceType type)
        {
            return _rules[type];
        }

        /// <summary>
        /// 지정된 기물의 이동 가능 위치 목록을 반환합니다 (편의 메서드).
        /// </summary>
        public static List<BoardPosition> GetValidMoves(Board board, Piece piece)
        {
            var rule = GetMoveRule(piece.Type);
            return rule.GetValidMoves(board, piece);
        }
    }
}
