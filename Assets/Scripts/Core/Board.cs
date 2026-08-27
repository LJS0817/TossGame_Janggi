using System.Collections.Generic;

namespace Janggi.Core
{
    /// <summary>
    /// 10×9 장기판의 상태를 관리합니다.
    /// Row 0~2: 초(Cho) 진영 / Row 7~9: 한(Han) 진영
    /// </summary>
    public class Board
    {
        private readonly Piece[,] _grid;
        private readonly List<Piece> _allPieces;

        public Board()
        {
            _grid = new Piece[BoardPosition.MaxCol, BoardPosition.MaxRow];
            _allPieces = new List<Piece>();
        }

        /// <summary>
        /// 보드의 복사본을 생성합니다 (시뮬레이션용).
        /// </summary>
        public Board Clone()
        {
            var clone = new Board();
            foreach (var piece in _allPieces)
            {
                if (piece.IsAlive)
                {
                    var clonedPiece = new Piece(piece.Type, piece.Side, piece.Position);
                    clone._grid[clonedPiece.Position.Col, clonedPiece.Position.Row] = clonedPiece;
                    clone._allPieces.Add(clonedPiece);
                }
            }
            return clone;
        }

        // ──────────────────────────────────────────────
        // 기물 조회
        // ──────────────────────────────────────────────

        /// <summary>
        /// 지정 좌표의 기물을 반환합니다. 없으면 null.
        /// </summary>
        public Piece GetPieceAt(BoardPosition pos)
        {
            if (!pos.IsValid()) return null;
            return _grid[pos.Col, pos.Row];
        }

        /// <summary>
        /// 지정 좌표에 기물이 있는지 확인합니다.
        /// </summary>
        public bool HasPieceAt(BoardPosition pos)
        {
            return GetPieceAt(pos) != null;
        }

        /// <summary>
        /// 지정 좌표가 비어있는지 확인합니다.
        /// </summary>
        public bool IsEmpty(BoardPosition pos)
        {
            return pos.IsValid() && GetPieceAt(pos) == null;
        }

        /// <summary>
        /// 보드 위의 모든 살아있는 기물 목록을 반환합니다.
        /// </summary>
        public List<Piece> GetAllPieces()
        {
            var result = new List<Piece>();
            foreach (var piece in _allPieces)
            {
                if (piece.IsAlive)
                    result.Add(piece);
            }
            return result;
        }

        /// <summary>
        /// 지정 진영의 모든 살아있는 기물 목록을 반환합니다.
        /// </summary>
        public List<Piece> GetPiecesBySide(PlayerSide side)
        {
            var result = new List<Piece>();
            foreach (var piece in _allPieces)
            {
                if (piece.IsAlive && piece.Side == side)
                    result.Add(piece);
            }
            return result;
        }

        /// <summary>
        /// 지정 진영의 왕(궁)을 찾습니다.
        /// </summary>
        public Piece FindKing(PlayerSide side)
        {
            foreach (var piece in _allPieces)
            {
                if (piece.IsAlive && piece.Type == PieceType.King && piece.Side == side)
                    return piece;
            }
            return null;
        }

        /// <summary>
        /// 지정 진영이 현재 필드에 보유한 모든 살아있는 기물들의 총 코스트 합계를 반환합니다.
        /// </summary>
        public int GetTotalPieceCost(PlayerSide side)
        {
            int total = 0;
            foreach (var piece in _allPieces)
            {
                if (piece.IsAlive && piece.Side == side)
                {
                    total += piece.Type.GetCost();
                }
            }
            return total;
        }

        // ──────────────────────────────────────────────
        // 기물 배치 / 이동 / 제거
        // ──────────────────────────────────────────────

        /// <summary>
        /// 기물을 보드에 배치합니다.
        /// </summary>
        public void PlacePiece(Piece piece)
        {
            var pos = piece.Position;
            _grid[pos.Col, pos.Row] = piece;
            if (!_allPieces.Contains(piece))
                _allPieces.Add(piece);
        }

