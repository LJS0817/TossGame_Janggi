namespace Janggi.Core
{
    /// <summary>
    /// 장기의 진영(플레이어 편)을 정의합니다.
    /// </summary>
    public enum PlayerSide
    {
        /// <summary>초(楚) — 화면 아래쪽, 녹색 기물</summary>
        Cho,
        /// <summary>한(漢) — 화면 위쪽, 빨간 기물</summary>
        Han
    }

    /// <summary>
    /// PlayerSide 확장 메서드를 제공합니다.
    /// </summary>
    public static class PlayerSideExtensions
    {
        /// <summary>
        /// 상대 진영을 반환합니다.
        /// </summary>
        public static PlayerSide Opposite(this PlayerSide side)
        {
            return side == PlayerSide.Cho ? PlayerSide.Han : PlayerSide.Cho;
        }
    }
}
