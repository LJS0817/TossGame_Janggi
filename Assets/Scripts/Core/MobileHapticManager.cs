using System;
using UnityEngine;

namespace Janggi.Core
{
    public enum HapticType
    {
        Light,
        Medium,
        Heavy,
        Success,
        Warning,
        Error
    }

    /// <summary>
    /// 모바일(Android / iOS) 및 에디터 환경을 위한 범용 햅틱 진동 피드백 매니저.
    /// </summary>
    public class MobileHapticManager : MonoBehaviour
    {
        private static MobileHapticManager _instance;
        public static MobileHapticManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("MobileHapticManager");
                    _instance = go.AddComponent<MobileHapticManager>();
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
        /// 햅틱 피드백을 트리거합니다.
        /// </summary>
        public void Trigger(HapticType type)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                // Android 기본 진동 피드백 지원
                if (SystemInfo.supportsVibration)
                {
                    Handheld.Vibrate();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Haptic] 진동 호출 실패: {ex.Message}");
            }
#elif UNITY_IOS && !UNITY_EDITOR
            try
            {
                if (SystemInfo.supportsVibration)
                {
                    Handheld.Vibrate();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Haptic] 진동 호출 실패: {ex.Message}");
            }
#else
            Debug.Log($"[Haptic (Editor Mock)] 햅틱 피드백 트리거: {type}");
#endif
        }
    }
}
