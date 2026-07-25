using ChessChallenge.API;
using System;
using System.Linq;

public class MyBot : IChessBot
{
    int[][] _unpackedPestoTables;
    (ulong, int, int, int, Move)[] _tt = new (ulong, int, int, int, Move)[4194304];
    int[] _corr = new int[65536];

    public MyBot() => _unpackedPestoTables = new[]
    {
        64056190532934862535115182336m, 71817484777349427393728857088m, 75531323896504881327324719616m, 75534941283922731684258701056m,
        76772871878331001482534463744m, 2799909883628844474924580864m, 936945638459632287977174528m, 75530077040072166139492022528m,
        77047298039705029164133175105m, 3420098330133489500069553497m, 2810795141459723637314826025m, 3437013754922314846191425599m,
        3452687399601669993672216365m, 7756477354678319089008978259m, 4666481959896172492430705942m, 2166404344976801627463873784m,
        2458978764686864123971363324m, 3407990144915349139311044357m, 4648343388770830403206125585m, 2825278511831799532529265428m,
        4060800717490094020123637547m, 9313555142942623235395638565m, 8990767170971816684347207952m, 2792609270433996274743057906m,
        77681965261137903437104413430m, 4349719913650185835651468041m, 4660451666051563133822831619m, 5297517757161421759538340621m,
        5305980311756629996622845967m, 6840107139378774769718013191m, 5307179644552139055246412555m, 338503987890727377340141296m,
        75809329684924988220715628526m, 78321482353259760869261771775m, 4037836016230363395302951933m, 4679766237412432713899641095m,
        5286618535172578479103676939m, 4670061681696123972825320451m, 1580047120093612252498169095m, 76771592099215640831612681200m,
        75811728573021415751104393454m, 78896921543467230670600403709m, 2178470270525718857312176125m, 4028136238365719466699458553m,
        4649505256930799955128945665m, 3418804475759153966398638849m, 1557077491473974928893218838m, 77376040767991305941258073592m,
        73951186976511258715111025896m, 77043619187199676893167738367m, 1213766170969874494374279410m, 3081561358716687252805713648m,
        3082732579536109867624693750m, 1219782446916489223095520015m, 78581353977031800797252744729m, 75798434926037487999592297714m,
        68680260848152242288790780416m, 72707225993312342439043723776m, 75187941591991138791072389376m, 77029126410771504959778384384m,
        73964489957686756296824976384m, 76417372019396592654299491840m, 74568981255664116992330626048m, 70840573581001112577000009728m,
    }.Select(p => decimal.GetBits(p).SelectMany(BitConverter.GetBytes).Select((s, i) => (int)((sbyte)s * 1.5) + new[] { 82, 337, 365, 477, 1025, 0, 94, 281, 297, 512, 936, 0, 0, 0, 0, 0 }[i]).ToArray()).ToArray();

