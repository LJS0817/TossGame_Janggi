using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using Janggi.Core;
using Janggi.UI;
using Janggi.AI;
using TossGame.Toss;

namespace Janggi
{
    /// <summary>
    /// 장기 디펜스 게임의 메인 관리자.
    /// 보드, 자원(코스트), 손패(덱), 소환, 행마, 턴 시퀀스 및 적 AI(PvE)를 총괄합니다.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private PanelRenderer _panelRenderer;

        [Header("AI Settings")]
        [SerializeField] private AIDifficulty _aiDifficulty = AIDifficulty.Normal;

        // 보드 및 플레이어 자원 상태
        private Board _board;
        private PlayerState _choState; // 플레이어 (초)
        private PlayerState _hanState; // 적 AI (한)
        private PlayerSide _currentTurn;
        private bool _gameOver;
        private Coroutine _aiCoroutine;

        // 전투 통계
        private int _turnCount = 1;
        private int _playerCaptures = 0;
        private int _playerSummons = 0;
        private bool _isInGame = false;

        // UI 컨트롤러
        private BoardUIController _uiController;

        private void OnEnable()
        {
            InitializeGame();

            if (_panelRenderer != null)
            {
                _panelRenderer.RegisterUIReloadCallback(OnUIReload);
            }
        }

        private void OnDisable()
        {
            if (_panelRenderer != null)
            {
                _panelRenderer.UnregisterUIReloadCallback(OnUIReload);
            }

            if (_aiCoroutine != null)
            {
                StopCoroutine(_aiCoroutine);
                _aiCoroutine = null;
            }
        }

        /// <summary>
        /// PanelRenderer의 UI가 로드/리로드될 때 호출되는 콜백.
        /// </summary>
        private void OnUIReload(PanelRenderer renderer, VisualElement root)
        {
            _uiController = new BoardUIController(root);
            _uiController.SetGameState(_board, _choState, _hanState, _currentTurn);
            _uiController.SelectDifficulty(_aiDifficulty);

            // 이벤트 바인딩
            _uiController.OnPieceSelected = OnPieceSelected;
            _uiController.OnMoveRequested = OnMoveRequested;
            _uiController.OnSpawnRequested = OnSpawnRequested;
            _uiController.OnDiscardRequested = OnDiscardRequested;
            _uiController.OnPassRequested = OnPassRequested;
            _uiController.OnDifficultyToggled = OnDifficultyToggled;
            _uiController.OnRestartRequested = InitializeGame;
            _uiController.OnStartGameRequested = StartGame;
            _uiController.OnReturnToMenuRequested = ReturnToMainMenu;
            _uiController.OnShareRequested = OnTossShareRequested;
            _uiController.OnAdRequested = OnTossAdRequested;

            if (_isInGame)
            {
                _uiController.ShowGamePlay();
            }
            else
            {
                _uiController.ShowMainMenu();
            }

            Debug.Log("[Janggi] UI 바인딩 완료 (PanelRenderer).");
        }

        /// <summary>
        /// 메인 메뉴에서 난이도를 선택하고 게임을 시작합니다.
        /// </summary>
        public void StartGame(AIDifficulty difficulty)
        {
            _aiDifficulty = difficulty;
            _isInGame = true;
            _uiController?.ShowGamePlay();
            InitializeGame();
            Debug.Log($"[Janggi] 게임 시작! 난이도: {difficulty.GetDisplayName()}");
        }

        /// <summary>
        /// 인게임에서 메인 메뉴 화면으로 복귀합니다.
        /// </summary>
        public void ReturnToMainMenu()
        {
            if (_aiCoroutine != null)
            {
                StopCoroutine(_aiCoroutine);
                _aiCoroutine = null;
            }

            _isInGame = false;
            _gameOver = true;
            _uiController?.ShowMainMenu();
            Debug.Log("[Janggi] 메인 메뉴로 복귀");
        }

