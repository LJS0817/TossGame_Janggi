using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Janggi.Core;
using Janggi.AI;

namespace Janggi.UI
{
    public enum CalloutType
    {
        Check,  // 장군 (將軍)
        Escape  // 멍군 (應將)
    }

    /// <summary>
    /// UI Toolkit 기반 장기판 UI 컨트롤러.
    /// Board 로직, 자원(Cost), 손패(Hand), 소환(Spawn) 구역 렌더링, 다국어 지원 및 이벤트를 총괄합니다.
    /// </summary>
    public class BoardUIController
    {
        private const float BoardAspectRatio = 9f / 10f;
        private const float PieceFontRatio = 0.045f;

        private readonly VisualElement _root;
        private readonly VisualElement _boardArea;
        private readonly VisualElement _boardContainer;
        private readonly VisualElement _gridBackground;
        private readonly VisualElement _intersectionsGrid;
        private readonly VisualElement _calloutBanner;
        private readonly Label _calloutHanja;
        private readonly Label _calloutSubtitle;
        private IVisualElementScheduledItem _hideCalloutSchedule;
        private readonly VisualElement _hanTurnInfo;
        private readonly VisualElement _choTurnInfo;
        private readonly Label _hanTurnLabel;
        private readonly Label _choTurnLabel;
        private readonly Label _hanStatusLabel;
        private readonly Label _choStatusLabel;

        // 플레이어/AI 패널 및 손패 슬롯
        private readonly Label _hanCostLabel;
        private readonly Label _choCostLabel;
        private readonly Label _hanFieldCostLabel;
        private readonly Label _choFieldCostLabel;
        private readonly Label _hanSideLabel;
        private readonly Label _choSideLabel;
        private readonly VisualElement _hanHandContainer;
        private readonly VisualElement _choHandContainer;
        private readonly Button _btnDiscardMode;
        private readonly Button _btnDifficulty;

        // 게임 오버 모달 UI
        private readonly VisualElement _gameOverModal;
        private readonly Label _modalTitle;
        private readonly Label _modalDesc;
        private readonly Label _statLabelDifficulty;
        private readonly Label _statDifficulty;
        private readonly Label _statLabelTurns;
        private readonly Label _statTurns;
        private readonly Label _statLabelCaptures;
        private readonly Label _statCaptures;
        private readonly Label _statLabelSummons;
        private readonly Label _statSummons;
        private readonly Button _btnModalRestart;

        // 교차점 버튼 배열 [col, row]
        private readonly VisualElement[,] _intersectionElements;

        // 현재 게임 상태 참조
        private Board _board;
        private PlayerState _choState;
        private PlayerState _hanState;
        private PlayerSide _currentTurn;
        private bool _isInteractive = true;

        // 마지막 모달 상태 저장 (언어 전환 시 즉시 갱신용)
        private bool _isModalShown = false;
        private bool _lastModalIsWin;
        private bool _lastModalIsDraw;
        private AIDifficulty _lastModalDiff;
        private int _lastModalTurns;
        private int _lastModalCaptures;
        private int _lastModalSummons;

        // 선택 상태
        private Piece _selectedPiece;
        private List<BoardPosition> _highlightedMoves;
        private BoardPosition? _lastMoveFrom;
        private BoardPosition? _lastMoveTo;

        // 화면 컨테이너
        private readonly VisualElement _mainMenuScreen;
        private readonly VisualElement _gamePlayScreen;

        // 메인 메뉴 요소
        private readonly Label _menuTitleSub;
        private readonly Label _menuSubtitle;
        private readonly Label _menuDiffSectionTitle;
        private readonly VisualElement _diffCardEasy;
        private readonly VisualElement _diffCardNormal;
        private readonly VisualElement _diffCardHard;
        private readonly VisualElement _diffCardHell;
        private readonly Label _diffEasyTitle;
        private readonly Label _diffEasyDesc;
        private readonly Label _diffNormalTitle;
        private readonly Label _diffNormalDesc;
        private readonly Label _diffHardTitle;
        private readonly Label _diffHardDesc;
        private readonly Label _diffHellTitle;
        private readonly Label _diffHellDesc;

        private readonly Button _btnStartGame;
        private readonly Button _btnOpenRules;
        private readonly Button _btnLanguageToggle;
        private readonly Button _btnCloseRules;
        private readonly VisualElement _ruleGuideModal;
        private readonly Label _ruleModalTitle;
        private readonly Label _rule1Title;
        private readonly Label _rule1Desc;
        private readonly Label _rule2Title;
        private readonly Label _rule2Desc;
        private readonly Label _rule3Title;
        private readonly Label _rule3Desc;
        private readonly Label _rule4Title;
        private readonly Label _rule4Desc;
        private readonly Label _rule5Title;
        private readonly Label _rule5Desc;

        private AIDifficulty _selectedDifficulty = AIDifficulty.Normal;

        // 인게임 헤더 및 버튼
        private readonly Button _btnHeaderLanguage;
        private readonly Button _btnGotoMenu;
        private readonly Button _btnNewGame;
        private readonly Button _btnModalMenu;
        private readonly Button _btnModalShare;
        private readonly Button _btnModalAd;
        private readonly Button _btnPassTurn;

        // 소환 선택 상태
        private int _selectedHandCardIndex = -1;
        private List<BoardPosition> _highlightedSpawnPositions;
        private bool _isDiscardMode = false;
        private float _cachedBoardWidth = 0f;

        // 이벤트 콜백
        public System.Action<Piece> OnPieceSelected;
        public System.Action<Piece, BoardPosition> OnMoveRequested;
        public System.Action<int, BoardPosition> OnSpawnRequested; // handIndex, spawnPos
        public System.Action<int> OnDiscardRequested;              // handIndex
        public System.Action OnDifficultyToggled;
        public System.Action OnRestartRequested;
        public System.Action<AIDifficulty> OnStartGameRequested;
        public System.Action OnReturnToMenuRequested;
        public System.Action OnPassRequested;
        public System.Action OnShareRequested;
        public System.Action OnAdRequested;

