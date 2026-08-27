using System;

namespace Janggi.Core
{
    /// <summary>
    /// 장기판 위의 좌표를 나타내는 불변 구조체.
    /// Col: 0~8 (가로 9줄), Row: 0~9 (세로 10줄)
    /// Row 0은 초(楚)의 최후방(화면 아래), Row 9는 한(漢)의 최후방(화면 위).
    /// </summary>
    public readonly struct BoardPosition : IEquatable<BoardPosition>
    {
        public const int MaxCol = 9;  // 가로 칸 수
        public const int MaxRow = 10; // 세로 칸 수

        /// <summary>가로 좌표 (0~8)</summary>
        public readonly int Col;

        /// <summary>세로 좌표 (0~9)</summary>
        public readonly int Row;

        public BoardPosition(int col, int row)
        {
            Col = col;
            Row = row;
        }

        /// <summary>
        /// 이 좌표가 보드 범위 안에 있는지 확인합니다.
        /// </summary>
        public bool IsValid()
        {
            return Col >= 0 && Col < MaxCol && Row >= 0 && Row < MaxRow;
        }

        /// <summary>
        /// 이 좌표가 지정된 진영의 궁성(3×3) 범위 안에 있는지 확인합니다.
        /// 초(Cho): col 3~5, row 0~2
        /// 한(Han): col 3~5, row 7~9
        /// </summary>
        public bool IsInsidePalace(PlayerSide side)
        {
            if (Col < 3 || Col > 5) return false;

            if (side == PlayerSide.Cho)
                return Row >= 0 && Row <= 2;
            else
                return Row >= 7 && Row <= 9;
        }

        /// <summary>
        /// 이 좌표가 어느 쪽이든 궁성 안에 있는지 확인합니다.
        /// </summary>
        public bool IsInsideAnyPalace()
        {
            return IsInsidePalace(PlayerSide.Cho) || IsInsidePalace(PlayerSide.Han);
        }

        /// <summary>
        /// 상대 좌표를 더해 새 위치를 반환합니다.
        /// </summary>
        public BoardPosition Offset(int dCol, int dRow)
        {
            return new BoardPosition(Col + dCol, Row + dRow);
        }

        public bool Equals(BoardPosition other)
        {
            return Col == other.Col && Row == other.Row;
        }

        public override bool Equals(object obj)
        {
            return obj is BoardPosition other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Col * 31 + Row;
        }

        public static bool operator ==(BoardPosition left, BoardPosition right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BoardPosition left, BoardPosition right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"({Col}, {Row})";
        }
    }
}
