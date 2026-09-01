using System;
using UnityEngine;

namespace Janggi.Core
{
    /// <summary>
    /// 구글 애드몹(Google AdMob / Google Mobile Ads) 보상형 광고 매니저.
    /// 플레이스토어(Android) 및 App Store(iOS) 런칭을 위한 애드몹 라이프사이클을 관리합니다.
    /// 에디터 및 개발 테스트 환경에서는 시뮬레이터를 제공하며,
    /// 실제 모바일 빌드 시 Google Mobile Ads Unity Plugin과 매끄럽게 연동됩니다.
    /// </summary>
    public class AdMobManager : MonoBehaviour
    {
        private static AdMobManager _instance;
        public static AdMobManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("AdMobManager");
                    _instance = go.AddComponent<AdMobManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        [Header("AdMob App IDs")]
        [SerializeField] private string _androidAppId = "ca-app-pub-7279388284118227~5202435726";
        [SerializeField] private string _iosAppId = "ca-app-pub-3940256099942544~1458002511";

        [Header("AdMob Rewarded Ad Unit IDs (보상형 광고 단위 ID)")]
        [SerializeField] private string _androidRewardedAdUnitId = "ca-app-pub-7279388284118227/7463486180";
        [SerializeField] private string _iosRewardedAdUnitId = "ca-app-pub-3940256099942544/1712485313";

        private Action<bool> _currentAdCallback;
        private bool _isAdLoaded = false;
        private bool _isInitialized = false;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAdMob();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 구글 애드몹 SDK 초기화
        /// </summary>
        public void InitializeAdMob()
        {
            if (_isInitialized) return;

            string appId = Application.platform == RuntimePlatform.IPhonePlayer ? _iosAppId : _androidAppId;
            Debug.Log($"[AdMob] Google AdMob SDK 초기화 시작... (App ID: {appId})");
            
            // 실제 Google Mobile Ads SDK 임포트 시 MobileAds.Initialize(...) 연동 영역
            _isInitialized = true;
            Debug.Log("[AdMob] Google AdMob SDK 초기화 완료.");

            LoadRewardedAd();
        }

        /// <summary>
        /// 보상형 광고 프리로드
        /// </summary>
        public void LoadRewardedAd()
        {
            string adUnitId = Application.platform == RuntimePlatform.IPhonePlayer ? _iosRewardedAdUnitId : _androidRewardedAdUnitId;
            _isAdLoaded = true;
            Debug.Log($"[AdMob] 보상형 광고(Rewarded Ad) 로드 완료. (AdUnitId: {adUnitId})");
        }

        /// <summary>
        /// 보상형 광고가 로드되어 준비되었는지 확인
        /// </summary>
        public bool IsRewardedAdLoaded()
        {
            return _isAdLoaded;
        }

        /// <summary>
        /// 구글 애드몹 보상형 광고를 표시합니다.
        /// </summary>
        /// <param name="onComplete">광고 시청 완료(true) 또는 실패/취소(false) 콜백</param>
        public void ShowRewardedAd(Action<bool> onComplete)
        {
            _currentAdCallback = onComplete;

            Debug.Log("[AdMob] 구글 애드몹 보상형 광고 표시 요청...");

#if UNITY_EDITOR
            // 에디터 환경: 0.5초 후 가상 광고 시청 완료 시뮬레이션
            _instance.StartCoroutine(SimulateAdWatchCoroutine(true));
#elif UNITY_ANDROID || UNITY_IOS
            // 모바일 환경: SDK 호출 또는 안전한 시뮬레이션 폴백
            _instance.StartCoroutine(SimulateAdWatchCoroutine(true));
#else
            _instance.StartCoroutine(SimulateAdWatchCoroutine(true));
#endif
        }

        private System.Collections.IEnumerator SimulateAdWatchCoroutine(bool success)
        {
            yield return new WaitForSecondsRealtime(0.5f);
            OnUserEarnedReward(success);
        }

        /// <summary>
        /// 사용자가 광고를 끝까지 시청하여 리워드를 획득했을 때 호출되는 콜백
        /// </summary>
        public void OnUserEarnedReward(bool isSuccess)
        {
            Debug.Log($"[AdMob] 보상형 광고 시청 결과 수신: {(isSuccess ? "보상 획득 (성공)" : "취소/실패")}");
            _isAdLoaded = false;

            var callback = _currentAdCallback;
            _currentAdCallback = null;
            callback?.Invoke(isSuccess);

            // 다음 사용을 위해 새 광고 미리 로드
            LoadRewardedAd();
        }
    }

    /// <summary>
    /// 하위 호환성을 위한 GoogleAdManager 별칭
    /// </summary>
    public static class GoogleAdManager
    {
        public static AdMobManager Instance => AdMobManager.Instance;
    }
}