        public BoardUIController(VisualElement root)
        {
            _root = root;

            // 1. 화면 컨테이너 바인딩
            _mainMenuScreen = root.Q<VisualElement>("main-menu-screen");
            _gamePlayScreen = root.Q<VisualElement>("game-play-screen");

            // 2. 메인 메뉴 요소 바인딩
            _menuTitleSub = root.Q<Label>("menu-title-sub");
            _menuSubtitle = root.Q<Label>("menu-subtitle");
            _menuDiffSectionTitle = root.Q<Label>("menu-diff-section-title");

            _diffCardEasy = root.Q<VisualElement>("diff-card-easy");
            _diffCardNormal = root.Q<VisualElement>("diff-card-normal");
            _diffCardHard = root.Q<VisualElement>("diff-card-hard");
            _diffCardHell = root.Q<VisualElement>("diff-card-hell");

            _diffEasyTitle = root.Q<Label>("diff-easy-title");
            _diffEasyDesc = root.Q<Label>("diff-easy-desc");
            _diffNormalTitle = root.Q<Label>("diff-normal-title");
            _diffNormalDesc = root.Q<Label>("diff-normal-desc");
            _diffHardTitle = root.Q<Label>("diff-hard-title");
            _diffHardDesc = root.Q<Label>("diff-hard-desc");
            _diffHellTitle = root.Q<Label>("diff-hell-title");
            _diffHellDesc = root.Q<Label>("diff-hell-desc");

            _btnStartGame = root.Q<Button>("btn-start-game");
            _btnOpenRules = root.Q<Button>("btn-open-rules");
            _btnLanguageToggle = root.Q<Button>("btn-language-toggle");
            _btnCloseRules = root.Q<Button>("btn-close-rules");
            _ruleGuideModal = root.Q<VisualElement>("rule-guide-modal");

            _ruleModalTitle = root.Q<Label>("rule-modal-title");
            _rule1Title = root.Q<Label>("rule-1-title");
            _rule1Desc = root.Q<Label>("rule-1-desc");
            _rule2Title = root.Q<Label>("rule-2-title");
            _rule2Desc = root.Q<Label>("rule-2-desc");
            _rule3Title = root.Q<Label>("rule-3-title");
            _rule3Desc = root.Q<Label>("rule-3-desc");
            _rule4Title = root.Q<Label>("rule-4-title");
            _rule4Desc = root.Q<Label>("rule-4-desc");
            _rule5Title = root.Q<Label>("rule-5-title");
            _rule5Desc = root.Q<Label>("rule-5-desc");

            // 3. 인게임 요소 바인딩
            _boardArea = root.Q<VisualElement>("board-area");
            _boardContainer = root.Q<VisualElement>("board-container");
            _gridBackground = root.Q<VisualElement>("grid-background");
            _intersectionsGrid = root.Q<VisualElement>("intersections-grid");
            _calloutBanner = root.Q<VisualElement>("callout-banner");
            _calloutHanja = root.Q<Label>("callout-hanja");
            _calloutSubtitle = root.Q<Label>("callout-subtitle");

            _hanTurnInfo = root.Q<VisualElement>("han-turn-info");
            _choTurnInfo = root.Q<VisualElement>("cho-turn-info");
            _hanTurnLabel = root.Q<Label>("han-turn-label");
            _choTurnLabel = root.Q<Label>("cho-turn-label");
            _hanStatusLabel = root.Q<Label>("han-status-label");
            _choStatusLabel = root.Q<Label>("cho-status-label");

            _hanSideLabel = root.Q<Label>("han-side-label");
            _choSideLabel = root.Q<Label>("cho-side-label");
            _hanCostLabel = root.Q<Label>("han-cost-label");
            _choCostLabel = root.Q<Label>("cho-cost-label");
            _hanFieldCostLabel = root.Q<Label>("han-field-cost-label");
            _choFieldCostLabel = root.Q<Label>("cho-field-cost-label");
            _hanHandContainer = root.Q<VisualElement>("han-hand-container");
            _choHandContainer = root.Q<VisualElement>("cho-hand-container");
            _btnDiscardMode = root.Q<Button>("btn-discard-mode");
            _btnPassTurn = root.Q<Button>("btn-pass-turn");
            _btnDifficulty = root.Q<Button>("btn-difficulty");
            _btnHeaderLanguage = root.Q<Button>("btn-header-language");
            _btnNewGame = root.Q<Button>("btn-new-game");
            _btnGotoMenu = root.Q<Button>("btn-goto-menu");

            // 4. 모달 팝업 바인딩
            _gameOverModal = root.Q<VisualElement>("game-over-modal");
            _modalTitle = root.Q<Label>("modal-title");
            _modalDesc = root.Q<Label>("modal-desc");
            _statLabelDifficulty = root.Q<Label>("stat-label-difficulty");
            _statDifficulty = root.Q<Label>("stat-difficulty");
            _statLabelTurns = root.Q<Label>("stat-label-turns");
            _statTurns = root.Q<Label>("stat-turns");
            _statLabelCaptures = root.Q<Label>("stat-label-captures");
            _statCaptures = root.Q<Label>("stat-captures");
            _statLabelSummons = root.Q<Label>("stat-label-summons");
            _statSummons = root.Q<Label>("stat-summons");
            _btnModalRestart = root.Q<Button>("btn-modal-restart");
            _btnModalMenu = root.Q<Button>("btn-modal-menu");
            _btnModalShare = root.Q<Button>("btn-modal-share");
            _btnModalAd = root.Q<Button>("btn-modal-ad");

            _intersectionElements = new VisualElement[BoardPosition.MaxCol, BoardPosition.MaxRow];
            _highlightedMoves = new List<BoardPosition>();
            _highlightedSpawnPositions = new List<BoardPosition>();

            BuildGridLines();
            BuildGrid();
            SetupButtons();
            SetupMainMenu();

            // 다국어 초기 텍스트 적용 및 이벤트 구독
            ApplyLocalization();
            LocalizationManager.OnLanguageChanged += ApplyLocalization;
            ApplyDifficultyTheme(_selectedDifficulty);

            // 반응형 보드 크기 계산
            if (_boardArea != null)
            {
                _boardArea.RegisterCallback<GeometryChangedEvent>(OnBoardAreaGeometryChanged);
            }
        }

        private void BuildGridLines()
        {
            if (_gridBackground == null) return;
            _gridBackground.Clear();

            // 1. 가로선 10줄 (Row 0 ~ Row 9 중심)
            for (int row = 0; row < BoardPosition.MaxRow; row++)
            {
                var hLine = new VisualElement();
                hLine.AddToClassList("grid-line-h");
                // row 9(상단) ~ row 0(하단)
                float topPercent = (BoardPosition.MaxRow - 1 - row + 0.5f) * 10f;
                hLine.style.top = Length.Percent(topPercent);
                _gridBackground.Add(hLine);
            }

            // 2. 세로선 9줄 (Col 0 ~ Col 8 중심)
            float colStep = 100f / BoardPosition.MaxCol;
            for (int col = 0; col < BoardPosition.MaxCol; col++)
            {
                var vLine = new VisualElement();
                vLine.AddToClassList("grid-line-v");
                float leftPercent = (col + 0.5f) * colStep;
                vLine.style.left = Length.Percent(leftPercent);
                _gridBackground.Add(vLine);
            }
        }

