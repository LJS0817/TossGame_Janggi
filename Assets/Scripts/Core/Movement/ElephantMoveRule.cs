using System.Collections.Generic;

namespace Janggi.Core.Movement
{
    /// <summary>
    /// 상(象)의 행마법.
    /// 직선 1칸 + 대각선 2칸 (총 3칸 이동).
    /// 경로 상의 모든 칸에 기물이 있으면 이동 불가 (길막/멱).
    /// 
    /// 이동 패턴 (8방향):
    ///   직선1칸(상하좌우) → 대각선1칸 → 대각선1칸 (같은 방향으로)
    /// </summary>
    public class ElephantMoveRule : IMoveRule
    {
        // { 직선dCol, 직선dRow, 대각1dCol, 대각1dRow, 대각2dCol, 대각2dRow }
        // 각 방향에서 직선 이동 후 대각선으로 벌어지는 두 갈래
        private static readonly int[][] MovePatterns = new int[][]
        {
            // 위로 직선 → 좌상/우상 대각
            new[] { 0, 1,  -1, 1,  -1, 1 },  // 상→좌상→좌상 = (0,1)→(-1,2)→(-2,3)
            new[] { 0, 1,   1, 1,   1, 1 },  // 상→우상→우상 = (0,1)→(1,2)→(2,3)
            // 아래로 직선 → 좌하/우하 대각
            new[] { 0, -1, -1, -1, -1, -1 }, // 하→좌하→좌하 = (0,-1)→(-1,-2)→(-2,-3)
            new[] { 0, -1,  1, -1,  1, -1 }, // 하→우하→우하 = (0,-1)→(1,-2)→(2,-3)
            // 우로 직선 → 우상/우하 대각
            new[] { 1, 0,  1, 1,  1, 1 },    // 우→우상→우상 = (1,0)→(2,1)→(3,2)
            new[] { 1, 0,  1, -1, 1, -1 },   // 우→우하→우하 = (1,0)→(2,-1)→(3,-2)
            // 좌로 직선 → 좌상/좌하 대각
            new[] { -1, 0, -1, 1, -1, 1 },   // 좌→좌상→좌상 = (-1,0)→(-2,1)→(-3,2)
            new[] { -1, 0, -1, -1, -1, -1 }, // 좌→좌하→좌하 = (-1,0)→(-2,-1)→(-3,-2)
        };

        public List<BoardPosition> GetValidMoves(Board board, Piece piece)
        {
            var moves = new List<BoardPosition>();
            var from = piece.Position;

            foreach (var pattern in MovePatterns)
            {
                // 1단계: 직선 1칸
                var step1 = from.Offset(pattern[0], pattern[1]);
                if (!step1.IsValid() || board.HasPieceAt(step1))
                    continue; // 길막

                // 2단계: 대각선 1칸
                var step2 = step1.Offset(pattern[2], pattern[3]);
                if (!step2.IsValid() || board.HasPieceAt(step2))
                    continue; // 길막

                // 3단계: 대각선 1칸 (최종 도착)
                var destination = step2.Offset(pattern[4], pattern[5]);
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
