using System.Collections.Generic;

namespace Janggi.Core.Movement
{
    /// <summary>
    /// 마(馬)의 행마법.
    /// 직선 1칸 + 대각선 1칸 (총 2칸 이동).
    /// 첫 직선 칸에 기물이 있으면 이동 불가 (길막/멱).
    /// 
    /// 이동 패턴 (8방향):
    ///   직선1칸(상하좌우) → 대각선1칸 (벌어지는 방향 2가지)
    /// </summary>
    public class HorseMoveRule : IMoveRule
    {
        // { 직선dCol, 직선dRow, 대각dCol, 대각dRow }
        private static readonly int[][] MovePatterns = new int[][]
        {
            // 위로 직선 → 좌상/우상
            new[] { 0, 1, -1, 1 },  // (0,1)→(-1,2)
            new[] { 0, 1,  1, 1 },  // (0,1)→(1,2)
            // 아래로 직선 → 좌하/우하
            new[] { 0, -1, -1, -1 }, // (0,-1)→(-1,-2)
            new[] { 0, -1,  1, -1 }, // (0,-1)→(1,-2)
            // 우로 직선 → 우상/우하
            new[] { 1, 0, 1,  1 },  // (1,0)→(2,1)
            new[] { 1, 0, 1, -1 },  // (1,0)→(2,-1)
            // 좌로 직선 → 좌상/좌하
            new[] { -1, 0, -1,  1 }, // (-1,0)→(-2,1)
            new[] { -1, 0, -1, -1 }, // (-1,0)→(-2,-1)
        };

        public List<BoardPosition> GetValidMoves(Board board, Piece piece)
        {
            var moves = new List<BoardPosition>();
            var from = piece.Position;

            foreach (var pattern in MovePatterns)
            {
                // 1단계: 직선 1칸 (길막 체크)
                var step1 = from.Offset(pattern[0], pattern[1]);
                if (!step1.IsValid() || board.HasPieceAt(step1))
                    continue; // 길막

                // 2단계: 대각선 1칸 (최종 도착)
                var destination = step1.Offset(pattern[2], pattern[3]);
                if (!destination.IsValid())
                    continue;

                var target = board.GetPieceAt(destination);
                if (target == null || target.Side != piece.Side)
                    moves.Add(destination);
            }

            return moves;
        }
    }
}