        private void SetupButtons()
        {
            if (_btnDiscardMode != null)
            {
                _btnDiscardMode.clicked += ToggleDiscardMode;
            }

            if (_btnPassTurn != null)
            {
                _btnPassTurn.clicked += () => OnPassRequested?.Invoke();
            }

            if (_btnDifficulty != null)
            {
                _btnDifficulty.clicked += () => OnDifficultyToggled?.Invoke();
            }

            if (_btnHeaderLanguage != null)
            {
                _btnHeaderLanguage.clicked += () => LocalizationManager.ToggleLanguage();
            }

            if (_btnNewGame != null)
            {
                _btnNewGame.clicked += () => OnRestartRequested?.Invoke();
            }

            if (_btnGotoMenu != null)
            {
                _btnGotoMenu.clicked += () => OnReturnToMenuRequested?.Invoke();
            }

            if (_btnModalRestart != null)
            {
                _btnModalRestart.clicked += () =>
                {
                    HideGameOverModal();
                    OnRestartRequested?.Invoke();
                };
            }

            if (_btnModalMenu != null)
            {
                _btnModalMenu.clicked += () =>
                {
                    HideGameOverModal();
                    OnReturnToMenuRequested?.Invoke();
                };
            }

            if (_btnModalShare != null)
            {
                _btnModalShare.clicked += () => OnShareRequested?.Invoke();
            }

            if (_btnModalAd != null)
            {
                _btnModalAd.clicked += () => OnAdRequested?.Invoke();
            }
        }

        private void SetupMainMenu()
        {
            // 1. 난이도 카드 클릭 이벤트
            _diffCardEasy?.RegisterCallback<ClickEvent>(evt => SelectDifficulty(AIDifficulty.Easy));
            _diffCardNormal?.RegisterCallback<ClickEvent>(evt => SelectDifficulty(AIDifficulty.Normal));
            _diffCardHard?.RegisterCallback<ClickEvent>(evt => SelectDifficulty(AIDifficulty.Hard));
            _diffCardHell?.RegisterCallback<ClickEvent>(evt => SelectDifficulty(AIDifficulty.Hell));

            // 2. 게임 시작 버튼
            if (_btnStartGame != null)
            {
                _btnStartGame.clicked += () =>
                {
                    OnStartGameRequested?.Invoke(_selectedDifficulty);
                };
            }

            // 3. 게임 방법 버튼 & 닫기 버튼
            if (_btnOpenRules != null)
            {
                _btnOpenRules.clicked += () =>
                {
                    if (_ruleGuideModal != null) _ruleGuideModal.style.display = DisplayStyle.Flex;
                };
            }

            if (_btnCloseRules != null)
            {
                _btnCloseRules.clicked += () =>
                {
                    if (_ruleGuideModal != null) _ruleGuideModal.style.display = DisplayStyle.None;
                };
            }

            // 4. 언어 전환 버튼
            if (_btnLanguageToggle != null)
            {
                _btnLanguageToggle.clicked += () =>
                {
                    LocalizationManager.ToggleLanguage();
                };
            }

            // 초기 난이도 선택 상태 동기화
            SelectDifficulty(_selectedDifficulty);
        }

        /// <summary>
        /// BoardUIController 리소스 및 이벤트 구독을 해제합니다.
        /// </summary>
        public void Dispose()
        {
            LocalizationManager.OnLanguageChanged -= ApplyLocalization;

            if (_boardArea != null)
            {
                _boardArea.UnregisterCallback<GeometryChangedEvent>(OnBoardAreaGeometryChanged);
            }
        }