        private void OnDifficultyToggled()
        {
            // Easy -> Normal -> Hard -> Hell -> Easy 순환
            switch (_aiDifficulty)
            {
                case AIDifficulty.Easy:   _aiDifficulty = AIDifficulty.Normal; break;
                case AIDifficulty.Normal: _aiDifficulty = AIDifficulty.Hard;   break;
                case AIDifficulty.Hard:   _aiDifficulty = AIDifficulty.Hell;   break;
                case AIDifficulty.Hell:   _aiDifficulty = AIDifficulty.Easy;   break;
            }

            _uiController?.SelectDifficulty(_aiDifficulty);
            _uiController?.ShowStatus($"난이도가 [{_aiDifficulty.GetDisplayName()}]로 변경되었습니다.");
            Debug.Log($"[Janggi] AI 난이도 변경: {_aiDifficulty}");
        }

        /// <summary>
        /// 게임을 초기화합니다.
        /// </summary>
        public void InitializeGame()
        {
            if (_aiCoroutine != null)
            {
                StopCoroutine(_aiCoroutine);
                _aiCoroutine = null;
            }

            // 1. 보드 및 왕/사 초기 배치 (gemini.md §4)
            _board = new Board();
            _board.SetupInitialPosition();

            // 2. 자원 및 손패 4장 초기화 (양측 모두 자신의 첫 턴 시작 시 0 + 2 = 2 코스트로 시작)
            _choState = new PlayerState(PlayerSide.Cho);
            _hanState = new PlayerState(PlayerSide.Han);
            _choState.StartTurn(); // 선공(초) 첫 턴 코스트 +2 충전 -> 2

            _currentTurn = PlayerSide.Cho; // 초(플레이어) 선공
            _gameOver = false;

            // 통계 리셋
            _turnCount = 1;
            _playerCaptures = 0;
            _playerSummons = 0;

            // UI 갱신
            if (_uiController != null)
            {
                _uiController.HideGameOverModal();
                _uiController.SetInteractive(true);
                _uiController.SetDiscardMode(false);
                _uiController.ClearSelection();
                _uiController.ClearLastMove();
                _uiController.SetGameState(_board, _choState, _hanState, _currentTurn);
                _uiController.UpdateDifficultyDisplay(_aiDifficulty);
                _uiController.ShowStatus("게임 시작! 초(楚)의 차례입니다.");
            }

            Debug.Log("[Janggi] 게임 초기화 완료. 초(楚) 코스트: " + _choState.CurrentCost + ", 손패 4장 세팅.");
        }

        // ──────────────────────────────────────────────
        // 1. 기물 소환 (선택 행동)
        // ──────────────────────────────────────────────

        /// <summary>
        /// 플레이어가 손패의 카드를 보드에 소환합니다.
        /// </summary>
        private void OnSpawnRequested(int handIndex, BoardPosition spawnPos)
        {
            if (_gameOver || _currentTurn != PlayerSide.Cho) return;

            var currentState = _choState;
            if (handIndex < 0 || handIndex >= currentState.Hand.Count) return;
            var pieceType = currentState.Hand[handIndex];

            // 소환 구역 및 코스트 유효성 검증
            if (!SpawnRuleValidator.CanSpawnAt(_board, _currentTurn, pieceType, spawnPos))
            {
                _uiController?.ShowStatus("해당 위치에는 소환할 수 없습니다.");
                return;
            }

            // 필드 전력 상한(20) 검증
            if (_board.GetTotalPieceCost(_currentTurn) + pieceType.GetCost() > PlayerState.MaxFieldCost)
            {
                _uiController?.ShowStatus($"필드 전력 한도({PlayerState.MaxFieldCost})를 초과하여 소환할 수 없습니다!");
                return;
            }

            // 코스트 지불 및 카드 소모 (새 카드로 즉시 보충)
            if (!currentState.ConsumeCardForSummon(handIndex))
            {
                _uiController?.ShowStatus("코스트가 부족하거나 이미 소환을 완료했습니다.");
                return;
            }

            _playerSummons++;

            // 보드에 새 기물 배치
            var newPiece = new Piece(pieceType, _currentTurn, spawnPos);
            _board.PlacePiece(newPiece);

            TossSDKManager.Instance.TriggerHaptic(TossHapticType.Light);

            Debug.Log($"[Janggi] {_currentTurn} {pieceType} 소환됨 @ {spawnPos} (남은 코스트: {currentState.CurrentCost})");

            // UI 갱신 (선택을 비워두어 소환된 기물 또는 기존 기물 중 자유롭게 선택 가능)
            _uiController.ClearSelection();
            _uiController.RefreshBoardPieces();
            _uiController.RefreshPlayerPanels();
            _uiController.ShowStatus($"[{newPiece.GetDisplayName()}] 소환 완료! 이동할 기물을 선택하세요.");
        }

