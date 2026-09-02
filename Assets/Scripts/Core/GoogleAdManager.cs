using System;
using System.Collections;
using UnityEngine;
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
using GoogleMobileAds.Api;
#endif

namespace Janggi.Core
{
    /// <summary>
    /// 구글 애드몹(Google AdMob / Google Mobile Ads) 보상형 광고 매니저.
    /// 플레이스토어(Android) 및 App Store(iOS) 런칭을 위한 애드몹 라이프사이클을 관리합니다.
    /// 에디터 및 개발 환경에서는 안전한 시뮬레이터를 제공하며,
    /// 실제 모바일 빌드 시 Google Mobile Ads SDK v11.4.0과 완전히 연동됩니다.
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

        [Header("테스트 모드 (개발/검증 시 체크)")]
        [Tooltip("체크 시 구글 공식 테스트 광고 단위 ID를 사용하여 계정 정지 위험 없이 안전하게 테스트할 수 있습니다.")]
        [SerializeField] private bool _useTestAdUnitId = false;

        // 구글 공식 테스트용 보상형 광고 단위 ID
        private const string AndroidTestRewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917";
        private const string IosTestRewardedAdUnitId = "ca-app-pub-3940256099942544/1712485313";

#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
        private RewardedAd _rewardedAd;
#endif

        private Action<bool> _currentAdCallback;
        private bool _isInitialized = false;
        private bool _isLoadingAd = false;
        private bool _rewardEarned = false;

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

#if UNITY_EDITOR
            _isInitialized = true;
            Debug.Log("[AdMob] [Editor] Google Mobile Ads 가상 초기화 완료.");
            LoadRewardedAd();
#elif UNITY_ANDROID || UNITY_IOS
            MobileAds.Initialize(initStatus =>
            {
                _isInitialized = true;
                Debug.Log("[AdMob] Google Mobile Ads SDK 초기화 완료.");
                LoadRewardedAd();
            });
#else
            _isInitialized = true;
            LoadRewardedAd();
#endif
        }

        /// <summary>
        /// 사용할 보상형 광고 단위 ID를 반환합니다.
        /// </summary>
        public string GetRewardedAdUnitId()
        {
            if (_useTestAdUnitId)
            {
                return Application.platform == RuntimePlatform.IPhonePlayer
                    ? IosTestRewardedAdUnitId
                    : AndroidTestRewardedAdUnitId;
            }

            return Application.platform == RuntimePlatform.IPhonePlayer
                ? _iosRewardedAdUnitId
                : _androidRewardedAdUnitId;
        }

        /// <summary>
        /// 보상형 광고를 미리 로드(Preload)합니다.
        /// </summary>
        public void LoadRewardedAd()
        {
#if UNITY_EDITOR
            Debug.Log("[AdMob] [Editor] 보상형 광고 프리로드 완료 (시뮬레이터 모드)");
            return;
#elif UNITY_ANDROID || UNITY_IOS
            if (_isLoadingAd)
            {
                Debug.Log("[AdMob] 이미 보상형 광고를 로드 중입니다.");
                return;
            }

            // 이미 사용 가능한 광고가 있다면 재로드 생략
            if (_rewardedAd != null && _rewardedAd.CanShowAd())
            {
                Debug.Log("[AdMob] 유효한 보상형 광고가 이미 준비되어 있습니다.");
                return;
            }

            // 이전 광고 객체 정리
            DestroyRewardedAd();

            string adUnitId = GetRewardedAdUnitId();
            Debug.Log($"[AdMob] 보상형 광고 로드 요청 시작 (AdUnitId: {adUnitId})");
            _isLoadingAd = true;

            var adRequest = new AdRequest();
            RewardedAd.Load(adUnitId, adRequest, (RewardedAd ad, LoadAdError error) =>
            {
                _isLoadingAd = false;

                if (error != null || ad == null)
                {
                    Debug.LogError($"[AdMob] 보상형 광고 로드 실패. Code: {error?.GetCode()}, Message: {error?.GetMessage()}");
                    return;
                }

                _rewardedAd = ad;
                Debug.Log("[AdMob] 보상형 광고 로드 성공. 표시 대기 중.");

                RegisterAdEvents(_rewardedAd);
            });
#endif
        }