        /// <summary>
        /// 언어 설정 변경 시 모든 정적 및 동적 UI 텍스트를 즉시 갱신합니다.
        /// </summary>
        public void ApplyLocalization()
        {
            try
            {
                // 1. 메인 메뉴
                if (_menuTitleSub != null && _menuTitleSub.panel != null) _menuTitleSub.text = LocalizationManager.Get("Menu_Title_Sub");
                if (_menuSubtitle != null && _menuSubtitle.panel != null) _menuSubtitle.text = LocalizationManager.Get("Menu_Subtitle");
                if (_menuDiffSectionTitle != null && _menuDiffSectionTitle.panel != null) _menuDiffSectionTitle.text = LocalizationManager.Get("Menu_Diff_Section");

                if (_diffEasyTitle != null && _diffEasyTitle.panel != null) _diffEasyTitle.text = LocalizationManager.Get("Diff_Easy_Title");
                if (_diffEasyDesc != null && _diffEasyDesc.panel != null) _diffEasyDesc.text = LocalizationManager.Get("Diff_Easy_Desc");
                if (_diffNormalTitle != null && _diffNormalTitle.panel != null) _diffNormalTitle.text = LocalizationManager.Get("Diff_Normal_Title");
                if (_diffNormalDesc != null && _diffNormalDesc.panel != null) _diffNormalDesc.text = LocalizationManager.Get("Diff_Normal_Desc");
                if (_diffHardTitle != null && _diffHardTitle.panel != null) _diffHardTitle.text = LocalizationManager.Get("Diff_Hard_Title");
                if (_diffHardDesc != null && _diffHardDesc.panel != null) _diffHardDesc.text = LocalizationManager.Get("Diff_Hard_Desc");
                if (_diffHellTitle != null && _diffHellTitle.panel != null) _diffHellTitle.text = LocalizationManager.Get("Diff_Hell_Title");
                if (_diffHellDesc != null && _diffHellDesc.panel != null) _diffHellDesc.text = LocalizationManager.Get("Diff_Hell_Desc");

                if (_btnStartGame != null && _btnStartGame.panel != null) _btnStartGame.text = LocalizationManager.Get("Btn_Start_Game");
                if (_btnOpenRules != null && _btnOpenRules.panel != null) _btnOpenRules.text = LocalizationManager.Get("Btn_Open_Rules");
                if (_btnLanguageToggle != null && _btnLanguageToggle.panel != null) _btnLanguageToggle.text = LocalizationManager.GetLanguageToggleLabel();
                if (_btnCloseRules != null && _btnCloseRules.panel != null) _btnCloseRules.text = LocalizationManager.Get("Btn_Close_Rules");

                // 2. 규칙 모달
                if (_ruleModalTitle != null && _ruleModalTitle.panel != null) _ruleModalTitle.text = LocalizationManager.Get("Rule_Modal_Title");
                if (_rule1Title != null && _rule1Title.panel != null) _rule1Title.text = LocalizationManager.Get("Rule_1_Title");
                if (_rule1Desc != null && _rule1Desc.panel != null) _rule1Desc.text = LocalizationManager.Get("Rule_1_Desc");
                if (_rule2Title != null && _rule2Title.panel != null) _rule2Title.text = LocalizationManager.Get("Rule_2_Title");
                if (_rule2Desc != null && _rule2Desc.panel != null) _rule2Desc.text = LocalizationManager.Get("Rule_2_Desc");
                if (_rule3Title != null && _rule3Title.panel != null) _rule3Title.text = LocalizationManager.Get("Rule_3_Title");
                if (_rule3Desc != null && _rule3Desc.panel != null) _rule3Desc.text = LocalizationManager.Get("Rule_3_Desc");
                if (_rule4Title != null && _rule4Title.panel != null) _rule4Title.text = LocalizationManager.Get("Rule_4_Title");
                if (_rule4Desc != null && _rule4Desc.panel != null) _rule4Desc.text = LocalizationManager.Get("Rule_4_Desc");
                if (_rule5Title != null && _rule5Title.panel != null) _rule5Title.text = LocalizationManager.Get("Rule_5_Title");
                if (_rule5Desc != null && _rule5Desc.panel != null) _rule5Desc.text = LocalizationManager.Get("Rule_5_Desc");

                // 3. 인게임 헤더 및 패널
                if (_btnHeaderLanguage != null && _btnHeaderLanguage.panel != null) _btnHeaderLanguage.text = LocalizationManager.GetHeaderLanguageToggleLabel();
                if (_btnNewGame != null && _btnNewGame.panel != null) _btnNewGame.text = LocalizationManager.Get("Btn_Restart");
                if (_btnGotoMenu != null && _btnGotoMenu.panel != null) _btnGotoMenu.text = LocalizationManager.Get("Btn_Menu");
                if (_btnPassTurn != null && _btnPassTurn.panel != null) _btnPassTurn.text = LocalizationManager.Get("Btn_Pass");

                if (_hanSideLabel != null && _hanSideLabel.panel != null) _hanSideLabel.text = LocalizationManager.Get("Side_Han");
                if (_choSideLabel != null && _choSideLabel.panel != null) _choSideLabel.text = LocalizationManager.Get("Side_Cho");

                UpdateDifficultyDisplay(_selectedDifficulty);
                UpdateDiscardButtonUI();

                // 4. 모달 라벨들
                if (_statLabelDifficulty != null && _statLabelDifficulty.panel != null) _statLabelDifficulty.text = LocalizationManager.Get("Stat_Difficulty");
                if (_statLabelTurns != null && _statLabelTurns.panel != null) _statLabelTurns.text = LocalizationManager.Get("Stat_Turns");
                if (_statLabelCaptures != null && _statLabelCaptures.panel != null) _statLabelCaptures.text = LocalizationManager.Get("Stat_Captures");
                if (_statLabelSummons != null && _statLabelSummons.panel != null) _statLabelSummons.text = LocalizationManager.Get("Stat_Summons");

                if (_btnModalShare != null && _btnModalShare.panel != null) _btnModalShare.text = LocalizationManager.Get("Btn_Modal_Share");
                if (_btnModalAd != null && _btnModalAd.panel != null) _btnModalAd.text = LocalizationManager.Get("Btn_Modal_Ad");
                if (_btnModalRestart != null && _btnModalRestart.panel != null) _btnModalRestart.text = LocalizationManager.Get("Btn_Modal_Restart");
                if (_btnModalMenu != null && _btnModalMenu.panel != null) _btnModalMenu.text = LocalizationManager.Get("Btn_Modal_Menu");

                if (_isModalShown)
                {
                    ShowGameOverModal(_lastModalIsWin, _lastModalIsDraw, _lastModalDiff, _lastModalTurns, _lastModalCaptures, _lastModalSummons);
                }

                // 5. 인게임 동적 UI 갱신
                RefreshAll();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BoardUIController] ApplyLocalization 중 예외 무시: {ex.Message}");
            }
        }

        public void SelectDifficulty(AIDifficulty difficulty)
        {
            _selectedDifficulty = difficulty;

            _diffCardEasy?.parent?.RemoveFromClassList("diff-card-wrapper--selected");
            _diffCardNormal?.parent?.RemoveFromClassList("diff-card-wrapper--selected");
            _diffCardHard?.parent?.RemoveFromClassList("diff-card-wrapper--selected");
            _diffCardHell?.parent?.RemoveFromClassList("diff-card-wrapper--selected");

            VisualElement selectedCard = difficulty switch
            {
                AIDifficulty.Easy => _diffCardEasy,
                AIDifficulty.Normal => _diffCardNormal,
                AIDifficulty.Hard => _diffCardHard,
                AIDifficulty.Hell => _diffCardHell,
                _ => null
            };

            selectedCard?.parent?.AddToClassList("diff-card-wrapper--selected");

            UpdateDifficultyDisplay(difficulty);
        }

        public void ShowMainMenu()
        {
            if (_mainMenuScreen != null) _mainMenuScreen.style.display = DisplayStyle.Flex;
            if (_gamePlayScreen != null) _gamePlayScreen.style.display = DisplayStyle.None;
            if (_ruleGuideModal != null) _ruleGuideModal.style.display = DisplayStyle.None;
            HideGameOverModal();
        }

        public void ShowGamePlay()
        {
            if (_mainMenuScreen != null) _mainMenuScreen.style.display = DisplayStyle.None;
            if (_gamePlayScreen != null) _gamePlayScreen.style.display = DisplayStyle.Flex;
        }