    public Move Think(Board board, Timer timer)
    {
        bool aborted = false;
        Move bestMoveRoot = default, completedBestMoveRoot = default;

        var history = new int[65536];
        var killers = new Move[256];

        int maxThinkTime = Math.Min(timer.MillisecondsRemaining * 6 / 10, timer.MillisecondsRemaining / 25 + timer.IncrementMilliseconds / 2);

        int guess = 0;
        for (int depth = 1; !aborted; depth++)
        {
            int lower = -9999999, upper = 9999999;
            while (lower < upper && !aborted)
            {
                int beta = Math.Max(guess, lower + 1);
                guess = Search(depth, 0, beta - 1, beta);
                if (guess < beta) upper = guess; else lower = guess;
            }
            if (!aborted)
                completedBestMoveRoot = bestMoveRoot;
            if (timer.MillisecondsElapsedThisTurn * 2 >= maxThinkTime)
                break;
        }

        return completedBestMoveRoot.IsNull ? board.GetLegalMoves()[0] : completedBestMoveRoot;

        int Search(int depth, int ply, int alpha, int beta)
        {
            int Recurse(int a, int b, int r = 1) =>
                -Search(depth - r, ply + 1, a, b);

            if (timer.MillisecondsElapsedThisTurn >= maxThinkTime)
            {
                aborted = true;
                return 0;
            }

            bool inCheck = board.IsInCheck();

            if (inCheck) depth++;

            if (board.IsRepeatedPosition())
                return 0;

            ulong key = board.ZobristKey;
            var (zobristHash, score, ttDepth, flag, ttMove) = _tt[key % 4194304UL];

            if (zobristHash == key && ttDepth >= depth)
            {
                int val = score > 90000 ? score - ply : score < -90000 ? score + ply : score;
                if (ply == 0) bestMoveRoot = ttMove;
                if (flag == 0 || (flag == 1 ? val <= alpha : val >= beta)) return val;
            }

            int scoreVal = 0;

            if (depth <= 0)
            {
                scoreVal = Evaluate() + _corr[key % 65536UL] / 32;
                if (scoreVal >= beta) return scoreVal;
                alpha = Math.Max(alpha, scoreVal);
            }

            Span<Move> moves = stackalloc Move[256];
            board.GetLegalMovesNonAlloc(ref moves, depth <= 0 && !inCheck);

            if (moves.IsEmpty)
                return depth <= 0 ? Evaluate() : inCheck ? ply - 100000 : 0;

            Span<int> scores = stackalloc int[moves.Length];
            int i = 0;
            foreach (var m in moves)
                scores[i++] = m == ttMove && zobristHash == key ? -100000000 :
                              m.IsCapture ? -80000000 - 100 * (int)m.CapturePieceType :
                              m.IsPromotion ? -70000000 :
                              m == killers[ply] ? -60000000 :
                              -history[m.RawValue];

            scores.Sort(moves);

            int bestScore = -9999999, movesSearched = 0;
            Move bestMove = default;

            foreach (var move in moves)
            {
                board.MakeMove(move);

                int eval = movesSearched++ == 0 ? Recurse(-beta, -alpha) :
                    (depth >= 3 && movesSearched >= 3 && !move.IsCapture && !move.IsPromotion && !inCheck) ? Recurse(-alpha - 1, -alpha, 2) : alpha + 1;

                if (eval > alpha && (eval = Recurse(-alpha - 1, -alpha)) > alpha && eval < beta)
                    eval = Recurse(-beta, -eval);

                board.UndoMove(move);

                if (aborted) return 0;

                if (eval > bestScore)
                {
                    bestScore = eval;
                    bestMove = move;
                    if (ply == 0) bestMoveRoot = move;
                }

                if (eval > alpha)
                {
                    alpha = eval;
                    if (alpha >= beta)
                    {
                        if (depth > 0 && !move.IsCapture)
                        {
                            killers[ply] = move;
                            history[move.RawValue] += depth * depth - history[move.RawValue] * depth * depth / 512;
                        }
                        break;
                    }
                }
            }

            if (depth <= 0)
                _corr[key % 65536UL] += (bestScore - scoreVal) / 4;

            _tt[key % 4194304UL] = (key, bestScore > 90000 ? bestScore + ply : bestScore < -90000 ? bestScore - ply : bestScore, depth, bestScore >= beta ? 2 : bestScore > alpha ? 0 : 1, bestMove);

            return bestScore;
        }

        int Evaluate()
        {
            int middleGame = 0, endGame = 0, phase = 0, side = 2, pieceType, square;

            for (; --side >= 0; middleGame = -middleGame, endGame = -endGame)
                for (pieceType = -1; ++pieceType < 6;)
                    for (ulong mask = board.GetPieceBitboard((PieceType)pieceType + 1, side > 0); mask != 0;)
                    {
                        phase += 0x00042110 >> pieceType * 4 & 0x0F;
                        square = BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ 56 * side;
                        middleGame += _unpackedPestoTables[square][pieceType];
                        endGame += _unpackedPestoTables[square][pieceType + 6];
                    }

            return (middleGame * phase + endGame * (24 - phase)) / (board.IsWhiteToMove ? 24 : -24);
        }
    }
}