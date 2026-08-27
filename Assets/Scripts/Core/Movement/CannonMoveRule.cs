using System.Collections.Generic;

namespace Janggi.Core.Movement
{
    /// <summary>
    /// 포(包)의 행마법.
    /// 직선으로 이동하되, 반드시 다른 기물 1개를 뛰어넘어야 함 (포대).
    /// 포끼리는 뛰어넘거나 잡을 수 없음.
    /// 궁성 내에서는 대각선 포격도 가능.
    /// </summary>
    public class CannonMoveRule : IMoveRule
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

            // 1. 상하좌우 직선 포격
            for (int d = 0; d < OrthogonalDirs.GetLength(0); d++)
            {
                int dCol = OrthogonalDirs[d, 0];
                int dRow = OrthogonalDirs[d, 1];

                AddCannonMovesInDirection(board, piece, from, dCol, dRow, moves, false);
            }

            // 2. 궁성 내 대각선 포격
            if (from.IsInsideAnyPalace())
            {
                PlayerSide palaceSide = from.IsInsidePalace(PlayerSide.Cho)
                    ? PlayerSide.Cho
                    : PlayerSide.Han;

                for (int d = 0; d < DiagonalDirs.GetLength(0); d++)
                {
                    int dCol = DiagonalDirs[d, 0];
                    int dRow = DiagonalDirs[d, 1];

                    AddCannonMovesInDirection(board, piece, from, dCol, dRow, moves, true);
                }
            }

            return moves;
        }

        private void AddCannonMovesInDirection(Board board, Piece piece, BoardPosition from,
            int dCol, int dRow, List<BoardPosition> moves, bool isPalaceDiagonal)
        {
            bool foundPlatform = false; // 포대(뛰어넘을 기물) 발견 여부
            var current = from;

            // 궁성 대각선인 경우 어느 궁성인지 기억
            PlayerSide palaceSide = from.IsInsidePalace(PlayerSide.Cho)
                ? PlayerSide.Cho
                : PlayerSide.Han;

            while (true)
            {
                var next = current.Offset(dCol, dRow);
                if (!next.IsValid())
                    break;

                // 궁성 대각선 이동 시 궁성 범위 및 대각선 경로 검증
                if (isPalaceDiagonal)
                {
                    if (!next.IsInsidePalace(palaceSide))
                        break;
                    if (!Board.IsPalaceDiagonalMove(current, next, palaceSide))
                        break;
                }

                current = next;
                var target = board.GetPieceAt(current);

                if (!foundPlatform)
                {
                    // 포대 찾기: 포가 아닌 기물이 있어야 함
                    if (target != null)
                    {
                        if (target.Type == PieceType.Cannon)
                            break; // 포는 포를 뛰어넘을 수 없음
                        foundPlatform = true;
                    }
                }
                else
                {
                    // 포대를 넘은 후
                    if (target == null)
                    {
                        moves.Add(current);
                    }
                    else
                    {
                        // 포는 포를 잡을 수 없음
                        if (target.Type != PieceType.Cannon && target.Side != piece.Side)
                            moves.Add(current);
                        break; // 두 번째 기물을 만나면 정지
                    }
                }
            }
        }
    }
}