        public void ShowGameOverModal(bool isWin, bool isDraw, AIDifficulty difficulty, int turnCount, int captures, int summons)
        {
            _isModalShown = true;
            _lastModalIsWin = isWin;
            _lastModalIsDraw = isDraw;
            _lastModalDiff = difficulty;
            _lastModalTurns = turnCount;
            _lastModalCaptures = captures;
            _lastModalSummons = summons;

            if (_gameOverModal == null) return;

            if (_modalTitle != null)
            {
                _modalTitle.RemoveFromClassList("modal-title--win");
                _modalTitle.RemoveFromClassList("modal-title--lose");
                _modalTitle.RemoveFromClassList("modal-title--draw");

                if (isDraw)
                {
                    _modalTitle.text = LocalizationManager.Get("Modal_Title_Draw");
                    _modalTitle.AddToClassList("modal-title--draw");
                    if (_modalDesc != null) _modalDesc.text = LocalizationManager.Get("Modal_Desc_Draw");
                }
                else if (isWin)
                {
                    _modalTitle.text = LocalizationManager.Get("Modal_Title_Win");
                    _modalTitle.AddToClassList("modal-title--win");
                    if (_modalDesc != null) _modalDesc.text = LocalizationManager.Get("Modal_Desc_Win");
                }
                else
                {
                    _modalTitle.text = LocalizationManager.Get("Modal_Title_Lose");
                    _modalTitle.AddToClassList("modal-title--lose");
                    if (_modalDesc != null) _modalDesc.text = LocalizationManager.Get("Modal_Desc_Lose");
                }
            }

            if (_statDifficulty != null) _statDifficulty.text = difficulty.GetDisplayName();
            if (_statTurns != null) _statTurns.text = LocalizationManager.Get("Stat_Turns_Value", turnCount);
            if (_statCaptures != null) _statCaptures.text = LocalizationManager.Get("Stat_Captures_Value", captures);
            if (_statSummons != null) _statSummons.text = LocalizationManager.Get("Stat_Summons_Value", summons);

            _gameOverModal.style.display = DisplayStyle.Flex;
        }

        public void HideGameOverModal()
        {
            _isModalShown = false;
            if (_gameOverModal != null)
            {
                _gameOverModal.style.display = DisplayStyle.None;
            }
        }

        public void UpdateDifficultyDisplay(AIDifficulty difficulty)
        {
            if (_btnDifficulty != null)
            {
                _btnDifficulty.text = $"{LocalizationManager.Get("Header_Diff_Prefix")}{difficulty.GetDisplayName()}";
            }
            ApplyDifficultyTheme(difficulty);
        }

        public void ApplyDifficultyTheme(AIDifficulty difficulty)
        {
            if (_root == null) return;

            _root.RemoveFromClassList("theme-diff-easy");
            _root.RemoveFromClassList("theme-diff-normal");
            _root.RemoveFromClassList("theme-diff-hard");
            _root.RemoveFromClassList("theme-diff-hell");

            switch (difficulty)
            {
                case AIDifficulty.Easy:
                    _root.AddToClassList("theme-diff-easy");
                    break;
                case AIDifficulty.Normal:
                    _root.AddToClassList("theme-diff-normal");
                    break;
                case AIDifficulty.Hard:
                    _root.AddToClassList("theme-diff-hard");
                    break;
                case AIDifficulty.Hell:
                    _root.AddToClassList("theme-diff-hell");
                    break;
            }
        }

        public void SetInteractive(bool active)
        {
            _isInteractive = active;
        }

        private void ToggleDiscardMode()
        {
            _isDiscardMode = !_isDiscardMode;
            UpdateDiscardButtonUI();
            ClearSelection();
            RefreshPlayerPanels();
        }

        public void SetDiscardMode(bool active)
        {
            _isDiscardMode = active;
            UpdateDiscardButtonUI();
            RefreshPlayerPanels();
        }

        private void UpdateDiscardButtonUI()
        {
            if (_btnDiscardMode == null) return;
            if (_isDiscardMode)
            {
                _btnDiscardMode.AddToClassList("discard-btn--active");
                _btnDiscardMode.text = LocalizationManager.Get("Btn_Discard_Active");
            }
            else
            {
                _btnDiscardMode.RemoveFromClassList("discard-btn--active");
                _btnDiscardMode.text = LocalizationManager.Get("Btn_Discard_Default");
            }
        }

        // ──────────────────────────────────────────────
        // 반응형 레이아웃
        // ──────────────────────────────────────────────

        private void OnBoardAreaGeometryChanged(GeometryChangedEvent evt)
        {
            var areaRect = _boardArea.contentRect;
            if (areaRect.width <= 0 || areaRect.height <= 0) return;

            float availableWidth = areaRect.width;
            float availableHeight = areaRect.height;

            float boardWidth, boardHeight;
            if (availableWidth / availableHeight < BoardAspectRatio)
            {
                boardWidth = availableWidth;
                boardHeight = boardWidth / BoardAspectRatio;
            }
            else
            {
                boardHeight = availableHeight;
                boardWidth = boardHeight * BoardAspectRatio;
            }

            _cachedBoardWidth = boardWidth;
            _boardContainer.style.width = boardWidth;
            _boardContainer.style.height = boardHeight;
            _boardContainer.style.flexGrow = 0;
            _boardContainer.style.flexShrink = 0;

            UpdatePieceFontSizes(boardWidth);
        }

        private void UpdatePieceFontSizes(float boardWidth)
        {
            if (boardWidth <= 0) return;
            _cachedBoardWidth = boardWidth;
            int fontSize = Mathf.Max(12, Mathf.RoundToInt(boardWidth * PieceFontRatio));

            for (int col = 0; col < BoardPosition.MaxCol; col++)
            {
                for (int row = 0; row < BoardPosition.MaxRow; row++)
                {
                    var element = _intersectionElements[col, row];
                    var pieceLabel = element.Q<Label>(className: "piece");
                    if (pieceLabel != null)
                    {
                        pieceLabel.style.fontSize = fontSize;
                    }
                }
            }
        }

        // ──────────────────────────────────────────────
        // 그리드 구축
        // ──────────────────────────────────────────────

        private void BuildGrid()
        {
            _intersectionsGrid.Clear();

            // Row 9(한) ~ Row 0(초)
            for (int row = BoardPosition.MaxRow - 1; row >= 0; row--)
            {
                var rowContainer = new VisualElement();
                rowContainer.AddToClassList("board-row");

                for (int col = 0; col < BoardPosition.MaxCol; col++)
                {
                    var intersection = CreateIntersection(col, row);
                    rowContainer.Add(intersection);
                    _intersectionElements[col, row] = intersection;
                }

                _intersectionsGrid.Add(rowContainer);
            }
        }

