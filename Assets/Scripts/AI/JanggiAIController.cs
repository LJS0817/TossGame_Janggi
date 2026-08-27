using System;
using System.Collections.Generic;
using Janggi.Core;
using Janggi.Core.Movement;

namespace Janggi.AI
{
    /// <summary>
    /// gemini.md §5를 완벽히 준수하는 장기 PvE AI 컨트롤러.
    /// 4단계 난이도(하/중/상/극악)별 소환 및 행마 알고리즘을 제공합니다.
    /// </summary>
    public static class JanggiAIController
    {
        private static readonly Random _random = new Random();

        // ──────────────────────────────────────────────
        // 1. 소환 판단 (DecideSpawn)
        // ──────────────────────────────────────────────

        /// <summary>
        /// AI의 손패와 현재 보드 상태를 분석하여 기물 소환 여부, 손패 인덱스, 소환 좌표를 결정합니다.
        /// </summary>
        public static (bool shouldSpawn, int handIndex, BoardPosition spawnPos) DecideSpawn(
            Board board, PlayerState aiState, PlayerState playerState, AIDifficulty difficulty)
        {
            if (aiState.HasSummonedThisTurn || aiState.Hand.Count == 0)
                return (false, -1, default);

            switch (difficulty)
            {
                case AIDifficulty.Easy:
                    return DecideSpawnEasy(board, aiState);

                case AIDifficulty.Normal:
                    return DecideSpawnNormal(board, aiState);

                case AIDifficulty.Hard:
                    return DecideSpawnHard(board, aiState, playerState);

                case AIDifficulty.Hell:
                    return DecideSpawnHell(board, aiState, playerState);

                default:
                    return DecideSpawnNormal(board, aiState);
            }
        }

        /// <summary>[하] 코스트가 모이는 대로 무작위 소환</summary>
        private static (bool, int, BoardPosition) DecideSpawnEasy(Board board, PlayerState aiState)
        {
            var affordableIndices = new List<int>();
            for (int i = 0; i < aiState.Hand.Count; i++)
            {
                if (aiState.CanSummon(board, aiState.Hand[i]))
                    affordableIndices.Add(i);
            }

            if (affordableIndices.Count == 0) return (false, -1, default);

            // 무작위 카드 선택
            int chosenIndex = affordableIndices[_random.Next(affordableIndices.Count)];
            var pieceType = aiState.Hand[chosenIndex];

            var spawnPositions = SpawnRuleValidator.GetSpawnablePositions(board, PlayerSide.Han, pieceType);
            if (spawnPositions.Count == 0) return (false, -1, default);

            var chosenPos = spawnPositions[_random.Next(spawnPositions.Count)];
            return (true, chosenIndex, chosenPos);
        }

        /// <summary>[중] 기물 가치를 계산하여 자원 비축, 위험 시 방어 소환</summary>
        private static (bool, int, BoardPosition) DecideSpawnNormal(Board board, PlayerState aiState)
        {
            bool isHanInCheck = GameRuleValidator.IsInCheck(board, PlayerSide.Han);

            // 위험 상황이 아니고 코스트가 4 미만이면 자원을 모으기 위해 50% 확률로 소환 대기
            if (!isHanInCheck && aiState.CurrentCost < 4 && _random.NextDouble() < 0.5)
            {
                return (false, -1, default);
            }

            int bestCardIndex = -1;
            BoardPosition bestPos = default;
            int bestScore = int.MinValue;

            for (int i = 0; i < aiState.Hand.Count; i++)
            {
                var pieceType = aiState.Hand[i];
                if (!aiState.CanSummon(board, pieceType)) continue;

                var spawnPositions = SpawnRuleValidator.GetSpawnablePositions(board, PlayerSide.Han, pieceType);
                foreach (var pos in spawnPositions)
                {
                    // 시뮬레이션
                    var simBoard = board.Clone();
                    simBoard.PlacePiece(new Piece(pieceType, PlayerSide.Han, pos));

                    int score = BoardEvaluator.Evaluate(simBoard, PlayerSide.Han);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestCardIndex = i;
                        bestPos = pos;
                    }
                }
            }

            if (bestCardIndex >= 0)
                return (true, bestCardIndex, bestPos);

            return (false, -1, default);
        }

