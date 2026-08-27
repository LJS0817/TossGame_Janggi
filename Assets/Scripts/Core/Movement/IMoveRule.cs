using System.Collections.Generic;

namespace Janggi.Core.Movement
{
    /// <summary>
    /// 기물의 행마법(이동 규칙) 인터페이스.
    /// 각 기물 종류별로 구현체를 제공합니다.
    /// </summary>
    public interface IMoveRule
    {
        /// <summary>
        /// 주어진 보드 상태에서 해당 기물이 이동할 수 있는 모든 유효 좌표를 반환합니다.
        /// 반환되는 좌표에는 빈칸 이동과 적 기물 잡기가 모두 포함됩니다.
        /// 주의: 이 메서드는 장군 상태 체크(자신의 왕이 위험해지는 수)는 포함하지 않습니다.
        /// </summary>
        List<BoardPosition> GetValidMoves(Board board, Piece piece);
    }
}
