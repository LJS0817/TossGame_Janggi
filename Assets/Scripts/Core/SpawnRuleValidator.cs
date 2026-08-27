using System.Collections.Generic;

namespace Janggi.Core
{
    /// <summary>
    /// 기물별 소환 구역 규칙 및 스폰 블로킹을 검증합니다.
    /// </summary>
    public static class SpawnRuleValidator
    {
        // 포의 기본 포진 위치 (초: 2열/8열의 3선, 한: 2열/8열의 8선)
        private static readonly BoardPosition[] ChoCannonSpawnPositions = new BoardPosition[]
        {
            new BoardPosition(1, 2),
            new BoardPosition(7, 2)
        };

        private static readonly BoardPosition[] HanCannonSpawnPositions = new BoardPosition[]
        {
            new BoardPosition(1, 7),
            new BoardPosition(7, 7)
        };

        /// <summary>
        /// 주어진 기물 종류와 진영에 대해 현재 보드 상에서 소환 가능한 모든 빈칸 좌표를 반환합니다.
        /// (소환 즉시 상대 왕에게 장군이 되는 위치는 소환 불가)
        /// </summary>
        public static List<BoardPosition> GetSpawnablePositions(Board board, PlayerSide side, PieceType type)
        {
            var candidatePositions = new List<BoardPosition>();
            if (!type.IsSummonable()) return candidatePositions;

            // 필드 총 코스트 상한(20) 초과 시 소환 불가
            if (board != null && board.GetTotalPieceCost(side) + type.GetCost() > PlayerState.MaxFieldCost)
            {
                return candidatePositions;
            }

            switch (type)
            {
                case PieceType.Pawn:
                    // 졸/병: 아군 4선 (초: Row 3, 한: Row 6)
                    int pawnRow = (side == PlayerSide.Cho) ? 3 : 6;
                    for (int col = 0; col < BoardPosition.MaxCol; col++)
                    {
                        var pos = new BoardPosition(col, pawnRow);
                        if (board.IsEmpty(pos))
                        {
                            candidatePositions.Add(pos);
                        }
                    }
                    break;

                case PieceType.Cannon:
                    // 포: 아군 포진 위치 2곳 중 빈칸
                    var cannonPositions = (side == PlayerSide.Cho)
                        ? ChoCannonSpawnPositions
                        : HanCannonSpawnPositions;

                    foreach (var pos in cannonPositions)
                    {
                        if (board.IsEmpty(pos))
                        {
                            candidatePositions.Add(pos);
                        }
                    }
                    break;

                case PieceType.Chariot:
                case PieceType.Horse:
                case PieceType.Elephant:
                    // 차 / 마 / 상: 아군 1선 최후방 라인 (초: Row 0, 한: Row 9)
                    int backRow = (side == PlayerSide.Cho) ? 0 : 9;
                    for (int col = 0; col < BoardPosition.MaxCol; col++)
                    {
                        var pos = new BoardPosition(col, backRow);
                        if (board.IsEmpty(pos))
                        {
                            candidatePositions.Add(pos);
                        }
                    }
                    break;
            }

            // 즉시 장군(Direct Check)이 되는 위치 필터링 (소환 즉시 장군 금지 룰)
            var opponentSide = side.Opposite();
            var validPositions = new List<BoardPosition>();

            foreach (var pos in candidatePositions)
            {
                var simBoard = board.Clone();
                simBoard.PlacePiece(new Piece(type, side, pos));

                // 소환 직후 상대 왕이 장군 상태가 되지 않는 안전한 위치만 허용
                if (!GameRuleValidator.IsInCheck(simBoard, opponentSide))
                {
                    validPositions.Add(pos);
                }
            }

            return validPositions;
        }

        /// <summary>
        /// 지정한 좌표에 해당 기물을 소환할 수 있는지 검증합니다.
        /// </summary>
        public static bool CanSpawnAt(Board board, PlayerSide side, PieceType type, BoardPosition pos)
        {
            if (!board.IsEmpty(pos)) return false;

            var validPositions = GetSpawnablePositions(board, side, type);
            return validPositions.Contains(pos);
        }
    }
}