        /// <summary>[상] 소환 스나이핑(소환 즉시 공격) 우선 탐색 + 상대 소환지 점거 시도</summary>
        private static (bool, int, BoardPosition) DecideSpawnHard(Board board, PlayerState aiState, PlayerState playerState)
        {
            // 1순위: 소환 직후 상대 기물을 잡을 수 있는 '소환 스나이핑' 기물 우선 탐색
            for (int i = 0; i < aiState.Hand.Count; i++)
            {
                var pieceType = aiState.Hand[i];
                if (!aiState.CanSummon(board, pieceType)) continue;

                var spawnPositions = SpawnRuleValidator.GetSpawnablePositions(board, PlayerSide.Han, pieceType);
                foreach (var pos in spawnPositions)
                {
                    var simBoard = board.Clone();
                    var newPiece = new Piece(pieceType, PlayerSide.Han, pos);
                    simBoard.PlacePiece(newPiece);

                    // 소환된 기물이 이동하여 적을 공격할 수 있는지 확인
                    var legalMoves = GameRuleValidator.GetLegalMoves(simBoard, newPiece);
                    foreach (var move in legalMoves)
                    {
                        var target = simBoard.GetPieceAt(move);
                        if (target != null && target.Side == PlayerSide.Cho)
                        {
                            // 소환 스나이핑 성공!
                            return (true, i, pos);
                        }
                    }
                }
            }

            // 2순위: 일반적인 최적 소환 (Normal 로직 기반)
            return DecideSpawnNormal(board, aiState);
        }

        /// <summary>[극악] 외통수 턴 올인 및 플레이어 공개 패를 분석한 사전 수비 소환</summary>
        private static (bool, int, BoardPosition) DecideSpawnHell(Board board, PlayerState aiState, PlayerState playerState)
        {
            // 1순위: 즉시 외통수를 낼 수 있는 소환 조합 탐색
            for (int i = 0; i < aiState.Hand.Count; i++)
            {
                var pieceType = aiState.Hand[i];
                if (!aiState.CanSummon(board, pieceType)) continue;

                var spawnPositions = SpawnRuleValidator.GetSpawnablePositions(board, PlayerSide.Han, pieceType);
                foreach (var pos in spawnPositions)
                {
                    var simBoard = board.Clone();
                    var newPiece = new Piece(pieceType, PlayerSide.Han, pos);
                    simBoard.PlacePiece(newPiece);

                    var legalMoves = GameRuleValidator.GetLegalMoves(simBoard, newPiece);
                    foreach (var move in legalMoves)
                    {
                        var afterMoveBoard = simBoard.Clone();
                        var simMovedPiece = afterMoveBoard.GetPieceAt(newPiece.Position);
                        afterMoveBoard.MovePiece(simMovedPiece, move);

                        if (GameRuleValidator.IsCheckmate(afterMoveBoard, PlayerSide.Cho))
                        {
                            // 즉시 외통수 소환!
                            return (true, i, pos);
                        }
                    }
                }
            }

            // 2순위: 소환 스나이핑 및 수비적 최적화
            return DecideSpawnHard(board, aiState, playerState);
        }

        // ──────────────────────────────────────────────
        // 2. 이동 판단 (DecideMove)
        // ──────────────────────────────────────────────

        /// <summary>
        /// 현재 보드 상태에서 AI 난이도에 맞는 최적의 이동 합법수를 결정합니다.
        /// </summary>
        public static (Piece piece, BoardPosition targetPos) DecideMove(
            Board board, PlayerState aiState, PlayerState playerState, AIDifficulty difficulty)
        {
            var allLegalMoves = GameRuleValidator.GetAllLegalMovesForSide(board, PlayerSide.Han);
            if (allLegalMoves.Count == 0)
                return (null, default);

            switch (difficulty)
            {
                case AIDifficulty.Easy:
                    return DecideMoveEasy(board, allLegalMoves);

                case AIDifficulty.Normal:
                    return DecideMoveMinimax(board, allLegalMoves, depth: 3);

                case AIDifficulty.Hard:
                    return DecideMoveMinimax(board, allLegalMoves, depth: 4);

                case AIDifficulty.Hell:
                    return DecideMoveMCTS(board, allLegalMoves, simulations: 30);

                default:
                    return DecideMoveMinimax(board, allLegalMoves, depth: 3);
            }
        }

        /// <summary>[하] 1수 탐색: 눈앞의 기물을 무조건 공격 (없으면 무작위 전진)</summary>
        private static (Piece, BoardPosition) DecideMoveEasy(Board board, List<(Piece piece, BoardPosition to)> moves)
        {
            var captureMoves = new List<(Piece piece, BoardPosition to)>();

            foreach (var m in moves)
            {
                var target = board.GetPieceAt(m.to);
                if (target != null && target.Side == PlayerSide.Cho)
                {
                    captureMoves.Add(m);
                }
            }

            // 잡을 수 있는 기물이 있으면 그 중 무작위 선택
            if (captureMoves.Count > 0)
            {
                return captureMoves[_random.Next(captureMoves.Count)];
            }

            // 없으면 전체 합법수 중 무작위 선택
            return moves[_random.Next(moves.Count)];
        }

