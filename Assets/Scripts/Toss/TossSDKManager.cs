using System;
using System.Runtime.InteropServices;
using UnityEngine;

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
    /// 토스(Toss) 인앱 게임 SDK 매니저.
    /// WebGL 환경에서는 .jslib 브릿지를 통해 window.toss와 통신하고,
    /// Unity 에디터에서는 안전하게 Mock 시뮬레이션을 수행합니다.
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

        private Action<bool> _currentAdCallback;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void Toss_TriggerHaptic(string type);

        [DllImport("__Internal")]
        private static extern void Toss_ShareResult(string title, string desc);

        [DllImport("__Internal")]
        private static extern void Toss_ShowRewardedAd(string callbackObj, string callbackMethod);

        [DllImport("__Internal")]
        private static extern void Toss_CloseWebview();
#endif

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
        /// 토스 햅틱 진동 피드백을 트리거합니다.
        /// </summary>
        public void TriggerHaptic(TossHapticType type)
        {
            string typeStr = type.ToString().ToLower();

#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                Toss_TriggerHaptic(typeStr);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TossSDK] 햅틱 호출 실패: {ex.Message}");
            }
#else
            Debug.Log($"[TossSDK (Editor Mock)] 햅틱 피드백 트리거: {typeStr}");
#endif
        }

        /// <summary>
        /// 게임 결과나 초대 링크를 토스 친구에게 공유합니다.
        /// </summary>
        public void ShareGameResult(string title, string desc)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                Toss_ShareResult(title, desc);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TossSDK] 공유 호출 실패: {ex.Message}");
            }
#else
            Debug.Log($"[TossSDK (Editor Mock)] 공유 호출:\n제목: {title}\n내용: {desc}");
#endif
        }

        /// <summary>
        /// 토스 보상형 광고를 표시합니다.
        /// </summary>
        /// <param name="onComplete">광고 시청 완료(true) 또는 취소/실패(false) 콜백</param>
        public void ShowRewardedAd(Action<bool> onComplete)
        {
            _currentAdCallback = onComplete;

#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                Toss_ShowRewardedAd(gameObject.name, nameof(OnRewardedAdFinished));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TossSDK] 광고 호출 실패: {ex.Message}");
                _currentAdCallback?.Invoke(false);
                _currentAdCallback = null;
            }
#else
            Debug.Log("[TossSDK (Editor Mock)] 보상형 광고 시뮬레이션: 1초 후 성공 처리");
            // 에디터에서는 즉시 가상 성공 콜백
            _currentAdCallback?.Invoke(true);
            _currentAdCallback = null;
#endif
        }

        /// <summary>
        /// .jslib에서 SendMessage로 호출되는 광고 완료 콜백
        /// </summary>
        /// <param name="resultStr">"1" 성공, "0" 실패</param>
        public void OnRewardedAdFinished(string resultStr)
        {
            bool isSuccess = resultStr == "1";
            Debug.Log($"[TossSDK] 광고 시청 결과 수신: {(isSuccess ? "성공" : "실패/취소")}");
            _currentAdCallback?.Invoke(isSuccess);
            _currentAdCallback = null;
        }

        /// <summary>
        /// 토스 인앱 웹뷰를 종료하고 토스 앱 메인으로 돌아갑니다.
        /// </summary>
        public void CloseWebview()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                Toss_CloseWebview();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TossSDK] 웹뷰 닫기 실패: {ex.Message}");
            }
#else
            Debug.Log("[TossSDK (Editor Mock)] 토스 웹뷰 닫기 호출됨");
#endif
        }
    }
}