        // ──────────────────────────────────────────────
        // 2. 패 버리기 (선택 행동)
        // ──────────────────────────────────────────────

        private void OnDiscardRequested(int handIndex)
        {
            if (_gameOver || _currentTurn != PlayerSide.Cho) return;

            if (_choState.CurrentCost < PlayerState.DiscardCost)
            {
                _uiController?.ShowStatus("패를 버릴 코스트(1)가 부족합니다.");
                return;
            }

            var discardedType = _choState.Hand[handIndex];
            _choState.DiscardCard(handIndex);

            TossSDKManager.Instance.TriggerHaptic(TossHapticType.Light);

            _uiController.SetDiscardMode(false);
            _uiController.ClearSelection();
            _uiController.RefreshPlayerPanels();
            _uiController.ShowStatus($"[{discardedType.GetKoreanName(PlayerSide.Cho)}] 카드를 버리고 새 카드를 뽑았습니다.");

            Debug.Log($"[Janggi] 패 버리기 완료: {discardedType} -> 남은 코스트: {_choState.CurrentCost}");
        }

        // ──────────────────────────────────────────────
        // 3. 한 수 쉼 (한 턴 쉬기)
        // ──────────────────────────────────────────────

        private void OnPassRequested()
        {
            if (_gameOver || _currentTurn != PlayerSide.Cho) return;

            // 장군(Check) 상태에서는 한 수 쉼 불가
            if (GameRuleValidator.IsInCheck(_board, PlayerSide.Cho))
            {
                TossSDKManager.Instance.TriggerHaptic(TossHapticType.Warning);
                _uiController?.ShowStatus("장군(Check) 상태에서는 한 수 쉴 수 없습니다! 왕을 지키세요.");
                return;
            }

            TossSDKManager.Instance.TriggerHaptic(TossHapticType.Light);

            _uiController.ClearSelection();
            _uiController.SetDiscardMode(false);
            _uiController.ShowStatus("초(楚) 플레이어가 한 수 쉬었습니다.");

            Debug.Log("[Janggi] 초(楚) 한 수 쉼 선택 -> 턴 종료");
            ProcessPostMove();
        }

        // ──────────────────────────────────────────────
        // 4. 기물 선택
        // ──────────────────────────────────────────────

        private void OnPieceSelected(Piece piece)
        {
            if (_gameOver || _currentTurn != PlayerSide.Cho) return;

            if (piece.Side != _currentTurn)
            {
                _uiController.ClearSelection();
                return;
            }

            _uiController.SetDiscardMode(false);

            var legalMoves = GameRuleValidator.GetLegalMoves(_board, piece);
            _uiController.SelectPiece(piece, legalMoves);

            Debug.Log($"[Janggi] {piece} 선택됨. 합법수: {legalMoves.Count}개");
        }

        // ──────────────────────────────────────────────
        // 5. 기물 이동/공격 (필수 행동 -> 턴 종료)
        // ──────────────────────────────────────────────

        private void OnMoveRequested(Piece piece, BoardPosition to)
        {
            if (_gameOver || piece.Side != _currentTurn) return;

            if (!GameRuleValidator.IsMoveLegal(_board, piece, to))
            {
                Debug.LogWarning($"[Janggi] 불법 수: {piece} → {to}");
                return;
            }

            ExecuteMove(piece, to);
        }