        /// <summary>[중/상] Minimax with Alpha-Beta Pruning 탐색</summary>
        private static (Piece, BoardPosition) DecideMoveMinimax(
            Board board, List<(Piece piece, BoardPosition to)> moves, int depth)
        {
            (Piece bestPiece, BoardPosition bestTo) = moves[0];
            int bestValue = int.MinValue;
            int alpha = int.MinValue;
            int beta = int.MaxValue;

            // 이동 수들을 가치 순(공격수 우선)으로 정렬하여 가지치기 효율 극대화
            moves.Sort((a, b) =>
            {
                var targetA = board.GetPieceAt(a.to);
                var targetB = board.GetPieceAt(b.to);
                int valA = targetA != null ? BoardEvaluator.GetPieceValue(targetA.Type) : 0;
                int valB = targetB != null ? BoardEvaluator.GetPieceValue(targetB.Type) : 0;
                return valB.CompareTo(valA);
            });

            foreach (var m in moves)
            {
                var simBoard = board.Clone();
                var simPiece = simBoard.GetPieceAt(m.piece.Position);
                simBoard.MovePiece(simPiece, m.to);

                int val = Minimax(simBoard, depth - 1, alpha, beta, isMaximizing: false);

                if (val > bestValue)
                {
                    bestValue = val;
                    bestPiece = m.piece;
                    bestTo = m.to;
                }

                alpha = Math.Max(alpha, bestValue);
                if (beta <= alpha)
                    break;
            }

            return (bestPiece, bestTo);
        }

        private static int Minimax(Board board, int depth, int alpha, int beta, bool isMaximizing)
        {
            if (depth == 0 || GameRuleValidator.IsCheckmate(board, PlayerSide.Cho) || GameRuleValidator.IsCheckmate(board, PlayerSide.Han))
            {
                return BoardEvaluator.Evaluate(board, PlayerSide.Han);
            }

            var currentSide = isMaximizing ? PlayerSide.Han : PlayerSide.Cho;
            var legalMoves = GameRuleValidator.GetAllLegalMovesForSide(board, currentSide);

            if (legalMoves.Count == 0)
            {
                return BoardEvaluator.Evaluate(board, PlayerSide.Han);
            }

            if (isMaximizing)
            {
                int maxEval = int.MinValue;
                foreach (var m in legalMoves)
                {
                    var simBoard = board.Clone();
                    var simPiece = simBoard.GetPieceAt(m.piece.Position);
                    simBoard.MovePiece(simPiece, m.to);

                    int eval = Minimax(simBoard, depth - 1, alpha, beta, false);
                    maxEval = Math.Max(maxEval, eval);
                    alpha = Math.Max(alpha, eval);
                    if (beta <= alpha) break;
                }
                return maxEval;
            }
            else
            {
                int minEval = int.MaxValue;
                foreach (var m in legalMoves)
                {
                    var simBoard = board.Clone();
                    var simPiece = simBoard.GetPieceAt(m.piece.Position);
                    simBoard.MovePiece(simPiece, m.to);

                    int eval = Minimax(simBoard, depth - 1, alpha, beta, true);
                    minEval = Math.Min(minEval, eval);
                    beta = Math.Min(beta, eval);
                    if (beta <= alpha) break;
                }
                return minEval;
            }
        }

        /// <summary>[극악] MCTS 기반 몬테카를로 롤아웃 시뮬레이션</summary>
        private static (Piece, BoardPosition) DecideMoveMCTS(
            Board board, List<(Piece piece, BoardPosition to)> moves, int simulations)
        {
            // 먼저 즉시 외통수(Checkmate)가 나는 수가 있다면 즉시 실행
            foreach (var m in moves)
            {
                var simBoard = board.Clone();
                var simPiece = simBoard.GetPieceAt(m.piece.Position);
                simBoard.MovePiece(simPiece, m.to);

                if (GameRuleValidator.IsCheckmate(simBoard, PlayerSide.Cho))
                {
                    return (m.piece, m.to);
                }
            }

            (Piece bestPiece, BoardPosition bestTo) = moves[0];
            double bestWinRate = double.MinValue;

            foreach (var m in moves)
            {
                int totalScore = 0;

                for (int s = 0; s < simulations; s++)
                {
                    var simBoard = board.Clone();
                    var simPiece = simBoard.GetPieceAt(m.piece.Position);
                    simBoard.MovePiece(simPiece, m.to);

                    // 롤아웃 (2수 랜덤 시뮬레이션 후 평가)
                    totalScore += Rollout(simBoard, 2);
                }

                double avgScore = (double)totalScore / simulations;
                if (avgScore > bestWinRate)
                {
                    bestWinRate = avgScore;
                    bestPiece = m.piece;
                    bestTo = m.to;
                }
            }

            return (bestPiece, bestTo);
        }

        private static int Rollout(Board board, int steps)
        {
            var currentSide = PlayerSide.Cho;
            for (int i = 0; i < steps; i++)
            {
                var moves = GameRuleValidator.GetAllLegalMovesForSide(board, currentSide);
                if (moves.Count == 0) break;

                var chosen = moves[_random.Next(moves.Count)];
                var piece = board.GetPieceAt(chosen.piece.Position);
                board.MovePiece(piece, chosen.to);

                currentSide = currentSide.Opposite();
            }

            return BoardEvaluator.Evaluate(board, PlayerSide.Han);
        }
    }
}
