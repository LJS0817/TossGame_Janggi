using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Janggi.Core;
using Janggi.AI;

namespace Janggi.UI
{
    /// <summary>
    /// UI Toolkit 기반 장기판 UI 컨트롤러.
    /// Board 로직, 자원(Cost), 손패(Hand), 소환(Spawn) 구역 렌더링 및 이벤트를 총괄합니다.
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
        private readonly Label _turnLabel;
        private readonly Label _statusLabel;

        // 플레이어/AI 패널 및 손패 슬롯
        private readonly Label _hanCostLabel;
        private readonly Label _choCostLabel;
        private readonly Label _hanFieldCostLabel;
        private readonly Label _choFieldCostLabel;
        private readonly VisualElement _hanHandContainer;
        private readonly VisualElement _choHandContainer;
        private readonly Button _btnDiscardMode;
        private readonly Button _btnDifficulty;

        // 게임 오버 모달 UI
        private readonly VisualElement _gameOverModal;
        private readonly Label _modalTitle;
        private readonly Label _modalDesc;
        private readonly Label _statDifficulty;
        private readonly Label _statTurns;
        private readonly Label _statCaptures;
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

        // 선택 상태
        private Piece _selectedPiece;
        private List<BoardPosition> _highlightedMoves;
        private BoardPosition? _lastMoveFrom;
        private BoardPosition? _lastMoveTo;

        // 화면 컨테이너
        private readonly VisualElement _mainMenuScreen;
        private readonly VisualElement _gamePlayScreen;

        // 메인 메뉴 요소
        private readonly VisualElement _diffCardEasy;
        private readonly VisualElement _diffCardNormal;
        private readonly VisualElement _diffCardHard;
        private readonly VisualElement _diffCardHell;
        private readonly Button _btnStartGame;
        private readonly Button _btnOpenRules;
        private readonly Button _btnCloseRules;
        private readonly VisualElement _ruleGuideModal;
        private AIDifficulty _selectedDifficulty = AIDifficulty.Normal;

        // 인게임 헤더 및 버튼
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
            _diffCardEasy = root.Q<VisualElement>("diff-card-easy");
            _diffCardNormal = root.Q<VisualElement>("diff-card-normal");
            _diffCardHard = root.Q<VisualElement>("diff-card-hard");
            _diffCardHell = root.Q<VisualElement>("diff-card-hell");
            _btnStartGame = root.Q<Button>("btn-start-game");
            _btnOpenRules = root.Q<Button>("btn-open-rules");
            _btnCloseRules = root.Q<Button>("btn-close-rules");
            _ruleGuideModal = root.Q<VisualElement>("rule-guide-modal");

            // 3. 인게임 요소 바인딩
            _boardArea = root.Q<VisualElement>("board-area");
            _boardContainer = root.Q<VisualElement>("board-container");
            _gridBackground = root.Q<VisualElement>("grid-background");
            _intersectionsGrid = root.Q<VisualElement>("intersections-grid");
            _turnLabel = root.Q<Label>("turn-label");
            _statusLabel = root.Q<Label>("status-label");

            _hanCostLabel = root.Q<Label>("han-cost-label");
            _choCostLabel = root.Q<Label>("cho-cost-label");
            _hanFieldCostLabel = root.Q<Label>("han-field-cost-label");
            _choFieldCostLabel = root.Q<Label>("cho-field-cost-label");
            _hanHandContainer = root.Q<VisualElement>("han-hand-container");
            _choHandContainer = root.Q<VisualElement>("cho-hand-container");
            _btnDiscardMode = root.Q<Button>("btn-discard-mode");
            _btnPassTurn = root.Q<Button>("btn-pass-turn");
            _btnDifficulty = root.Q<Button>("btn-difficulty");
            _btnNewGame = root.Q<Button>("btn-new-game");
            _btnGotoMenu = root.Q<Button>("btn-goto-menu");

