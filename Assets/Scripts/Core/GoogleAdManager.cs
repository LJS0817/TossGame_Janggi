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
        [SerializeField] private string _androidAppId = "";
        [SerializeField] private string _iosAppId = "";

        [Header("AdMob Rewarded Ad Unit IDs (보상형 광고 단위 ID)")]
        [SerializeField] private string _androidRewardedAdUnitId = "";
        [SerializeField] private string _iosRewardedAdUnitId = "";

        [Header("테스트 모드 (개발/검증 시 체크)")]
        [Tooltip("체크 시 구글 공식 테스트 광고 단위 ID를 사용하여 계정 정지 위험 없이 안전하게 테스트할 수 있습니다.")]
        [SerializeField] private bool _useTestAdUnitId = false;

        // 구글 공식 테스트용 보상형 전면 광고 단위 ID
        private const string AndroidTestRewardedAdUnitId = "ca-app-pub-3940256099942544/5354046379";
        private const string IosTestRewardedAdUnitId = "ca-app-pub-3940256099942544/6978759866";
        private const string AndroidTestAppId = "ca-app-pub-3940256099942544~3347511713";
        private const string IosTestAppId = "ca-app-pub-3940256099942544~1458002511";

#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
        private RewardedInterstitialAd _rewardedInterstitialAd;
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
                LoadSecretsIfAvailable();
                InitializeAdMob();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// .gitignore 처리된 로컬 시크릿 파일(AdMobSecrets.json 또는 AdMobSecretConfig.asset)이 있으면 자동 로드합니다.
        /// 이를 통해 깃허브 공개 저장소에는 상용 키가 노출되지 않고 로컬에서만 안전하게 적용됩니다.
        /// </summary>
        private void LoadSecretsIfAvailable()
        {
            // 1. ScriptableObject 에셋(AdMobSecretConfig) 우선 확인
            var secretConfig = Resources.Load<AdMobConfig>("AdMobSecretConfig");
            if (secretConfig != null)
            {
                ApplySecretConfig(secretConfig.androidAppId, secretConfig.iosAppId,
                    secretConfig.androidRewardedAdUnitId, secretConfig.iosRewardedAdUnitId);
                return;
            }

            // 2. JSON 에셋(AdMobSecrets.json) 확인
            var jsonAsset = Resources.Load<TextAsset>("AdMobSecrets");
            if (jsonAsset != null && !string.IsNullOrEmpty(jsonAsset.text))
            {
                try
                {
                    var data = JsonUtility.FromJson<AdMobSecretData>(jsonAsset.text);
                    if (data != null)
                    {
                        ApplySecretConfig(data.androidAppId, data.iosAppId,
                            data.androidRewardedAdUnitId, data.iosRewardedAdUnitId);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[AdMob] AdMobSecrets.json 파싱 중 예외 발생: {ex.Message}");
                }
            }

            Debug.Log("[AdMob] 로컬 시크릿 키 파일을 찾지 못했습니다. 기본 설정 또는 테스트 ID를 사용합니다.");
        }

        private void ApplySecretConfig(string androidApp, string iosApp, string androidUnit, string iosUnit)
        {
            if (!string.IsNullOrEmpty(androidApp)) _androidAppId = androidApp;
            if (!string.IsNullOrEmpty(iosApp)) _iosAppId = iosApp;
            if (!string.IsNullOrEmpty(androidUnit)) _androidRewardedAdUnitId = androidUnit;
            if (!string.IsNullOrEmpty(iosUnit)) _iosRewardedAdUnitId = iosUnit;
            Debug.Log("[AdMob] 로컬 시크릿 파일에서 애드몹 상용 키를 성공적으로 로드했습니다.");
        }

        /// <summary>
        /// 구글 애드몹 SDK 초기화
        /// </summary>
        public void InitializeAdMob()
        {
            if (_isInitialized) return;

            string appId = Application.platform == RuntimePlatform.IPhonePlayer ? _iosAppId : _androidAppId;
            if (string.IsNullOrEmpty(appId))
            {
                appId = Application.platform == RuntimePlatform.IPhonePlayer ? IosTestAppId : AndroidTestAppId;
            }

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

            string targetId = Application.platform == RuntimePlatform.IPhonePlayer
                ? _iosRewardedAdUnitId
                : _androidRewardedAdUnitId;

            // 상용 광고 단위 ID가 비어있는 경우(예: 오픈소스 클론 환경), 안전하게 공식 테스트 ID로 폴백
            if (string.IsNullOrEmpty(targetId))
            {
                Debug.LogWarning("[AdMob] 상용 광고 단위 ID가 비어있어 구글 공식 테스트 ID로 자동 폴백합니다.");
                return Application.platform == RuntimePlatform.IPhonePlayer
                    ? IosTestRewardedAdUnitId
                    : AndroidTestRewardedAdUnitId;
            }

            return targetId;
        }

        /// <summary>
        /// 보상형 광고를 미리 로드(Preload)합니다.
        /// </summary>
        public void LoadRewardedAd()
        {
#if UNITY_EDITOR
            Debug.Log("[AdMob] [Editor] 보상형 전면 광고 프리로드 완료 (시뮬레이터 모드)");
            return;
#elif UNITY_ANDROID || UNITY_IOS
            if (_isLoadingAd)
            {
                Debug.Log("[AdMob] 이미 보상형 전면 광고를 로드 중입니다.");
                return;
            }

            // 이미 사용 가능한 광고가 있다면 재로드 생략
            if (_rewardedInterstitialAd != null && _rewardedInterstitialAd.CanShowAd())
            {
                Debug.Log("[AdMob] 유효한 보상형 전면 광고가 이미 준비되어 있습니다.");
                return;
            }

            // 이전 광고 객체 정리
            DestroyRewardedAd();

            string adUnitId = GetRewardedAdUnitId();
            Debug.Log($"[AdMob] 보상형 전면 광고 로드 요청 시작 (AdUnitId: {adUnitId})");
            _isLoadingAd = true;

            var adRequest = new AdRequest();
            RewardedInterstitialAd.Load(adUnitId, adRequest, (RewardedInterstitialAd ad, LoadAdError error) =>
            {
                _isLoadingAd = false;

                if (error != null || ad == null)
                {
                    Debug.LogError($"[AdMob] 보상형 전면 광고 로드 실패. Code: {error?.GetCode()}, Message: {error?.GetMessage()}");
                    return;
                }

                _rewardedInterstitialAd = ad;
                Debug.Log("[AdMob] 보상형 전면 광고 로드 성공. 표시 대기 중.");

                RegisterAdEvents(_rewardedInterstitialAd);
            });
#endif
        }

#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
        /// <summary>
        /// 보상형 전면 광고 라이프사이클 이벤트 등록
        /// </summary>
        private void RegisterAdEvents(RewardedInterstitialAd ad)
        {
            ad.OnAdFullScreenContentOpened += () =>
            {
                Debug.Log("[AdMob] 보상형 전면 광고 화면이 열렸습니다.");
            };

            ad.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("[AdMob] 보상형 전면 광고 화면이 닫혔습니다.");
                
                // 광고가 닫힌 시점에 시청 완료 여부 콜백 호출
                OnUserEarnedReward(_rewardEarned);
                _rewardEarned = false;

                // 다음 사용을 위해 새 광고 미리 로드
                LoadRewardedAd();
            };

            ad.OnAdFullScreenContentFailed += (AdError error) =>
            {
                Debug.LogError($"[AdMob] 보상형 전면 광고 표시 실패. Message: {error?.GetMessage()}");
                _rewardEarned = false;
                OnUserEarnedReward(false);

                // 실패 시 새 광고 미리 로드
                LoadRewardedAd();
            };

            ad.OnAdImpressionRecorded += () =>
            {
                Debug.Log("[AdMob] 보상형 전면 광고 노출이 기록되었습니다.");
            };

            ad.OnAdClicked += () =>
            {
                Debug.Log("[AdMob] 보상형 전면 광고를 클릭했습니다.");
            };
        }