        private void ExecuteMove(Piece piece, BoardPosition to)
        {
            var from = piece.Position;

            var captured = _board.MovePiece(piece, to);
            if (captured != null)
            {
                if (_currentTurn == PlayerSide.Cho) _playerCaptures++;
                TossSDKManager.Instance.TriggerHaptic(TossHapticType.Medium);
                Debug.Log($"[Janggi] {captured} 잡힘! (플레이어 총 처치: {_playerCaptures})");
            }
            else
            {
                TossSDKManager.Instance.TriggerHaptic(TossHapticType.Light);
            }

            Debug.Log($"[Janggi] {_currentTurn} {piece.Type}: {from} → {to}");

            _uiController.ClearSelection();
            _uiController.SetLastMove(from, to);
            _uiController.RefreshBoardPieces();

            ProcessPostMove();
        }

        /// <summary>
        /// 필수 행동(이동) 완료 후 승패 판정 및 턴 교대를 처리합니다.
        /// </summary>
        private void ProcessPostMove()
        {
            var nextTurn = _currentTurn.Opposite();

            // 2. 외통수(체크메이트) 체크
            if (GameRuleValidator.IsCheckmate(_board, nextTurn))
            {
                _gameOver = true;
                bool isPlayerWin = _currentTurn == PlayerSide.Cho;
                string winner = isPlayerWin ? "초(楚) 플레이어" : "한(漢) 적 AI";
                
                if (isPlayerWin)
                {
                    TossSDKManager.Instance.TriggerHaptic(TossHapticType.Success);
                }
                else
                {
                    TossSDKManager.Instance.TriggerHaptic(TossHapticType.Error);
                }

                _uiController.ShowStatus($"외통수! {winner} 승리!");
                _uiController.ShowGameOverModal(isWin: isPlayerWin, isDraw: false, _aiDifficulty, _turnCount, _playerCaptures, _playerSummons);
                Debug.Log($"[Janggi] 외통수! {winner} 승리!");
                return;
            }

            // 3. 스테일메이트 체크
            if (GameRuleValidator.IsStalemate(_board, nextTurn))
            {
                _gameOver = true;
                TossSDKManager.Instance.TriggerHaptic(TossHapticType.Medium);
                _uiController.ShowStatus("교착 상태! 무승부!");
                _uiController.ShowGameOverModal(isWin: false, isDraw: true, _aiDifficulty, _turnCount, _playerCaptures, _playerSummons);
                Debug.Log("[Janggi] 교착 상태(Stalemate) — 무승부!");
                return;
            }

            // 3-1. 장군(Check) 경고 햅틱
            if (GameRuleValidator.IsInCheck(_board, nextTurn))
            {
                TossSDKManager.Instance.TriggerHaptic(TossHapticType.Warning);
            }

            // 4. 턴 교대 및 새 턴 자원 충전 (+2, gemini.md §3)
            if (nextTurn == PlayerSide.Cho)
            {
                _turnCount++;
            }

            _currentTurn = nextTurn;
            var nextState = GetCurrentPlayerState();
            nextState.StartTurn();

            _uiController.SetDiscardMode(false);
            _uiController.ClearSelection();
            _uiController.SetGameState(_board, _choState, _hanState, _currentTurn);

            // 5. 장군 상태 알림
            if (GameRuleValidator.IsInCheck(_board, _currentTurn))
            {
                string checkedSide = _currentTurn == PlayerSide.Cho ? "초(楚)" : "한(漢)";
                _uiController.ShowStatus($"장군! {checkedSide}이(가) 위험합니다!");
            }
            else
            {
                _uiController.ShowStatus("");
            }

            // 6. 적 AI 턴인 경우 자동 실행
            if (_currentTurn == PlayerSide.Han && !_gameOver)
            {
                _uiController.SetInteractive(false);
                _aiCoroutine = StartCoroutine(ExecuteAITurnCoroutine());
            }
            else
            {
                _uiController.SetInteractive(true);
            }

            Debug.Log($"[Janggi] {_currentTurn}의 턴 시작 (코스트: {nextState.CurrentCost}/10)");
        }

        // ──────────────────────────────────────────────
        // 5. PvE 적 AI 턴 실행 코루틴
        // ──────────────────────────────────────────────

