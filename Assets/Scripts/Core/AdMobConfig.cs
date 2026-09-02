using System;
using UnityEngine;

namespace Janggi.Core
{
    /// <summary>
    /// 로컬 전용 애드몹 상용 키 설정 ScriptableObject.
    /// 로컬(Git 무시)로 관리되어 실제 상용 키를 안전하게 보관합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "AdMobSecretConfig", menuName = "Janggi/AdMob Secret Config")]
    public class AdMobConfig : ScriptableObject
    {
        [Header("AdMob App IDs")]
        public string androidAppId;
        public string iosAppId;

        [Header("AdMob Rewarded Ad Unit IDs (보상형 광고 단위 ID)")]
        public string androidRewardedAdUnitId;
        public string iosRewardedAdUnitId;
    }

    /// <summary>
    /// JSON 직렬화용 애드몹 시크릿 데이터 컨테이너.
    /// </summary>
    [Serializable]
    public class AdMobSecretData
    {
        public string androidAppId;
        public string iosAppId;
        public string androidRewardedAdUnitId;
        public string iosRewardedAdUnitId;
    }
}
