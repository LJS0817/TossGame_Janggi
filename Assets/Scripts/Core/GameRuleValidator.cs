using System.Collections.Generic;
using Janggi.Core.Movement;

namespace Janggi.Core
{
    /// <summary>
    /// 장기의 게임 규칙을 판정합니다.
    /// 장군(체크), 외통수(체크메이트), 빅장(면장) 등을 검증합니다.
    /// </summary>
    public static class GameRuleValidator
    {
        /// <summary>
        /// 지정된 진영의 왕이 현재 장군(체크) 상태인지 확인합니다.
        /// 상대방의 어떤 기물이라도 왕의 위치를 공격할 수 있으면 장군입니다.
        /// </summary>
        public static bool IsInCheck(Board board, PlayerSide side)
        {
            var king = board.FindKing(side);
            if (king == null) return false;

            var enemySide = side.Opposite();
            var enemyPieces = board.GetPiecesBySide(enemySide);

            foreach (var enemy in enemyPieces)
            {
                var validMoves = MoveRuleFactory.GetValidMoves(board, enemy);
                foreach (var move in validMoves)
                {
                    if (move == king.Position)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 지정된 기물의 이동이 합법적인지 확인합니다.
        /// 이동 후 자신의 왕이 장군 상태가 되지 않아야 합니다.
        /// </summary>
        public static bool IsMoveLegal(Board board, Piece piece, BoardPosition to)
        {
            // 보드를 복사하여 시뮬레이션
            var simBoard = board.Clone();
            var simPiece = simBoard.GetPieceAt(piece.Position);
            if (simPiece == null) return false;

            simBoard.MovePiece(simPiece, to);

            // 이동 후 자신의 왕이 장군 상태이면 불법 수
            return !IsInCheck(simBoard, piece.Side);
        }

        /// <summary>
        /// 지정된 기물의 모든 합법수(장군 체크 포함)를 반환합니다.
        /// </summary>
        public static List<BoardPosition> GetLegalMoves(Board board, Piece piece)
        {
            var rawMoves = MoveRuleFactory.GetValidMoves(board, piece);
            var legalMoves = new List<BoardPosition>();

            foreach (var move in rawMoves)
            {
                if (IsMoveLegal(board, piece, move))
                    legalMoves.Add(move);
            }

            return legalMoves;
        }

        /// <summary>
        /// 지정된 진영의 모든 합법수를 반환합니다.
        /// </summary>
        public static List<(Piece piece, BoardPosition to)> GetAllLegalMovesForSide(
            Board board, PlayerSide side)
        {
            var allMoves = new List<(Piece, BoardPosition)>();
            var pieces = board.GetPiecesBySide(side);

            foreach (var piece in pieces)
            {
                var legalMoves = GetLegalMoves(board, piece);
                foreach (var move in legalMoves)
                {
                    allMoves.Add((piece, move));
                }
            }

            return allMoves;
        }

        /// <summary>
        /// 지정된 진영이 외통수(체크메이트) 상태인지 확인합니다.
        /// 장군 상태이면서 합법수가 하나도 없으면 외통수입니다.
        /// </summary>
        public static bool IsCheckmate(Board board, PlayerSide side)
        {
            // 먼저 장군 상태인지 확인
            if (!IsInCheck(board, side))
                return false;

            // 합법수가 하나라도 있으면 외통수가 아님
            var allMoves = GetAllLegalMovesForSide(board, side);
            return allMoves.Count == 0;
        }

        /// <summary>
        /// 지정된 진영이 움직일 수 없는 상태(스테일메이트)인지 확인합니다.
        /// 장군 상태가 아닌데 합법수가 없으면 교착 상태입니다.
        /// </summary>
        public static bool IsStalemate(Board board, PlayerSide side)
        {
            if (IsInCheck(board, side))
                return false;

            var allMoves = GetAllLegalMovesForSide(board, side);
            return allMoves.Count == 0;
        }

        /// <summary>
        /// 빅장(面將) 상태인지 확인합니다.
        /// 양쪽 왕이 같은 열(Col)에 있고, 그 사이에 다른 기물이 없으면 빅장입니다.
        /// </summary>
        public static bool IsBikjang(Board board)
        {
            var choKing = board.FindKing(PlayerSide.Cho);
            var hanKing = board.FindKing(PlayerSide.Han);

            if (choKing == null || hanKing == null)
                return false;

            // 같은 열에 있어야 함
            if (choKing.Position.Col != hanKing.Position.Col)
                return false;

            int col = choKing.Position.Col;
            int minRow = System.Math.Min(choKing.Position.Row, hanKing.Position.Row);
            int maxRow = System.Math.Max(choKing.Position.Row, hanKing.Position.Row);

            // 두 왕 사이에 기물이 있는지 확인
            for (int row = minRow + 1; row < maxRow; row++)
            {
                if (board.HasPieceAt(new BoardPosition(col, row)))
                    return false; // 사이에 기물이 있으면 빅장이 아님
            }

            return true;
        }

        /// <summary>
        /// 게임 종료 시점의 보드를 분석하여 외통수/종료 원인 및 복기 데이터를 생성합니다.
        /// </summary>
        public static BoardReviewData AnalyzeGameOver(Board board, PlayerSide loserSide, bool isDraw, bool isPlayerWin)
        {
            var data = new BoardReviewData
            {
                IsCheckmate = !isDraw,
                IsDraw = isDraw,
                IsPlayerWin = isPlayerWin
            };

            if (isDraw)
            {
                data.Title = isPlayerWin ? "⚖️ 무승부 판정" : "⚖️ 게임 종료 (무승부)";
                data.Explanation = "• 양측 모두 공격 기물이 소진되었거나 교착 상태(Stalemate)가 발생하여 무승부로 종료되었습니다.";
                return data;
            }

            var king = board.FindKing(loserSide);
            data.KingPiece = king;
            var winnerSide = loserSide.Opposite();
            var winnerPieces = board.GetPiecesBySide(winnerSide);

            if (king != null)
            {
                // 1. 직접 공격 기물 (장군 친 기물)
                foreach (var piece in winnerPieces)
                {
                    var moves = MoveRuleFactory.GetValidMoves(board, piece);
                    if (moves.Contains(king.Position))
                    {
                        data.DirectAttackers.Add(piece);
                    }
                }

                // 2. 왕의 후보 이동 칸들 중 적의 공격선에 막힌 칸들 분석
                var rawKingMoves = MoveRuleFactory.GetValidMoves(board, king);
                foreach (var movePos in rawKingMoves)
                {
                    var simBoard = board.Clone();
                    var simKing = simBoard.GetPieceAt(king.Position);
                    if (simKing != null)
                    {
                        simBoard.MovePiece(simKing, movePos);
                        foreach (var enemy in winnerPieces)
                        {
                            var enemyMoves = MoveRuleFactory.GetValidMoves(simBoard, enemy);
                            if (enemyMoves.Contains(movePos))
                            {
                                if (!data.BlockedEscapePositions.Contains(movePos))
                                    data.BlockedEscapePositions.Add(movePos);

                                if (!data.PathControllers.Contains(enemy) && !data.DirectAttackers.Contains(enemy))
                                    data.PathControllers.Add(enemy);

                                break;
                            }
                        }
                    }
                }
            }

            // 3. 설명 문구 작성
            data.Title = isPlayerWin ? "⚔️ 외통수 승리 (외통수 제압)" : "💀 외통수 패배 (외통수 당함)";
            
            var sb = new System.Text.StringBuilder();
            
            // 직접 공격자
            if (data.DirectAttackers.Count > 0)
            {
                var atkList = new List<string>();
                foreach (var atk in data.DirectAttackers)
                {
                    atkList.Add($"[{atk.GetFullDisplayName()}]");
                }
                string atkNames = string.Join(", ", atkList);
                string kingName = king != null ? $"[{king.GetFullDisplayName()}]" : "궁";
                sb.Append($"• 직접 장군: {atkNames}이(가) {kingName}을(를) 직접 조준 공격 중입니다.\n");
            }

            // 경로 차단자
            if (data.PathControllers.Count > 0)
            {
                var ctrlList = new List<string>();
                foreach (var ctrl in data.PathControllers)
                {
                    ctrlList.Add($"[{ctrl.GetFullDisplayName()}]");
                }
                string ctrlNames = string.Join(", ", ctrlList);
                sb.Append($"• 퇴로 차단: {ctrlNames}이(가) 궁의 탈출로를 통제하여 도망칠 수 없습니다.\n");
            }
            else if (data.BlockedEscapePositions.Count > 0)
            {
                sb.Append("• 퇴로 차단: 궁성 내 모든 인접 칸이 상대 기물의 공격 범위에 막혀 있습니다.\n");
            }

            sb.Append("• 수비 불가: 공격 기물을 잡거나 사이를 가로막을 수 있는 합법 수가 없습니다.");

            data.Explanation = sb.ToString();
            return data;
        }
    }

    /// <summary>
    /// 게임 종료 복기 분석 데이터
    /// </summary>
    public class BoardReviewData
    {
        public bool IsCheckmate { get; set; }
        public bool IsDraw { get; set; }
        public bool IsPlayerWin { get; set; }
        public Piece KingPiece { get; set; }
        public List<Piece> DirectAttackers { get; set; } = new List<Piece>();
        public List<Piece> PathControllers { get; set; } = new List<Piece>();
        public List<BoardPosition> BlockedEscapePositions { get; set; } = new List<BoardPosition>();
        public string Title { get; set; }
        public string Explanation { get; set; }
    }
}