#endif

        /// <summary>
        /// 보상형 전면 광고가 로드되어 표시 준비가 되었는지 확인합니다.
        /// </summary>
        public bool IsRewardedAdLoaded()
        {
#if UNITY_EDITOR
            return true;
#elif UNITY_ANDROID || UNITY_IOS
            return _rewardedInterstitialAd != null && _rewardedInterstitialAd.CanShowAd();
#else
            return true;
#endif
        }

        /// <summary>
        /// 구글 애드몹 보상형 전면 광고를 표시합니다.
        /// </summary>
        /// <param name="onComplete">광고 시청 완료(true) 또는 실패/취소(false) 콜백</param>
        public void ShowRewardedAd(Action<bool> onComplete)
        {
            _currentAdCallback = onComplete;
            _rewardEarned = false;

            Debug.Log("[AdMob] 구글 애드몹 보상형 전면 광고 표시 요청...");

#if UNITY_EDITOR
            _showEditorAdModal = true;
            _editorAdStartTime = Time.realtimeSinceStartup;
            Debug.Log("[AdMob] [Editor] 가상 광고 시뮬레이터 UI가 게임 뷰에 표시되었습니다.");
#elif UNITY_ANDROID || UNITY_IOS
            if (_rewardedInterstitialAd != null && _rewardedInterstitialAd.CanShowAd())
            {
                _rewardedInterstitialAd.Show((Reward reward) =>
                {
                    Debug.Log($"[AdMob] 유저 보상 획득 완료! (타입: {reward.Type}, 수량: {reward.Amount})");
                    _rewardEarned = true;
                });
            }
            else
            {
                Debug.LogWarning("[AdMob] 표시 가능한 보상형 전면 광고가 아직 준비되지 않았습니다. 즉시 로드를 재시도합니다.");
                LoadRewardedAd();
                OnUserEarnedReward(false);
            }
#else
            StartCoroutine(SimulateAdWatchCoroutine(true));
#endif
        }

