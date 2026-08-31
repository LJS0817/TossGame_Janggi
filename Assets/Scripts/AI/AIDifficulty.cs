using Janggi.Core;

namespace Janggi.AI
{
    /// <summary>
    /// gemini.md §5에 정의된 4단계 PvE AI 난이도.
    /// </summary>
    public enum AIDifficulty
    {
        /// <summary>하 (입문) — 1수 탐색. 코스트 모이는 대로 소환, 왕 방어 무시하고 눈앞의 기물만 공격</summary>
        Easy,
        /// <summary>중 (보통) — 2~3수 탐색. 기물 가치(차>포>마/상>졸) 계산, 자원 비축, 위험 시 왕 방어</summary>
        Normal,
        /// <summary>상 (숙련) — 4~5수 탐색. 소환 직후 즉시 공격(소환 스나이핑) 적극 활용, 소환 구역 점거(블로킹)</summary>
        Hard,
        /// <summary>극악 (신) — 심층 MCTS. 외통수 턴 계산해 자원 집중 투입, 플레이어 공개 손패 예측 사전 수비</summary>
        Hell
    }

    public static class AIDifficultyExtensions
    {
        public static string GetDisplayName(this AIDifficulty difficulty)
        {
            return LocalizationManager.GetDifficultyName(difficulty);
        }
    }
}