        private VisualElement CreateIntersection(int col, int row)
        {
            var element = new VisualElement();
            element.AddToClassList("intersection");
            element.name = $"cell-{col}-{row}";

            var pos = new BoardPosition(col, row);
            if (pos.IsInsideAnyPalace())
            {
                element.AddToClassList("intersection--palace");
            }

            element.RegisterCallback<ClickEvent>(evt =>
            {
                OnIntersectionClicked(col, row);
            });

            return element;
        }

        // ──────────────────────────────────────────────
        // UI 갱신 (전체)
        // ──────────────────────────────────────────────

        public void SetGameState(Board board, PlayerState choState, PlayerState hanState, PlayerSide currentTurn)
        {
            _board = board;
            _choState = choState;
            _hanState = hanState;
            _currentTurn = currentTurn;
            RefreshAll();
        }

        public void RefreshAll()
        {
            RefreshBoardPieces();
            RefreshPlayerPanels();
            UpdateTurnDisplay(_currentTurn);
            ShowCheckWarning();
        }

        public void RefreshBoardPieces()
        {
            if (_board == null) return;

            for (int col = 0; col < BoardPosition.MaxCol; col++)
            {
                for (int row = 0; row < BoardPosition.MaxRow; row++)
                {
                    var element = _intersectionElements[col, row];
                    ClearIntersection(element);

                    var pos = new BoardPosition(col, row);
                    var piece = _board.GetPieceAt(pos);

                    if (piece != null)
                    {
                        RenderPiece(element, piece);
                    }
                }
            }

            // 직전 수의 이전 위치만 노란색 하이라이트 표시
            if (_lastMoveFrom.HasValue)
            {
                var fromEl = _intersectionElements[_lastMoveFrom.Value.Col, _lastMoveFrom.Value.Row];
                fromEl.AddToClassList("intersection--last-from");
            }

            // 보드 크기가 이미 계산되어 있으면 즉시 동기적으로 폰트 크기 보정
            float currentWidth = _cachedBoardWidth > 0 ? _cachedBoardWidth : _boardContainer.resolvedStyle.width;
            if (currentWidth <= 0 && _boardArea != null)
            {
                currentWidth = _boardArea.resolvedStyle.width;
            }

            if (currentWidth > 0)
            {
                UpdatePieceFontSizes(currentWidth);
            }
            else
            {
                _boardContainer.RegisterCallbackOnce<GeometryChangedEvent>(evt =>
                {
                    UpdatePieceFontSizes(evt.newRect.width);
                });
            }
        }

        private void ClearIntersection(VisualElement element)
        {
            var existingPiece = element.Q<Label>(className: "piece");
            if (existingPiece != null)
                element.Remove(existingPiece);

            var existingShadow = element.Q<VisualElement>(className: "piece-shadow");
            if (existingShadow != null)
                element.Remove(existingShadow);

            element.RemoveFromClassList("intersection--selected");
            element.RemoveFromClassList("intersection--movable");
            element.RemoveFromClassList("intersection--attackable");
            element.RemoveFromClassList("intersection--spawnable");
            element.RemoveFromClassList("intersection--check");
            element.RemoveFromClassList("intersection--last-from");
            element.RemoveFromClassList("intersection--last-to");
        }

        private void RenderPiece(VisualElement intersection, Piece piece)
        {
            // 1. 기물 뒤에 깔리는 전용 솔리드 섀도우 엘리먼트
            var shadow = new VisualElement();
            shadow.AddToClassList("piece-shadow");
            shadow.AddToClassList(piece.Side == PlayerSide.Cho ? "piece-shadow-cho" : "piece-shadow-han");
            intersection.Add(shadow);

            // 2. 기물 본체 라벨 (완벽한 정원형)
            var pieceLabel = new Label();
            pieceLabel.AddToClassList("piece");
            pieceLabel.AddToClassList(piece.Side == PlayerSide.Cho ? "piece-cho" : "piece-han");
            pieceLabel.text = piece.GetDisplayName();

            // 캐시된 폰트 크기가 있으면 생성 즉시 설정
            if (_cachedBoardWidth > 0)
            {
                int fontSize = Mathf.Max(12, Mathf.RoundToInt(_cachedBoardWidth * PieceFontRatio));
                pieceLabel.style.fontSize = fontSize;
            }

            intersection.Add(pieceLabel);
        }

        // ──────────────────────────────────────────────
        // 손패 & 코스트 렌더링
        // ──────────────────────────────────────────────

        public void RefreshPlayerPanels()
        {
            if (_choState != null)
            {
                if (_choCostLabel != null)
                    _choCostLabel.text = LocalizationManager.Get("Cost_Format", _choState.CurrentCost, PlayerState.MaxCost);
                if (_choFieldCostLabel != null)
                    _choFieldCostLabel.text = LocalizationManager.Get("Power_Format", _board?.GetTotalPieceCost(PlayerSide.Cho) ?? 0, PlayerState.MaxFieldCost);
                RenderHand(_choHandContainer, _choState, PlayerSide.Cho, isInteractive: _currentTurn == PlayerSide.Cho);
            }

            if (_hanState != null)
            {
                if (_hanCostLabel != null)
                    _hanCostLabel.text = LocalizationManager.Get("Cost_Format", _hanState.CurrentCost, PlayerState.MaxCost);
                if (_hanFieldCostLabel != null)
                    _hanFieldCostLabel.text = LocalizationManager.Get("Power_Format", _board?.GetTotalPieceCost(PlayerSide.Han) ?? 0, PlayerState.MaxFieldCost);
                RenderHand(_hanHandContainer, _hanState, PlayerSide.Han, isInteractive: false);
            }
        }

