namespace Janggi.Core
{
    /// <summary>
    /// 장기 기물 하나를 나타내는 클래스.
    /// 보드 위에 배치된 기물의 종류, 진영, 위치 정보를 담습니다.
    /// </summary>
    public class Piece
    {
        /// <summary>기물 종류</summary>
        public PieceType Type { get; private set; }

        /// <summary>소속 진영</summary>
        public PlayerSide Side { get; private set; }

        /// <summary>보드 상의 현재 위치</summary>
        public BoardPosition Position { get; set; }

        /// <summary>이 기물이 아직 보드 위에 살아있는지 여부</summary>
        public bool IsAlive { get; set; }

        public Piece(PieceType type, PlayerSide side, BoardPosition position)
        {
            Type = type;
            Side = side;
            Position = position;
            IsAlive = true;
        }

        /// <summary>
        /// 기물의 한자 표시명을 반환합니다.
        /// </summary>
        public string GetDisplayName()
        {
            return Type.GetKoreanName(Side);
        }

        /// <summary>
        /// 현재 언어에 맞게 진영과 기물명을 모두 포함한 상세 표시명을 반환합니다 (예: ko: "초(楚) 차", en: "Cho Chariot").
        /// </summary>
        public string GetFullDisplayName()
        {
            return LocalizationManager.GetFullPieceName(Type, Side);
        }

        /// <summary>
        /// 이 기물의 소환 코스트를 반환합니다.
        /// </summary>
        public int GetCost()
        {
            return Type.GetCost();
        }

        public override string ToString()
        {
            return $"{Side} {Type} at {Position}";
        }
    }
}
