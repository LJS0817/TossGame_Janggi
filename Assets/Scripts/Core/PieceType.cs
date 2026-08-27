namespace Janggi.Core
{
    /// <summary>
    /// 장기 기물의 종류를 정의합니다.
    /// </summary>
    public enum PieceType
    {
        /// <summary>궁 (왕) — 코스트: 없음 (초기 배치 전용)</summary>
        King,
        /// <summary>사 (신하) — 코스트: 없음 (초기 배치 전용)</summary>
        Advisor,
        /// <summary>상 — 코스트: 2</summary>
        Elephant,
        /// <summary>마 — 코스트: 2</summary>
        Horse,
        /// <summary>차 — 코스트: 4</summary>
        Chariot,
        /// <summary>포 — 코스트: 3</summary>
        Cannon,
        /// <summary>졸/병 — 코스트: 1</summary>
        Pawn
    }

    /// <summary>
    /// 기물 종류에 대한 확장 메서드를 제공합니다.
    /// </summary>
    public static class PieceTypeExtensions
    {
        /// <summary>
        /// 해당 기물을 소환하는 데 필요한 코스트를 반환합니다.
        /// King과 Advisor는 소환 불가(초기 배치 전용)이므로 0을 반환합니다.
        /// </summary>
        public static int GetCost(this PieceType type)
        {
            switch (type)
            {
                case PieceType.King:     return 0;
                case PieceType.Advisor:  return 0;
                case PieceType.Pawn:     return 1;
                case PieceType.Horse:    return 2;
                case PieceType.Elephant: return 2;
                case PieceType.Cannon:   return 4;
                case PieceType.Chariot:  return 6;
                default:                 return 0;
            }
        }

        /// <summary>
        /// 기물의 한글 이름을 반환합니다.
        /// </summary>
        public static string GetKoreanName(this PieceType type, PlayerSide side)
        {
            switch (type)
            {
                case PieceType.King:     return side == PlayerSide.Cho ? "楚" : "漢";
                case PieceType.Advisor:  return "士";
                case PieceType.Elephant: return "象";
                case PieceType.Horse:    return "馬";
                case PieceType.Chariot:  return "車";
                case PieceType.Cannon:   return "包";
                case PieceType.Pawn:     return side == PlayerSide.Cho ? "卒" : "兵";
                default:                 return "?";
            }
        }

        /// <summary>
        /// 덱에서 뽑을 수 있는 기물인지 여부. King과 Advisor는 초기 배치 전용.
        /// </summary>
        public static bool IsSummonable(this PieceType type)
        {
            return type != PieceType.King && type != PieceType.Advisor;
        }
    }
}
