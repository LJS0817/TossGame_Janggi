using System;
using Janggi.Core;

namespace Janggi.AI
{
    /// <summary>
    /// 장기 보드 상태를 정량적으로 평가하는 엔진.
    /// 기물 가치(차>포>마/상>졸), 위치 가중치, 장군/외통수, 스폰 블로킹을 평가합니다.
    /// </summary>
    public static class BoardEvaluator
    {
        public const int CheckmateScore = 99999;
        public const int CheckBonus = 50;

        /// <summary>
        /// 기물의 기본 점수 (장기 표준 가치 기반)
        /// 차(130) > 포(70) > 마(50) > 상(30) = 사(30) > 졸(20)
        /// </summary>
        public static int GetPieceValue(PieceType type)
        {
            switch (type)
            {
                case PieceType.King:     return 10000;
                case PieceType.Chariot:  return 130;
                case PieceType.Cannon:   return 70;
                case PieceType.Horse:    return 50;
                case PieceType.Elephant: return 30;
                case PieceType.Advisor:  return 30;
                case PieceType.Pawn:     return 20;
                default:                 return 0;
            }
        }

        /// <summary>
        /// 특정 진영(forSide)의 관점에서 현재 보드의 점수를 계산합니다. (점수가 높을수록 유리)
        /// </summary>
        public static int Evaluate(Board board, PlayerSide forSide)
        {
            var oppositeSide = forSide.Opposite();

            // 1. 외통수(체크메이트) 판정
            if (GameRuleValidator.IsCheckmate(board, oppositeSide))
                return CheckmateScore; // 내가 승리

            if (GameRuleValidator.IsCheckmate(board, forSide))
                return -CheckmateScore; // 내가 패배

            int score = 0;

            // 3. 기물 점수 및 위치 가중치 합산
            var allPieces = board.GetAllPieces();
            foreach (var piece in allPieces)
            {
                int pieceScore = GetPieceValue(piece.Type) + GetPositionBonus(piece);

                if (piece.Side == forSide)
                    score += pieceScore;
                else
                    score -= pieceScore;
            }

            // 4. 장군 보너스
            if (GameRuleValidator.IsInCheck(board, oppositeSide))
                score += CheckBonus; // 상대에게 장군을 침

            if (GameRuleValidator.IsInCheck(board, forSide))
                score -= CheckBonus; // 내가 장군을 당함

            return score;
        }

        /// <summary>
        /// 기물의 위치에 따른 전술적 보너스 점수를 계산합니다.
        /// </summary>
        private static int GetPositionBonus(Piece piece)
        {
            int bonus = 0;
            var pos = piece.Position;

            // 중앙(Col 3,4,5) 장악 보너스
            if (pos.Col >= 3 && pos.Col <= 5)
            {
                bonus += 5;
            }

            switch (piece.Type)
            {
                case PieceType.Pawn:
                    // 졸/병은 전진할수록 가치가 올라감
                    if (piece.Side == PlayerSide.Cho)
                        bonus += pos.Row * 2; // 위로 전진
                    else
                        bonus += (9 - pos.Row) * 2; // 아래로 전진
                    break;

                case PieceType.Horse:
                case PieceType.Elephant:
                    // 마/상은 중앙 진출 시 활약도 증가
                    if (pos.Row >= 3 && pos.Row <= 6)
                        bonus += 8;
                    break;

                case PieceType.Chariot:
                    // 차는 적 진영 깊숙이 침투 시 높은 점수
                    if (piece.Side == PlayerSide.Han && pos.Row <= 3)
                        bonus += 15; // 한 차가 초 진영 침투
                    else if (piece.Side == PlayerSide.Cho && pos.Row >= 6)
                        bonus += 15;
                    break;

                case PieceType.Cannon:
                    // 포는 궁성 주변 조준 시 가치 상승
                    if (pos.Col >= 3 && pos.Col <= 5)
                        bonus += 10;
                    break;
            }

            // 상대 소환 구역 점거(스폰 블로킹) 보너스
            if (IsBlockingOpponentSpawn(piece))
            {
                bonus += 12;
            }

            return bonus;
        }

        /// <summary>
        /// 해당 기물이 상대방의 핵심 소환 구역(4선, 1선, 포진지)을 점거(스폰 블로킹)하고 있는지 판정합니다.
        /// </summary>
        public static bool IsBlockingOpponentSpawn(Piece piece)
        {
            var pos = piece.Position;
            if (piece.Side == PlayerSide.Han)
            {
                // 한(AI) 기물이 초(플레이어) 소환 구역에 위치하는지
                if (pos.Row == 3) return true; // 초의 졸 소환선 (4선)
                if (pos.Row == 0) return true; // 초의 차/마/상 소환선 (1선)
                if (pos == new BoardPosition(1, 2) || pos == new BoardPosition(7, 2)) return true; // 초 포진지
            }
            else
            {
                // 초 기물이 한 소환 구역에 위치하는지
                if (pos.Row == 6) return true; // 한의 졸 소환선 (6선)
                if (pos.Row == 9) return true; // 한의 차/마/상 소환선 (9선)
                if (pos == new BoardPosition(1, 7) || pos == new BoardPosition(7, 7)) return true; // 한 포진지
            }

            return false;
        }
    }
}