            // 4. 모달 팝업 바인딩
            _gameOverModal = root.Q<VisualElement>("game-over-modal");
            _modalTitle = root.Q<Label>("modal-title");
            _modalDesc = root.Q<Label>("modal-desc");
            _statDifficulty = root.Q<Label>("stat-difficulty");
            _statTurns = root.Q<Label>("stat-turns");
            _statCaptures = root.Q<Label>("stat-captures");
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
        }

        public void SelectDifficulty(AIDifficulty difficulty)
        {
            _selectedDifficulty = difficulty;

            _diffCardEasy?.RemoveFromClassList("diff-card--selected");
            _diffCardNormal?.RemoveFromClassList("diff-card--selected");
            _diffCardHard?.RemoveFromClassList("diff-card--selected");
            _diffCardHell?.RemoveFromClassList("diff-card--selected");

            switch (difficulty)
            {
                case AIDifficulty.Easy:   _diffCardEasy?.AddToClassList("diff-card--selected"); break;
                case AIDifficulty.Normal: _diffCardNormal?.AddToClassList("diff-card--selected"); break;
                case AIDifficulty.Hard:   _diffCardHard?.AddToClassList("diff-card--selected"); break;
                case AIDifficulty.Hell:   _diffCardHell?.AddToClassList("diff-card--selected"); break;
            }

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
            if (_gameOverModal == null) return;

            if (_modalTitle != null)
            {
                _modalTitle.RemoveFromClassList("modal-title--win");
                _modalTitle.RemoveFromClassList("modal-title--lose");
                _modalTitle.RemoveFromClassList("modal-title--draw");

                if (isDraw)
                {
                    _modalTitle.text = "무승부 (引分)";
                    _modalTitle.AddToClassList("modal-title--draw");
                    if (_modalDesc != null) _modalDesc.text = "양측의 공방 끝에 교착 상태가 되었습니다.";
                }
                else if (isWin)
                {
                    _modalTitle.text = "승리 (勝利)";
                    _modalTitle.AddToClassList("modal-title--win");
                    if (_modalDesc != null) _modalDesc.text = "적의 궁을 외통수로 멋지게 제압했습니다!";
                }
                else
                {
                    _modalTitle.text = "패배 (敗北)";
                    _modalTitle.AddToClassList("modal-title--lose");
                    if (_modalDesc != null) _modalDesc.text = "아군 궁이 외통수로 제압당했습니다...";
                }
            }

            if (_statDifficulty != null) _statDifficulty.text = difficulty.GetDisplayName();
            if (_statTurns != null) _statTurns.text = $"{turnCount} 턴";
            if (_statCaptures != null) _statCaptures.text = $"{captures} 개";
            if (_statSummons != null) _statSummons.text = $"{summons} 개";

            _gameOverModal.style.display = DisplayStyle.Flex;
        }

        public void HideGameOverModal()
        {
            if (_gameOverModal != null)
            {
                _gameOverModal.style.display = DisplayStyle.None;
            }
        }

        public void UpdateDifficultyDisplay(Janggi.AI.AIDifficulty difficulty)
        {
            if (_btnDifficulty != null)
            {
                _btnDifficulty.text = $"난이도: {difficulty.GetDisplayName()}";
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
                _btnDiscardMode.text = "버릴 카드 선택";
            }
            else
            {
                _btnDiscardMode.RemoveFromClassList("discard-btn--active");
                _btnDiscardMode.text = "패 버리기 (-1)";
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

            _boardContainer.style.width = boardWidth;
            _boardContainer.style.height = boardHeight;
            _boardContainer.style.flexGrow = 0;
            _boardContainer.style.flexShrink = 0;

            UpdatePieceFontSizes(boardWidth);
        }

        private void UpdatePieceFontSizes(float boardWidth)
        {
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

            if (_lastMoveFrom.HasValue)
            {
                var fromEl = _intersectionElements[_lastMoveFrom.Value.Col, _lastMoveFrom.Value.Row];
                fromEl.AddToClassList("intersection--last-from");
            }
            if (_lastMoveTo.HasValue)
            {
                var toEl = _intersectionElements[_lastMoveTo.Value.Col, _lastMoveTo.Value.Row];
                toEl.AddToClassList("intersection--last-to");
            }

            _boardContainer.RegisterCallbackOnce<GeometryChangedEvent>(evt =>
            {
                UpdatePieceFontSizes(evt.newRect.width);
            });
        }

        private void ClearIntersection(VisualElement element)
        {
            var existingPiece = element.Q<Label>(className: "piece");
            if (existingPiece != null)
                element.Remove(existingPiece);

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
            var pieceLabel = new Label();
            pieceLabel.AddToClassList("piece");
            pieceLabel.AddToClassList(piece.Side == PlayerSide.Cho ? "piece-cho" : "piece-han");
            pieceLabel.text = piece.GetDisplayName();
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
                    _choCostLabel.text = $"코스트: {_choState.CurrentCost} / {PlayerState.MaxCost}";
                if (_choFieldCostLabel != null)
                    _choFieldCostLabel.text = $"전력: {_board?.GetTotalPieceCost(PlayerSide.Cho) ?? 0} / {PlayerState.MaxFieldCost}";
                RenderHand(_choHandContainer, _choState, PlayerSide.Cho, isInteractive: _currentTurn == PlayerSide.Cho);
            }

            if (_hanState != null)
            {
                if (_hanCostLabel != null)
                    _hanCostLabel.text = $"코스트: {_hanState.CurrentCost} / {PlayerState.MaxCost}";
                if (_hanFieldCostLabel != null)
                    _hanFieldCostLabel.text = $"전력: {_board?.GetTotalPieceCost(PlayerSide.Han) ?? 0} / {PlayerState.MaxFieldCost}";
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

                var card = new VisualElement();
                card.AddToClassList("hand-card");

                // 카드 상태 스타일링
                if (_selectedHandCardIndex == index && side == PlayerSide.Cho)
                {
                    card.AddToClassList("hand-card--selected");
                }

                if (_isDiscardMode && side == PlayerSide.Cho)
                {
                    card.AddToClassList("hand-card--discard-target");
                }
                else if (side == PlayerSide.Cho && !state.CanSummon(_board, pieceType) && !_isDiscardMode)
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

                // 기물 한국어 이름 라벨
                var nameLabel = new Label(GetPieceTypeName(pieceType));
                nameLabel.AddToClassList("card-name-label");
                card.Add(nameLabel);

                // 클릭 이벤트 바인딩 (플레이어 손패만 인터랙션 가능)
                if (isInteractive)
                {
                    card.RegisterCallback<ClickEvent>(evt =>
                    {
                        OnHandCardClicked(index);
                    });
                }

                container.Add(card);
            }
        }

        private string GetPieceTypeName(PieceType type)
        {
            switch (type)
            {
                case PieceType.Pawn: return "졸";
                case PieceType.Horse: return "마";
                case PieceType.Elephant: return "상";
                case PieceType.Cannon: return "포";
                case PieceType.Chariot: return "차";
                default: return "";
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
                    ShowStatus("이번 턴 소환을 이미 완료했습니다.");
                else if (_board != null && _board.GetTotalPieceCost(PlayerSide.Cho) + pieceType.GetCost() > PlayerState.MaxFieldCost)
                    ShowStatus($"필드 전력 한도({PlayerState.MaxFieldCost})를 초과하여 소환할 수 없습니다!");
                else
                    ShowStatus("코스트가 부족합니다!");
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
            ShowStatus($"[{GetPieceTypeName(pieceType)}] 소환할 빈 칸을 선택하세요.");
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
            if (_turnLabel != null)
            {
                string sideName = currentTurn == PlayerSide.Cho ? "초(楚)" : "한(漢)";
                _turnLabel.text = $"{sideName}의 차례";
            }
        }

        public void ShowStatus(string message)
        {
            if (_statusLabel != null)
                _statusLabel.text = message;
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
    }
}
