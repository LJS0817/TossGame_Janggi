using System;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Janggi.Core
{
    public enum HapticType
    {
        Light,      // 일반 말 놓기 (아주 약하게)
        Medium,     // 장군 / 멍군 (중간 강도)
        Heavy,      // 강한 타격
        Success,    // 승리 / 성공
        Warning,    // 경고
        Error       // 패배 / 오류
    }

    /// <summary>
    /// 모바일(Android / iOS), WebGL 및 에디터 환경을 위한 세밀한 진동 강도 제어 및 설정 저장 매니저.
    /// SharedPreferences (Unity PlayerPrefs)를 통해 On/Off 설정을 영구 보존합니다.
    /// </summary>
    public class MobileHapticManager : MonoBehaviour
    {
        private const string PrefKey_Haptic = "Setting_HapticEnabled";

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

        public bool IsHapticEnabled { get; private set; } = true;

        public event Action<bool> OnHapticSettingChanged;

#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject _vibrator;
        private bool _hasVibratorEffect = false;
#endif

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                LoadSettings();
                InitPlatformVibrator();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void LoadSettings()
        {
            // 기본값: 켜짐(1)
            IsHapticEnabled = PlayerPrefs.GetInt(PrefKey_Haptic, 1) == 1;
        }

        public void SetHapticEnabled(bool enabled)
        {
            IsHapticEnabled = enabled;
            PlayerPrefs.SetInt(PrefKey_Haptic, enabled ? 1 : 0);
            PlayerPrefs.Save();
            OnHapticSettingChanged?.Invoke(enabled);
            Debug.Log($"[Haptic] 햅틱 설정 변경: {(enabled ? "ON" : "OFF")}");
        }

        public bool ToggleHaptic()
        {
            SetHapticEnabled(!IsHapticEnabled);
            if (IsHapticEnabled)
            {
                Trigger(HapticType.Light);
            }
            return IsHapticEnabled;
        }

        private void InitPlatformVibrator()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    _vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }

                // Android 8.0 (API 26) 이상에서 VibrationEffect 지원 확인
                using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    int sdkInt = version.GetStatic<int>("SDK_INT");
                    _hasVibratorEffect = (sdkInt >= 26);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Haptic] Android Vibrator 초기화 실패: {ex.Message}");
            }
#endif
        }

        /// <summary>
        /// 햅틱 피드백을 트리거합니다.
        /// - Light: 일반 기물 착수 (10ms, 미세한 진폭)
        /// - Medium: 장군 / 멍군 (30ms, 중간 진폭)
        /// </summary>
        public void Trigger(HapticType type)
        {
            if (!IsHapticEnabled) return;

#if UNITY_ANDROID && !UNITY_EDITOR
            VibrateAndroid(type);
#elif UNITY_IOS && !UNITY_EDITOR
            VibrateIOS(type);
#elif UNITY_WEBGL && !UNITY_EDITOR
            VibrateWebGL(type);
#else
            Debug.Log($"[Haptic (Editor)] 피드백 트리거: {type}");
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private void VibrateAndroid(HapticType type)
        {
            try
            {
                if (_vibrator == null)
                {
                    Handheld.Vibrate();
                    return;
                }

                long durationMs;
                int amplitude;

                switch (type)
                {
                    case HapticType.Light:      // 말 둘 때: 아주 약하고 짧게
                        durationMs = 12;
                        amplitude = 35;
                        break;
                    case HapticType.Medium:     // 장군/멍군: 중간 강도
                        durationMs = 32;
                        amplitude = 110;
                        break;
                    case HapticType.Heavy:
                    case HapticType.Warning:
                        durationMs = 45;
                        amplitude = 180;
                        break;
                    case HapticType.Success:
                        durationMs = 35;
                        amplitude = 130;
                        break;
                    case HapticType.Error:
                        durationMs = 50;
                        amplitude = 200;
                        break;
                    default:
                        durationMs = 20;
                        amplitude = 80;
                        break;
                }

                if (_hasVibratorEffect)
                {
                    using (var effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (var effect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", durationMs, amplitude))
                    {
                        _vibrator.Call("vibrate", effect);
                    }
                }
                else
                {
                    _vibrator.Call("vibrate", durationMs);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Haptic] Android 진동 호출 실패: {ex.Message}");
                Handheld.Vibrate();
            }
        }
#endif

#if UNITY_IOS && !UNITY_EDITOR
        private void VibrateIOS(HapticType type)
        {
            try
            {
                if (SystemInfo.supportsVibration)
                {
                    Handheld.Vibrate();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Haptic] iOS 진동 호출 실패: {ex.Message}");
            }
        }
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
        private void VibrateWebGL(HapticType type)
        {
            try
            {
                int durationMs = type switch
                {
                    HapticType.Light => 10,     // 말 둘 때: 아주 짧게
                    HapticType.Medium => 30,    // 장군/멍군: 중간 강도
                    HapticType.Warning => 45,
                    HapticType.Success => 35,
                    HapticType.Error => 55,
                    _ => 20
                };

                Application.ExternalEval($"if (window.navigator && window.navigator.vibrate) {{ window.navigator.vibrate({durationMs}); }}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Haptic] WebGL 진동 호출 실패: {ex.Message}");
            }
        }
#endif
    }
}
