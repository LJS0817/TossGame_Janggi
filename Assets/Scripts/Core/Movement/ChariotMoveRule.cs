using System.Collections.Generic;

namespace Janggi.Core.Movement
{
    /// <summary>
    /// 차(車)의 행마법.
    /// 상하좌우 직선으로 무제한 이동. 기물을 만나면 정지 (적이면 잡기 가능).
    /// 궁성 내에서는 대각선 이동도 가능.
    /// </summary>
    public class ChariotMoveRule : IMoveRule
    {
        // 상하좌우 방향
        private static readonly int[,] OrthogonalDirs = {
            { 0, 1 }, { 0, -1 }, { 1, 0 }, { -1, 0 }
        };

        // 대각선 방향 (궁성 내 전용)
        private static readonly int[,] DiagonalDirs = {
            { 1, 1 }, { 1, -1 }, { -1, 1 }, { -1, -1 }
        };

        public List<BoardPosition> GetValidMoves(Board board, Piece piece)
        {
            var moves = new List<BoardPosition>();
            var from = piece.Position;

            // 1. 상하좌우 직선 이동
            for (int d = 0; d < OrthogonalDirs.GetLength(0); d++)
            {
                int dCol = OrthogonalDirs[d, 0];
                int dRow = OrthogonalDirs[d, 1];

                var current = from;
                while (true)
                {
                    current = current.Offset(dCol, dRow);
                    if (!current.IsValid())
                        break;

                    var target = board.GetPieceAt(current);
                    if (target == null)
                    {
                        moves.Add(current);
                    }
                    else
                    {
                        // 적 기물이면 잡을 수 있음
                        if (target.Side != piece.Side)
                            moves.Add(current);
                        break; // 기물을 만나면 더 이상 진행 불가
                    }
                }
            }

            // 2. 궁성 내 대각선 이동
            if (from.IsInsideAnyPalace())
            {
                // 어느 쪽 궁성에 있는지 확인
                PlayerSide palaceSide = from.IsInsidePalace(PlayerSide.Cho)
                    ? PlayerSide.Cho
                    : PlayerSide.Han;

                for (int d = 0; d < DiagonalDirs.GetLength(0); d++)
                {
                    int dCol = DiagonalDirs[d, 0];
                    int dRow = DiagonalDirs[d, 1];

                    var current = from;
                    while (true)
                    {
                        var next = current.Offset(dCol, dRow);
                        if (!next.IsValid() || !next.IsInsidePalace(palaceSide))
                            break;

                        // 궁성 대각선 경로 검증
                        if (!Board.IsPalaceDiagonalMove(current, next, palaceSide))
                            break;

                        var target = board.GetPieceAt(next);
                        if (target == null)
                        {
                            moves.Add(next);
                            current = next;
                        }
                        else
                        {
                            if (target.Side != piece.Side)
                                moves.Add(next);
                            break;
                        }
                    }
                }
            }

            return moves;
        }
    }
}