        private void RenderHand(VisualElement container, PlayerState state, PlayerSide side, bool isInteractive)
        {
            if (container == null || state == null) return;
            container.Clear();

            for (int i = 0; i < state.Hand.Count; i++)
            {
                int index = i;
                var pieceType = state.Hand[index];
                int cost = pieceType.GetCost();

                var cardWrapper = new VisualElement();
                cardWrapper.AddToClassList("hand-card-wrapper");

                // 카드 뒤에 깔리는 전용 솔리드 섀도우
                var cardShadow = new VisualElement();
                cardShadow.AddToClassList("hand-card-shadow");
                cardShadow.AddToClassList(side == PlayerSide.Cho ? "hand-card-shadow-cho" : "hand-card-shadow-han");
                cardWrapper.Add(cardShadow);

                var card = new VisualElement();
                card.AddToClassList("hand-card");

                // 카드 상태 스타일링
                if (_selectedHandCardIndex == index && side == PlayerSide.Cho)
                {
                    card.AddToClassList("hand-card--selected");
                    cardShadow.AddToClassList("hand-card-shadow--selected");
                }

                if (_isDiscardMode && side == PlayerSide.Cho)
                {
                    card.AddToClassList("hand-card--discard-target");
                    cardShadow.AddToClassList("hand-card-shadow--discard");
                }

                bool isUnusable = side == PlayerSide.Cho && !state.CanSummon(_board, pieceType) && !_isDiscardMode;
                if (isUnusable)
                {
                    card.AddToClassList("hand-card--unusable");
                }

                // 코스트 배지
                var costBadge = new VisualElement();
                costBadge.AddToClassList("card-cost-badge");
                var costText = new Label(cost.ToString());
                costText.AddToClassList("card-cost-text");
                costBadge.Add(costText);
                card.Add(costBadge);

                // 기물 한자 라벨
                var pieceLabel = new Label(pieceType.GetKoreanName(side));
                pieceLabel.AddToClassList("card-piece-label");
                pieceLabel.AddToClassList(side == PlayerSide.Cho ? "card-piece-cho" : "card-piece-han");
                card.Add(pieceLabel);

                // 기물 로컬라이즈된 이름 라벨 (한국어: 졸/마/상/포/차, 영어: Pawn/Horse/Elephant/Cannon/Chariot)
                var nameLabel = new Label(LocalizationManager.GetPieceName(pieceType, side));
                nameLabel.AddToClassList("card-name-label");
                card.Add(nameLabel);

                // 소환 불가 시 카드 표면에만 딤드 오버레이 적용 (뒤 섀도우 박스 투과 방지)
                if (isUnusable)
                {
                    var disabledOverlay = new VisualElement();
                    disabledOverlay.AddToClassList("hand-card-disabled-overlay");
                    card.Add(disabledOverlay);
                }

                // 클릭 이벤트 바인딩 (플레이어 손패만 인터랙션 가능)
                if (isInteractive)
                {
                    card.RegisterCallback<ClickEvent>(evt =>
                    {
                        OnHandCardClicked(index);
                    });
                }

                cardWrapper.Add(card);
                container.Add(cardWrapper);
            }
        }

        private void OnHandCardClicked(int handIndex)
        {
            if (!_isInteractive) return;
            if (_choState == null || _currentTurn != PlayerSide.Cho) return;

            // 1. 패 버리기 모드인 경우
            if (_isDiscardMode)
            {
                OnDiscardRequested?.Invoke(handIndex);
                return;
            }

            // 2. 이미 선택된 카드를 다시 클릭하면 선택 취소
            if (_selectedHandCardIndex == handIndex)
            {
                ClearSelection();
                return;
            }

            // 3. 소환 가능 여부 확인 (코스트 및 필드 전력 20 상한 검증)
            var pieceType = _choState.Hand[handIndex];
            if (!_choState.CanSummon(_board, pieceType))
            {
                if (_choState.HasSummonedThisTurn)
                    ShowStatus(LocalizationManager.Get("Msg_Already_Summoned"));
                else if (_board != null && _board.GetTotalPieceCost(PlayerSide.Cho) + pieceType.GetCost() > PlayerState.MaxFieldCost)
                    ShowStatus(LocalizationManager.Get("Msg_Power_Limit_Exceeded", PlayerState.MaxFieldCost));
                else
                    ShowStatus(LocalizationManager.Get("Msg_Cost_Insufficient"));
                return;
            }

            // 4. 소환 구역 하이라이트
            SelectHandCard(handIndex);
        }

        public void SelectHandCard(int handIndex)
        {
            ClearSelection();

            _selectedHandCardIndex = handIndex;
            var pieceType = _choState.Hand[handIndex];
            _highlightedSpawnPositions = SpawnRuleValidator.GetSpawnablePositions(_board, PlayerSide.Cho, pieceType);

            foreach (var pos in _highlightedSpawnPositions)
            {
                var cell = _intersectionElements[pos.Col, pos.Row];
                cell.AddToClassList("intersection--spawnable");
            }

            RefreshPlayerPanels();
            ShowStatus(LocalizationManager.Get("Msg_Select_Spawn_Cell", LocalizationManager.GetPieceName(pieceType, PlayerSide.Cho)));
        }

        // ──────────────────────────────────────────────
        // 기물 선택 / 하이라이트
        // ──────────────────────────────────────────────

        public void SelectPiece(Piece piece, List<BoardPosition> legalMoves)
        {
            ClearSelection();

            _selectedPiece = piece;
            _highlightedMoves = legalMoves ?? new List<BoardPosition>();

            var selectedEl = _intersectionElements[piece.Position.Col, piece.Position.Row];
            selectedEl.AddToClassList("intersection--selected");

            foreach (var move in _highlightedMoves)
            {
                var moveEl = _intersectionElements[move.Col, move.Row];
                var targetPiece = _board.GetPieceAt(move);

                if (targetPiece != null && targetPiece.Side != piece.Side)
                    moveEl.AddToClassList("intersection--attackable");
                else
                    moveEl.AddToClassList("intersection--movable");
            }
        }

        public void ClearSelection()
        {
            // 기물 선택 해제
            if (_selectedPiece != null)
            {
                var selectedEl = _intersectionElements[_selectedPiece.Position.Col, _selectedPiece.Position.Row];
                selectedEl.RemoveFromClassList("intersection--selected");
            }

            foreach (var move in _highlightedMoves)
            {
                var moveEl = _intersectionElements[move.Col, move.Row];
                moveEl.RemoveFromClassList("intersection--movable");
                moveEl.RemoveFromClassList("intersection--attackable");
            }

            // 소환 구역 하이라이트 해제
            foreach (var pos in _highlightedSpawnPositions)
            {
                var cell = _intersectionElements[pos.Col, pos.Row];
                cell.RemoveFromClassList("intersection--spawnable");
            }

            _selectedPiece = null;
            _highlightedMoves.Clear();
            _selectedHandCardIndex = -1;
            _highlightedSpawnPositions.Clear();
        }

        // ──────────────────────────────────────────────
        // 클릭 핸들러 (보드 교차점)
        // ──────────────────────────────────────────────