#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
        /// <summary>
        /// 보상형 광고 라이프사이클 이벤트 등록
        /// </summary>
        private void RegisterAdEvents(RewardedAd ad)
        {
            ad.OnAdFullScreenContentOpened += () =>
            {
                Debug.Log("[AdMob] 보상형 광고 화면이 열렸습니다.");
            };

            ad.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("[AdMob] 보상형 광고 화면이 닫혔습니다.");
                
                // 광고가 닫힌 시점에 시청 완료 여부 콜백 호출
                OnUserEarnedReward(_rewardEarned);
                _rewardEarned = false;

                // 다음 사용을 위해 새 광고 미리 로드
                LoadRewardedAd();
            };

            ad.OnAdFullScreenContentFailed += (AdError error) =>
            {
                Debug.LogError($"[AdMob] 보상형 광고 표시 실패. Message: {error?.GetMessage()}");
                _rewardEarned = false;
                OnUserEarnedReward(false);

                // 실패 시 새 광고 미리 로드
                LoadRewardedAd();
            };

            ad.OnAdImpressionRecorded += () =>
            {
                Debug.Log("[AdMob] 보상형 광고 노출이 기록되었습니다.");
            };

            ad.OnAdClicked += () =>
            {
                Debug.Log("[AdMob] 보상형 광고를 클릭했습니다.");
            };
        }
#endif

        /// <summary>
        /// 보상형 광고가 로드되어 표시 준비가 되었는지 확인합니다.
        /// </summary>
        public bool IsRewardedAdLoaded()
        {
#if UNITY_EDITOR
            return true;
#elif UNITY_ANDROID || UNITY_IOS
            return _rewardedAd != null && _rewardedAd.CanShowAd();
#else
            return true;
#endif
        }

        /// <summary>
        /// 구글 애드몹 보상형 광고를 표시합니다.
        /// </summary>
        /// <param name="onComplete">광고 시청 완료(true) 또는 실패/취소(false) 콜백</param>
        public void ShowRewardedAd(Action<bool> onComplete)
        {
            _currentAdCallback = onComplete;
            _rewardEarned = false;

            Debug.Log("[AdMob] 구글 애드몹 보상형 광고 표시 요청...");

#if UNITY_EDITOR
            // 에디터 환경: 0.5초 후 가상 광고 시청 완료 시뮬레이션
            StartCoroutine(SimulateAdWatchCoroutine(true));
#elif UNITY_ANDROID || UNITY_IOS
            if (_rewardedAd != null && _rewardedAd.CanShowAd())
            {
                _rewardedAd.Show((Reward reward) =>
                {
                    Debug.Log($"[AdMob] 유저 보상 획득 완료! (타입: {reward.Type}, 수량: {reward.Amount})");
                    _rewardEarned = true;
                });
            }
            else
            {
                Debug.LogWarning("[AdMob] 표시 가능한 보상형 광고가 아직 준비되지 않았습니다. 즉시 로드를 재시도합니다.");
                LoadRewardedAd();
                OnUserEarnedReward(false);
            }
#else
            StartCoroutine(SimulateAdWatchCoroutine(true));
#endif
        }

        private IEnumerator SimulateAdWatchCoroutine(bool success)
        {
            yield return new WaitForSecondsRealtime(0.5f);
            OnUserEarnedReward(success);
        }

        /// <summary>
        /// 보상 시청 결과 콜백을 실행합니다.
        /// </summary>
        private void OnUserEarnedReward(bool isSuccess)
        {
            Debug.Log($"[AdMob] 보상형 광고 시청 결과 통보: {(isSuccess ? "보상 획득 (성공)" : "취소/실패")}");

            var callback = _currentAdCallback;
            _currentAdCallback = null;
            callback?.Invoke(isSuccess);
        }

        /// <summary>
        /// 기존 로드된 광고 객체를 안전하게 해제합니다.
        /// </summary>
        private void DestroyRewardedAd()
        {
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
            if (_rewardedAd != null)
            {
                _rewardedAd.Destroy();
                _rewardedAd = null;
            }
#endif
        }

        private void OnDestroy()
        {
            DestroyRewardedAd();
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