        /// <summary>
        /// 기물을 보드에서 제거합니다 (잡힘).
        /// </summary>
        public void RemovePiece(Piece piece)
        {
            var pos = piece.Position;
            if (_grid[pos.Col, pos.Row] == piece)
                _grid[pos.Col, pos.Row] = null;
            piece.IsAlive = false;
        }

        /// <summary>
        /// 기물을 from에서 to로 이동합니다. to에 적 기물이 있으면 잡습니다.
        /// </summary>
        public Piece MovePiece(Piece piece, BoardPosition to)
        {
            Piece captured = null;

            // 도착지에 적 기물이 있으면 잡기
            var target = GetPieceAt(to);
            if (target != null && target.Side != piece.Side)
            {
                captured = target;
                RemovePiece(target);
            }

            // 현재 위치에서 제거
            var from = piece.Position;
            _grid[from.Col, from.Row] = null;

            // 새 위치로 이동
            piece.Position = to;
            _grid[to.Col, to.Row] = piece;

            return captured;
        }

        // ──────────────────────────────────────────────
        // 초기 배치
        // ──────────────────────────────────────────────

        /// <summary>
        /// gemini.md §4에 따라 왕(궁)과 사(신하)만 초기 배치합니다.
        /// 나머지 기물(차/마/상/포/졸)은 게임 중 소환 시스템으로 배치됩니다.
        /// </summary>
        public void SetupInitialPosition()
        {
            // 초(Cho) — Row 0~2 (화면 아래)
            // 왕: (4, 1) — 궁성 정중앙
            PlacePiece(new Piece(PieceType.King, PlayerSide.Cho, new BoardPosition(4, 1)));
            // 사: (3, 0), (5, 0) — 궁성 하단 양쪽
            PlacePiece(new Piece(PieceType.Advisor, PlayerSide.Cho, new BoardPosition(3, 0)));
            PlacePiece(new Piece(PieceType.Advisor, PlayerSide.Cho, new BoardPosition(5, 0)));

            // 한(Han) — Row 7~9 (화면 위)
            // 왕: (4, 8) — 궁성 정중앙
            PlacePiece(new Piece(PieceType.King, PlayerSide.Han, new BoardPosition(4, 8)));
            // 사: (3, 9), (5, 9) — 궁성 상단 양쪽
            PlacePiece(new Piece(PieceType.Advisor, PlayerSide.Han, new BoardPosition(3, 9)));
            PlacePiece(new Piece(PieceType.Advisor, PlayerSide.Han, new BoardPosition(5, 9)));
        }

        // ──────────────────────────────────────────────
        // 궁성 유틸리티
        // ──────────────────────────────────────────────

        /// <summary>
        /// 궁성 내의 대각선 이동 가능 위치 쌍을 반환합니다.
        /// 궁성의 대각선은 중앙(4, 1/8)을 기준으로 네 꼭짓점으로 연결됩니다.
        /// </summary>
        public static bool IsPalaceDiagonalMove(BoardPosition from, BoardPosition to, PlayerSide side)
        {
            if (!from.IsInsidePalace(side) || !to.IsInsidePalace(side))
                return false;

            int dCol = System.Math.Abs(to.Col - from.Col);
            int dRow = System.Math.Abs(to.Row - from.Row);

            // 대각선 1칸 이동인지 확인
            if (dCol != 1 || dRow != 1)
                return false;

            // 궁성 중앙 좌표
            int centerCol = 4;
            int centerRow = side == PlayerSide.Cho ? 1 : 8;

            // 대각선은 중앙에서 꼭짓점으로, 또는 꼭짓점에서 중앙으로만 가능
            bool fromCenter = (from.Col == centerCol && from.Row == centerRow);
            bool toCenter = (to.Col == centerCol && to.Row == centerRow);

            return fromCenter || toCenter;
        }
    }
}