#if UNITY_EDITOR
        private bool _showEditorAdModal = false;
        private float _editorAdStartTime = 0f;
        private const float EditorAdDuration = 3.0f;
        private GUIStyle _modalBoxStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _subStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _cancelButtonStyle;
        private Texture2D _boxBgTexture;

        private void InitEditorGuiStyles()
        {
            if (_modalBoxStyle != null) return;

            _boxBgTexture = new Texture2D(1, 1);
            _boxBgTexture.SetPixel(0, 0, new Color(0.12f, 0.14f, 0.18f, 0.98f));
            _boxBgTexture.Apply();

            _modalBoxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = _boxBgTexture },
                padding = new RectOffset(20, 20, 20, 20)
            };

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.85f, 0.3f) }
            };

            _subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                richText = true,
                normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            _cancelButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                normal = { textColor = new Color(1f, 0.7f, 0.7f) }
            };
        }

        private void OnGUI()
        {
            if (!_showEditorAdModal) return;

            InitEditorGuiStyles();

            // 반투명 어두운 전체화면 오버레이
            Color prevColor = GUI.color;
            GUI.color = new Color(0, 0, 0, 0.85f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = prevColor;

            float boxWidth = Mathf.Min(520, Screen.width * 0.92f);
            float boxHeight = Mathf.Min(360, Screen.height * 0.85f);
            float boxX = (Screen.width - boxWidth) * 0.5f;
            float boxY = (Screen.height - boxHeight) * 0.5f;

            GUILayout.BeginArea(new Rect(boxX, boxY, boxWidth, boxHeight), _modalBoxStyle);
            GUILayout.Space(10);

            GUILayout.Label("🎬 Google AdMob 보상형 전면 광고", _headerStyle);
            GUILayout.Space(4);
            GUILayout.Label("<color=#A0A0A0>[유니티 에디터 시각적 시뮬레이터]</color>", _subStyle);
            GUILayout.Space(12);

            string currentUnitId = GetRewardedAdUnitId();
            GUILayout.Label($"<b>적용된 광고 단위 ID:</b>\n<color=#4FC3F7>{currentUnitId}</color>", _subStyle);
            GUILayout.Space(10);

            float elapsed = Time.realtimeSinceStartup - _editorAdStartTime;
            float remaining = Mathf.Max(0, EditorAdDuration - elapsed);

            if (remaining > 0)
            {
                GUILayout.Label($"모바일 기기에서는 실제 전체 화면 광고가 재생됩니다.\n(가상 재생 시간: <color=#FFD54F>{remaining:F1}초</color>)", _subStyle);
            }
            else
            {
                GUILayout.Label("<color=#81C784>✨ 광고 시청 완료! 보상을 수령할 수 있습니다.</color>", _subStyle);
            }

            GUILayout.FlexibleSpace();

            // 버튼 영역
            GUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.2f, 0.75f, 0.3f);
            if (GUILayout.Button(remaining <= 0 ? "🎁 보상 받고 닫기 (성공)" : "⚡ 즉시 보상 획득 (테스트)", _buttonStyle, GUILayout.Height(46)))
            {
                _showEditorAdModal = false;
                StartCoroutine(DelayedEarnRewardCoroutine(true));
            }

            GUI.backgroundColor = new Color(0.85f, 0.25f, 0.25f);
            if (GUILayout.Button("✖ 닫기 (취소/실패)", _cancelButtonStyle, GUILayout.Height(46), GUILayout.Width(130)))
            {
                _showEditorAdModal = false;
                StartCoroutine(DelayedEarnRewardCoroutine(false));
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.EndArea();
        }

        private IEnumerator DelayedEarnRewardCoroutine(bool isSuccess)
        {
            yield return null; // 모달을 닫는 마우스 클릭 이벤트가 보드에 전달되어 취소되는 것을 방지하기 위해 1프레임 대기
            OnUserEarnedReward(isSuccess);
        }
#endif

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
            Debug.Log($"[AdMob] 보상형 전면 광고 시청 결과 통보: {(isSuccess ? "보상 획득 (성공)" : "취소/실패")}");

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
            if (_rewardedInterstitialAd != null)
            {
                _rewardedInterstitialAd.Destroy();
                _rewardedInterstitialAd = null;
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
