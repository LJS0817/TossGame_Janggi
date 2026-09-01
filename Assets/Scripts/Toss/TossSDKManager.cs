using System;
using UnityEngine;
using Janggi.Core;

namespace TossGame.Toss
{
    public enum TossHapticType
    {
        Light,
        Medium,
        Heavy,
        Success,
        Warning,
        Error
    }

    /// <summary>
    /// 모바일 범용 시스템 연동을 위한 호환 브릿지 매니저.
    /// </summary>
    public class TossSDKManager : MonoBehaviour
    {
        private static TossSDKManager _instance;
        public static TossSDKManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("TossSDKManager");
                    _instance = go.AddComponent<TossSDKManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 햅틱 진동 피드백을 트리거합니다.
        /// </summary>
        public void TriggerHaptic(TossHapticType type)
        {
            HapticType targetType = type switch
            {
                TossHapticType.Light => HapticType.Light,
                TossHapticType.Medium => HapticType.Medium,
                TossHapticType.Heavy => HapticType.Heavy,
                TossHapticType.Success => HapticType.Success,
                TossHapticType.Warning => HapticType.Warning,
                TossHapticType.Error => HapticType.Error,
                _ => HapticType.Medium
            };
            MobileHapticManager.Instance.Trigger(targetType);
        }

        /// <summary>
        /// 결과 공유 (일반 텍스트 공유)
        /// </summary>
        public void ShareGameResult(string title, string desc)
        {
            Debug.Log($"[Share] 결과 공유 호출:\n제목: {title}\n내용: {desc}");
        }

        /// <summary>
        /// 구글 보상형 광고를 호출합니다.
        /// </summary>
        public void ShowRewardedAd(Action<bool> onComplete)
        {
            GoogleAdManager.Instance.ShowRewardedAd(onComplete);
        }
    }
}
