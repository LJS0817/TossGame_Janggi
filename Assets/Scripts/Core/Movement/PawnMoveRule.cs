using System.Collections.Generic;

namespace Janggi.Core.Movement
{
    /// <summary>
    /// 졸/병(卒/兵)의 행마법.
    /// 전방 1칸 + 좌우 1칸 이동 (후퇴 불가).
    /// 궁성 내에서는 대각선 이동 가능.
    /// 
    /// 초(Cho)는 Row가 증가하는 방향이 전진.
    /// 한(Han)은 Row가 감소하는 방향이 전진.
    /// </summary>
    public class PawnMoveRule : IMoveRule
    {
        public List<BoardPosition> GetValidMoves(Board board, Piece piece)
        {
            var moves = new List<BoardPosition>();
            var from = piece.Position;
            var side = piece.Side;

            // 전진 방향 결정
            int forward = (side == PlayerSide.Cho) ? 1 : -1;

            // 전방 1칸
            TryAddMove(board, piece, from.Offset(0, forward), moves);

            // 좌우 1칸
            TryAddMove(board, piece, from.Offset(-1, 0), moves);
            TryAddMove(board, piece, from.Offset(1, 0), moves);

            // 궁성 내 대각선 이동
            if (from.IsInsideAnyPalace())
            {
                PlayerSide palaceSide = from.IsInsidePalace(PlayerSide.Cho)
                    ? PlayerSide.Cho
                    : PlayerSide.Han;

                // 전방 대각선만 허용 (후퇴 대각선은 불가)
                var diagLeft = from.Offset(-1, forward);
                var diagRight = from.Offset(1, forward);

                if (diagLeft.IsValid() && Board.IsPalaceDiagonalMove(from, diagLeft, palaceSide))
                    TryAddMove(board, piece, diagLeft, moves);

                if (diagRight.IsValid() && Board.IsPalaceDiagonalMove(from, diagRight, palaceSide))
                    TryAddMove(board, piece, diagRight, moves);
            }

            return moves;
        }

        private void TryAddMove(Board board, Piece piece, BoardPosition to, List<BoardPosition> moves)
        {
            if (!to.IsValid()) return;

            var target = board.GetPieceAt(to);
            if (target == null || target.Side != piece.Side)
                moves.Add(to);
        }
    }
}