        private void OnIntersectionClicked(int col, int row)
        {
            if (!_isInteractive || _board == null) return;
            var clickedPos = new BoardPosition(col, row);

            // A. 소환 카드 선택 상태에서 소환 가능 구역을 클릭한 경우
            if (_selectedHandCardIndex >= 0 && _highlightedSpawnPositions.Contains(clickedPos))
            {
                OnSpawnRequested?.Invoke(_selectedHandCardIndex, clickedPos);
                return;
            }

            // B. 기물 선택 상태에서 이동 가능 구역을 클릭한 경우
            if (_selectedPiece != null && _highlightedMoves.Contains(clickedPos))
            {
                OnMoveRequested?.Invoke(_selectedPiece, clickedPos);
                return;
            }

            // C. 기물 클릭 (새로운 기물 선택)
            var clickedPiece = _board.GetPieceAt(clickedPos);
            if (clickedPiece != null)
            {
                if (_selectedPiece == clickedPiece)
                {
                    ClearSelection();
                    return;
                }

                OnPieceSelected?.Invoke(clickedPiece);
                return;
            }

            // D. 빈 칸 클릭 -> 선택 해제
            ClearSelection();
            RefreshPlayerPanels();
        }

        // ──────────────────────────────────────────────
        // 정보 표시 유틸리티
        // ──────────────────────────────────────────────

        public void UpdateTurnDisplay(PlayerSide currentTurn)
        {
            _currentTurn = currentTurn;

            if (currentTurn == PlayerSide.Cho)
            {
                // 플레이어(楚) 턴: 하단(2번째) 턴 텍스트 노출, 상단(1번째) 숨김 (높이는 상시 유지하여 보드 위치 고정)
                if (_choTurnInfo != null)
                {
                    _choTurnInfo.style.display = DisplayStyle.Flex;
                    _choTurnInfo.style.visibility = Visibility.Visible;
                }
                if (_hanTurnInfo != null)
                {
                    _hanTurnInfo.style.display = DisplayStyle.Flex;
                    _hanTurnInfo.style.visibility = Visibility.Hidden;
                }

                if (_choTurnLabel != null) _choTurnLabel.text = LocalizationManager.Get("Turn_Cho");
            }
            else
            {
                // AI(漢) 턴: 상단(1번째) 턴 텍스트 노출, 하단(2번째) 숨김 (높이는 상시 유지하여 보드 위치 고정)
                if (_hanTurnInfo != null)
                {
                    _hanTurnInfo.style.display = DisplayStyle.Flex;
                    _hanTurnInfo.style.visibility = Visibility.Visible;
                }
                if (_choTurnInfo != null)
                {
                    _choTurnInfo.style.display = DisplayStyle.Flex;
                    _choTurnInfo.style.visibility = Visibility.Hidden;
                }

                if (_hanTurnLabel != null) _hanTurnLabel.text = LocalizationManager.Get("Turn_Han");
            }
        }

        public void ShowStatus(string message)
        {
            if (_choStatusLabel != null) _choStatusLabel.text = message;
            if (_hanStatusLabel != null) _hanStatusLabel.text = message;
        }

        private void ShowCheckWarning()
        {
            if (_board == null) return;

            foreach (var side in new[] { PlayerSide.Cho, PlayerSide.Han })
            {
                var king = _board.FindKing(side);
                if (king != null && GameRuleValidator.IsInCheck(_board, side))
                {
                    var kingEl = _intersectionElements[king.Position.Col, king.Position.Row];
                    kingEl.AddToClassList("intersection--check");
                }
            }
        }

        public void SetLastMove(BoardPosition from, BoardPosition to)
        {
            _lastMoveFrom = from;
            _lastMoveTo = to;
        }

        public void ClearLastMove()
        {
            _lastMoveFrom = null;
            _lastMoveTo = null;
        }

        // ──────────────────────────────────────────────
        // 장군 / 멍군 다이내믹 알림 배너 (자동 Fade In/Out)
        // ──────────────────────────────────────────────

        /// <summary>
        /// 장군(將軍) 또는 멍군(應將) 배너를 보드 중앙에 팝업하여 페이드인/아웃으로 연출합니다.
        /// </summary>
        public void ShowCallout(CalloutType type)
        {
            if (_calloutBanner == null) return;

            // 기존 예약된 타이머 취소
            _hideCalloutSchedule?.Pause();

            // 스타일 및 텍스트 설정
            _calloutBanner.RemoveFromClassList("callout-banner--check");
            _calloutBanner.RemoveFromClassList("callout-banner--escape");

            if (type == CalloutType.Check)
            {
                _calloutBanner.AddToClassList("callout-banner--check");
                if (_calloutHanja != null) _calloutHanja.text = "將 軍";
                if (_calloutSubtitle != null)
                {
                    _calloutSubtitle.text = LocalizationManager.CurrentLanguage == Language.Korean ? "장군!" : "Check!";
                }
            }
            else
            {
                _calloutBanner.AddToClassList("callout-banner--escape");
                if (_calloutHanja != null) _calloutHanja.text = "應 將";
                if (_calloutSubtitle != null)
                {
                    _calloutSubtitle.text = LocalizationManager.CurrentLanguage == Language.Korean ? "멍군!" : "Escape!";
                }
            }

            // 1. 활성화 및 초기 투명 상태 리셋
            _calloutBanner.style.display = DisplayStyle.Flex;
            _calloutBanner.RemoveFromClassList("callout-banner--visible");
            _calloutBanner.AddToClassList("callout-banner--hidden");

            // 2. 즉시 트랜지션을 통해 페이드인 & 스케일업
            _calloutBanner.schedule.Execute(() =>
            {
                _calloutBanner.RemoveFromClassList("callout-banner--hidden");
                _calloutBanner.AddToClassList("callout-banner--visible");
            }).StartingIn(20);

            // 3. 1초간 유지 후 페이드아웃 & 스케일다운
            _hideCalloutSchedule = _calloutBanner.schedule.Execute(() =>
            {
                _calloutBanner.RemoveFromClassList("callout-banner--visible");
                _calloutBanner.AddToClassList("callout-banner--hidden");

                // 4. 애니메이션 완료 후 display: none 처리
                _calloutBanner.schedule.Execute(() =>
                {
                    if (_calloutBanner.ClassListContains("callout-banner--hidden"))
                    {
                        _calloutBanner.style.display = DisplayStyle.None;
                    }
                }).StartingIn(320);
            }).StartingIn(1000);
        }
    }
}