        private IEnumerator ExecuteAITurnCoroutine()
        {
            _uiController.ShowStatus("적 AI(漢)가 수를 고민 중입니다...");
            yield return new WaitForSeconds(0.6f);

            if (_gameOver) yield break;

            // 1단계: AI 소환 판단 (gemini.md §5 난이도별 소환)
            var (shouldSpawn, handIndex, spawnPos) = JanggiAIController.DecideSpawn(_board, _hanState, _choState, _aiDifficulty);

            if (shouldSpawn && handIndex >= 0 && handIndex < _hanState.Hand.Count)
            {
                var pieceType = _hanState.Hand[handIndex];
                if (SpawnRuleValidator.CanSpawnAt(_board, PlayerSide.Han, pieceType, spawnPos))
                {
                    // 코스트 차감 및 소환 1회 완료 검증
                    if (_hanState.ConsumeCardForSummon(handIndex))
                    {
                        var aiNewPiece = new Piece(pieceType, PlayerSide.Han, spawnPos);
                        _board.PlacePiece(aiNewPiece);

                        _uiController.RefreshBoardPieces();
                        _uiController.RefreshPlayerPanels();
                        _uiController.ShowStatus($"적 AI(漢)가 [{aiNewPiece.GetDisplayName()}] 소환! (AI 남은 코스트: {_hanState.CurrentCost})");

                        Debug.Log($"[Janggi] AI {pieceType} 소환 완료 (소모: {pieceType.GetCost()}, 남은 코스트: {_hanState.CurrentCost})");
                        yield return new WaitForSeconds(0.5f);
                    }
                }
            }

            if (_gameOver) yield break;

            // 2단계: AI 행마 판단 (gemini.md §5 난이도별 이동)
            var (bestPiece, bestTargetPos) = JanggiAIController.DecideMove(_board, _hanState, _choState, _aiDifficulty);

            if (bestPiece != null)
            {
                _uiController.ShowStatus($"적 AI(漢)가 [{bestPiece.GetDisplayName()}]을(를) 이동합니다.");
                yield return new WaitForSeconds(0.3f);

                ExecuteMove(bestPiece, bestTargetPos);
            }
            else
            {
                // 합법수가 없는 경우(외통수/스테일메이트)
                ProcessPostMove();
            }

            _aiCoroutine = null;
        }

        private PlayerState GetCurrentPlayerState()
        {
            return _currentTurn == PlayerSide.Cho ? _choState : _hanState;
        }

        // ──────────────────────────────────────────────
        // 공개 API
        // ──────────────────────────────────────────────

        public Board GetBoard() => _board;
        public PlayerSide GetCurrentTurn() => _currentTurn;
        public PlayerState GetChoState() => _choState;
        public PlayerState GetHanState() => _hanState;
        public AIDifficulty GetDifficulty() => _aiDifficulty;
        public bool IsGameOver() => _gameOver;

        // ──────────────────────────────────────────────
        // 6. 토스 SDK 이벤트 핸들러
        // ──────────────────────────────────────────────

        private void OnTossShareRequested()
        {
            string title = "將棋: AI 장기 덱빌딩 디펜스";
            string desc = $"AI 난이도 [{_aiDifficulty.GetDisplayName()}]에서 {_turnCount}턴 만에 승리했습니다! 지금 도전해보세요.";
            
            TossSDKManager.Instance.ShareGameResult(title, desc);
            _uiController?.ShowStatus("토스 공유 시트를 열었습니다.");
        }

        private void OnTossAdRequested()
        {
            _uiController?.ShowStatus("광고를 불러오는 중입니다...");

            TossSDKManager.Instance.ShowRewardedAd(isSuccess =>
            {
                if (isSuccess)
                {
                    // 보상: 코스트 +5 즉시 충전 (최대 10까지)
                    _choState.AddCost(5);
                    _uiController?.RefreshPlayerPanels();
                    _uiController?.ShowStatus("📺 광고 보상 지급 완료! 아군 코스트 +5가 충전되었습니다.");
                    TossSDKManager.Instance.TriggerHaptic(TossHapticType.Success);
                    Debug.Log($"[TossSDK] 광고 보상 지급: 초(楚) 코스트 -> {_choState.CurrentCost}");
                }
                else
                {
                    _uiController?.ShowStatus("광고 시청이 취소되었습니다.");
                }
            });
        }
    }
}
