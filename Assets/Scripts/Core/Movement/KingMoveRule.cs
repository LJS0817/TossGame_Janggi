using System.Collections.Generic;

namespace Janggi.Core.Movement
{
    /// <summary>
    /// 궁(왕)의 행마법.
    /// 궁성(3×3) 내에서만 이동 가능: 상하좌우 1칸 + 궁성 대각선.
    /// </summary>
    public class KingMoveRule : IMoveRule
    {
        // 상하좌우 이동 방향
        private static readonly int[,] OrthogonalDirs = {
            { 0, 1 }, { 0, -1 }, { 1, 0 }, { -1, 0 }
        };

        // 대각선 이동 방향
        private static readonly int[,] DiagonalDirs = {
            { 1, 1 }, { 1, -1 }, { -1, 1 }, { -1, -1 }
        };

        public List<BoardPosition> GetValidMoves(Board board, Piece piece)
        {
            var moves = new List<BoardPosition>();
            var from = piece.Position;
            var side = piece.Side;

            // 상하좌우 1칸 — 궁성 내에서만
            for (int i = 0; i < OrthogonalDirs.GetLength(0); i++)
            {
                var to = from.Offset(OrthogonalDirs[i, 0], OrthogonalDirs[i, 1]);
                if (to.IsValid() && to.IsInsidePalace(side))
                {
                    var target = board.GetPieceAt(to);
                    if (target == null || target.Side != side)
                        moves.Add(to);
                }
            }

            // 대각선 1칸 — 궁성 대각선 경로에서만
            for (int i = 0; i < DiagonalDirs.GetLength(0); i++)
            {
                var to = from.Offset(DiagonalDirs[i, 0], DiagonalDirs[i, 1]);
                if (to.IsValid() && Board.IsPalaceDiagonalMove(from, to, side))
                {
                    var target = board.GetPieceAt(to);
                    if (target == null || target.Side != side)
                        moves.Add(to);
                }
            }

            return moves;
        }
    }
}
