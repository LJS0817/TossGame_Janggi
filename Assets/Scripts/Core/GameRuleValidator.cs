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
    }
}
