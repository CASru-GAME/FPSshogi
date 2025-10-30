using UnityEngine;
using App.Main.ShogiThings;
using App.Common.Initialize;
using System.Collections.Generic;

namespace App.Main.GameMaster
{
    public class ShogiBoard : MonoBehaviour, IInitializable
    {
        [SerializeField] private GameStateHolder gameStateHolder;
        private IPiece[,] board = new IPiece[9, 9]; // 9x9の将棋盤を表す多次元配列
        private Dictionary<PlayerType, List<PieceType>> capturedPieces = new Dictionary<PlayerType, List<PieceType>>()
        {
            { PlayerType.PlayerOne, new List<PieceType>() },
            { PlayerType.PlayerTwo, new List<PieceType>() }
        };
        int savedFromX = 0;
        int savedFromY = 0;
        int savedToX = 0;
        int savedToY = 0;
        PlayerType currentPlayer = PlayerType.PlayerOne;
        public int InitializationPriority => 100; // 優先度（低いほど先に初期化される）
        public System.Type[] Dependencies => new System.Type[] { typeof(GameStateHolder) }; // 依存関係
        public void Initialize(ReferenceHolder referenceHolder)
        {
            // GameStateHolderの参照を取得
            gameStateHolder = referenceHolder.GetInitializable<GameStateHolder>();

            // 盤面の初期化
            InitiateBoard();
            // イベントの購読
            gameStateHolder.SubscribeToChangeToDuelPlayerOneWin(OnChangeToDuelPlayerOneWin);
            gameStateHolder.SubscribeToChangeToDuelPlayerTwoWin(OnChangeToDuelPlayerTwoWin);

            Debug.Log(BoardToString());
            Debug.Log(CapturedPiecesToString());
        }

        private void InitiateBoard()
        {
            // 盤面の初期化ロジックをここに実装
            // 例: 駒の配置など
            board[0, 0] = new Kyosya(PlayerType.PlayerOne);
            board[1, 0] = new Keima(PlayerType.PlayerOne);
            board[2, 0] = new Gin(PlayerType.PlayerOne);
            board[3, 0] = new Kin(PlayerType.PlayerOne);
            board[4, 0] = new King(PlayerType.PlayerOne);
            board[5, 0] = new Kin(PlayerType.PlayerOne);
            board[6, 0] = new Gin(PlayerType.PlayerOne);
            board[7, 0] = new Keima(PlayerType.PlayerOne);
            board[8, 0] = new Kyosya(PlayerType.PlayerOne);
            board[8, 8] = new Kyosya(PlayerType.PlayerTwo);
            board[7, 8] = new Keima(PlayerType.PlayerTwo);
            board[6, 8] = new Gin(PlayerType.PlayerTwo);
            board[5, 8] = new Kin(PlayerType.PlayerTwo);
            board[4, 8] = new King(PlayerType.PlayerTwo);
            board[3, 8] = new Kin(PlayerType.PlayerTwo);
            board[2, 8] = new Gin(PlayerType.PlayerTwo);
            board[1, 8] = new Keima(PlayerType.PlayerTwo);
            board[0, 8] = new Kyosya(PlayerType.PlayerTwo);
            for (int i = 0; i < 9; i++)
            {
                board[i, 2] = new Fuhyo(PlayerType.PlayerOne);
                board[i, 6] = new Fuhyo(PlayerType.PlayerTwo);
            }
            board[1, 1] = new Kakugyo(PlayerType.PlayerOne);
            board[7, 1] = new Hisya(PlayerType.PlayerOne);
            board[1, 7] = new Hisya(PlayerType.PlayerTwo);
            board[7, 7] = new Kakugyo(PlayerType.PlayerTwo);
        }

        //UIや他のスクリプトから呼び出すためのメソッド
        public IPiece GetPieceAt(int x, int y)
        {
            return board[x, y];
        }
        public Dictionary<PlayerType, IPiece> GetDuelPiece()
        {
            Dictionary<PlayerType, IPiece> duelPieces = new Dictionary<PlayerType, IPiece>();
            duelPieces[board[savedFromX, savedFromY].Player] = board[savedFromX, savedFromY];
            duelPieces[board[savedToX, savedToY].Player] = board[savedToX, savedToY];
            return duelPieces;
        }
        public IPiece[,] GetBoard()
        {
            return board;
        }
        public Dictionary<PieceType, int> GetCapturedPieces(PlayerType player)
        {
            Dictionary<PieceType, int> pieceCount = new Dictionary<PieceType, int>();
            foreach (var piece in capturedPieces[player])
            {
                if (pieceCount.ContainsKey(piece))
                {
                    pieceCount[piece]++;
                }
                else
                {
                    pieceCount[piece] = 1;
                }
            }
            return pieceCount;
        }

        // その他の将棋盤操作メソッドをここに追加
        public MoveResult MovePiece(int fromX, int fromY, int toX, int toY, PlayerType player)
        {
            if (!IsValidMove(fromX, fromY, toX, toY, player)) return MoveResult.InvalidMove;
            currentPlayer = player;

            if (IsDuel(fromX, fromY, toX, toY))
            {
                savedFromX = fromX;
                savedFromY = fromY;
                savedToX = toX;
                savedToY = toY;
                gameStateHolder.ChangeState(GameStateHolder.GameState.Duel);
                return MoveResult.DuelMove;
            }
            else
            {
                // 通常の移動処理
                board[toX, toY] = board[fromX, fromY];
                RemovePiece(fromX, fromY);
                // 成る判定
                if (IsPromotableMove(fromY, toY, board[toX, toY]))
                {
                    board[toX, toY].Promote();
                }

                // 移動後の盤面をログ出力
                Debug.Log("[ShogiBoard] Move performed: " + fromX + "," + fromY + " -> " + toX + "," + toY);
                Debug.Log(BoardToString());
                Debug.Log(CapturedPiecesToString());

                // ターン交代
                if (currentPlayer == PlayerType.PlayerOne)
                {
                    gameStateHolder.ChangeState(GameStateHolder.GameState.PlayerTwoTurn);
                }
                else
                {
                    gameStateHolder.ChangeState(GameStateHolder.GameState.PlayerOneTurn);
                }
            }
            return MoveResult.NormalMove;
        }

        public enum MoveResult
        {
            InvalidMove = -1,
            NormalMove = 1,
            DuelMove = 0
        }

        // 盤面をデバッグ用文字列に変換して返すユーティリティ
        private string BoardToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("  0 1 2 3 4 5 6 7 8");
            for (int y = 8; y >= 0; y--) // 上(8) -> 下(0) の順で表示
            {
                sb.Append(y).Append(" ");
                for (int x = 0; x < 9; x++)
                {
                    var p = board[x, y];
                    if (p == null)
                    {
                        sb.Append(". ");
                    }
                    else
                    {
                        // 表示ルール: 駒種類の先頭1文字を使用、PlayerOneは大文字、PlayerTwoは小文字
                        string t = p.Type.ToString();
                        char c = t.Length > 0 ? t[0] : '?';
                        if (p.Player == PlayerType.PlayerOne) sb.Append(char.ToUpper(c)).Append(" ");
                        else sb.Append(char.ToLower(c)).Append(" ");
                    }
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private string CapturedPiecesToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Captured Pieces:");
            foreach (var kv in capturedPieces)
            {
                var player = kv.Key;
                var list = kv.Value;
                sb.Append(player == PlayerType.PlayerOne ? "PlayerOne: " : "PlayerTwo: ");
                if (list.Count == 0)
                {
                    sb.AppendLine("None");
                    continue;
                }
                // カウント集計
                var counts = new Dictionary<PieceType, int>();
                foreach (var p in list)
                {
                    if (counts.ContainsKey(p)) counts[p]++;
                    else counts[p] = 1;
                }
                bool first = true;
                foreach (var c in counts)
                {
                    if (!first) sb.Append(", ");
                    sb.Append($"{c.Key}x{c.Value}");
                    first = false;
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public MoveResult SetPiece(int x, int y, PieceType piece, PlayerType player)
        {
            currentPlayer = player;
            IPiece pieceInstance = FindPieceFromPieceType(piece, player);
            Debug.Log("[ShogiBoard] Attempting to set piece: " + piece + " at " + x + "," + y + " for " + player);
            if (pieceInstance == null) return MoveResult.InvalidMove;
            Debug.Log("[ShogiBoard] Found piece: " + pieceInstance.Type + " for " + player);
            if (!IsCaptured(player, pieceInstance.Type)) return MoveResult.InvalidMove; // 持ち駒にない場合は置けない
            Debug.Log("[ShogiBoard] Setting piece: " + pieceInstance.Type + " at " + x + "," + y + " for " + player);
            if (!IsSettable(x, y, pieceInstance, player)) return MoveResult.InvalidMove;
            Debug.Log("[ShogiBoard] Settable confirmed.");
            board[x, y] = pieceInstance;
            capturedPieces[player].Remove(pieceInstance.Type);
            if (currentPlayer == PlayerType.PlayerOne)
            {
                gameStateHolder.ChangeState(GameStateHolder.GameState.PlayerTwoTurn);
            }
            else
            {
                gameStateHolder.ChangeState(GameStateHolder.GameState.PlayerOneTurn);
            }
            return MoveResult.NormalMove;
        }

        private IPiece FindPieceFromPieceType(PieceType pieceType, PlayerType player)
        {
            switch (pieceType)
            {
                case PieceType.King:
                    return new King(player);
                case PieceType.Kin:
                    return new Kin(player);
                case PieceType.Gin:
                    return new Gin(player);
                case PieceType.Kyosya:
                    return new Kyosya(player);
                case PieceType.Keima:
                    return new Keima(player);
                case PieceType.Fuhyo:
                    return new Fuhyo(player);
                case PieceType.Hisya:
                    return new Hisya(player);
                case PieceType.Kakugyo:
                    return new Kakugyo(player);
                default:
                    return null;
            }
        }

        private bool IsPromotableMove(int fromY, int toY, IPiece piece)
        {
            if (piece == null) return false;
            if (piece.Type == PieceType.King || piece.Type == PieceType.Kin) return false; // 王と金は成れない
            if (piece.Player == PlayerType.PlayerOne && (fromY >= 6 || toY >= 6)) return true; // プレイヤー1の成る条件
            if (piece.Player == PlayerType.PlayerTwo && (fromY <= 2 || toY <= 2)) return true; // プレイヤー2の成る条件
            return false;
        }

        private bool IsDuel(int fromX, int fromY, int toX, int toY)
        {
            return board[toX, toY] != null && board[toX, toY].Player != board[fromX, fromY].Player;
        }

        private bool IsCaptured(PlayerType player, PieceType pieceType)
        {
            return capturedPieces[player].Contains(pieceType);
        }

        private bool IsSettable(int x, int y, IPiece piece, PlayerType player)
        {
            Debug.Log("[ShogiBoard] Checking if settable at " + x + "," + y + " for piece " + piece.Type + " by " + player);
            if (board[x, y] != null) return false; // 既に駒がある場合は置けない
            Debug.Log("[ShogiBoard] No piece at target position.");
            if (IsNifu(x, player)) return false; // 二歩の判定
            Debug.Log("[ShogiBoard] Nifu check passed.");
            if (IsDeadEndPiece(x, y, piece)) return false; // 駒が詰む場合は置けない
            Debug.Log("[ShogiBoard] Dead end piece check passed.");
            return true;
        }

        private bool IsNifu(int x, PlayerType player)
        {
            // 二歩の判定ロジックをここに実装
            for (int y = 0; y < 9; y++)
            {
                if (board[x, y] != null && board[x, y].Type == PieceType.Fuhyo && board[x, y].Player == player)
                {
                    return true; // 同じ列に歩が既に存在する場合は二歩
                }
            }
            return false;
        }

        private bool IsPathClear(int fromX, int fromY, int toX, int toY)
        {
            int dx = toX - fromX;
            int dy = toY - fromY;

            // 水平・垂直・斜め以外の移動（跳び越す系）は経路チェック対象外
            if (!(dx == 0 || dy == 0 || Mathf.Abs(dx) == Mathf.Abs(dy)))
            {
                return true;
            }

            int steps = System.Math.Max(System.Math.Abs(dx), System.Math.Abs(dy));
            if (steps <= 1) return true;

            int stepX = dx == 0 ? 0 : (dx / System.Math.Abs(dx)); // -1,0,1
            int stepY = dy == 0 ? 0 : (dy / System.Math.Abs(dy)); // -1,0,1

            int cx = fromX + stepX;
            int cy = fromY + stepY;
            while (cx != toX || cy != toY)
            {
                if (board[cx, cy] != null) return false;
                cx += stepX;
                cy += stepY;
            }
            return true;
        }

        private bool IsDeadEndPiece(int x, int y, IPiece piece)
        {
            // 駒が詰むかどうかの判定ロジックをここに実装
            if (piece.Type == PieceType.Kyosya || piece.Type == PieceType.Keima || piece.Type == PieceType.Fuhyo)
            {
                if ((piece.Player == PlayerType.PlayerOne && y == 8) || (piece.Player == PlayerType.PlayerTwo && y == 0))
                {
                    return true; // 香車、桂馬、歩が最終列にいる場合は詰み
                }
            }
            if (piece.Type == PieceType.Keima)
            {
                if ((piece.Player == PlayerType.PlayerOne && y >= 7) || (piece.Player == PlayerType.PlayerTwo && y <= 1))
                {
                    return true; // 桂馬が最終二列にいる場合は詰み
                }
            }
            return false;
        }

        private bool IsValidMove(int fromX, int fromY, int toX, int toY, PlayerType player)
        {
            // 盤外チェック
            if (toX < 0 || toX >= 9 || toY < 0 || toY >= 9)
                return false;

            // from位置の駒チェック（null を先に）
            var piece = board[fromX, fromY];
            if (piece == null)
                return false;

            // 所有者チェック
            if (piece.Player != player)
                return false;

            // 目的地に自分の駒がある場合は不可
            if (board[toX, toY] != null && board[toX, toY].Player == piece.Player)
                return false;

            if (piece.Movement == null || piece.Movement.Length == 0)
                return false;

            // Movement が PlayerOne 視点（y が前方向で定義）だと仮定。
            // PlayerTwo の場合は y 成分を反転して判定する。
            foreach (var move in piece.Movement)
            {
                int dx = move[0];
                int dy = move[1];

                if (piece.Player == PlayerType.PlayerTwo)
                {
                    dy = -dy;
                }

                int newX = fromX + dx;
                int newY = fromY + dy;
                if (newX == toX && newY == toY)
                {
                    // 長距離移動（複数マス移動）の場合は経路の遮蔽をチェックする
                    // ただし桂馬は飛び越す駒なのでチェックしない
                    if (piece.Type != PieceType.Keima)
                    {
                        // 直線（縦・横・斜め）の長距離移動のみ経路チェック
                        if (Mathf.Abs(dx) > 1 || Mathf.Abs(dy) > 1)
                        {
                            if (!IsPathClear(fromX, fromY, newX, newY))
                                return false;
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        private void RemovePiece(int x, int y)
        {
            board[x, y] = null;
        }

        private void OnChangeToDuelPlayerOneWin()
        {
            if (currentPlayer == PlayerType.PlayerOne)
            {
                AddToCapturedPieces(PlayerType.PlayerOne, board[savedToX, savedToY].Type);
                board[savedToX, savedToY] = board[savedFromX, savedFromY];
                RemovePiece(savedFromX, savedFromY);
                if (IsPromotableMove(savedFromY, savedToY, board[savedToX, savedToY]))
                {
                    board[savedToX, savedToY].Promote();
                }

                // 移動後の盤面をログ出力
                Debug.Log("[ShogiBoard] Move performed: " + savedFromX + "," + savedFromY + " -> " + savedToX + "," + savedToY);
                Debug.Log(BoardToString());
                Debug.Log(CapturedPiecesToString());
            }
            else
            {
                AddToCapturedPieces(PlayerType.PlayerOne, board[savedFromX, savedFromY].Type);
                RemovePiece(savedFromX, savedFromY);

                // 移動後の盤面をログ出力
                Debug.Log("[ShogiBoard] Move performed: " + savedFromX + "," + savedFromY + " -> " + savedToX + "," + savedToY);
                Debug.Log(BoardToString());
                Debug.Log(CapturedPiecesToString());
            }

            if (capturedPieces[PlayerType.PlayerOne].Contains(PieceType.King))
            {
                // 王が取られた場合、ゲーム終了
                gameStateHolder.ChangeState(GameStateHolder.GameState.PlayerOneWin);
                return;
            }

            if (currentPlayer == PlayerType.PlayerOne)
                gameStateHolder.ChangeState(GameStateHolder.GameState.PlayerTwoTurn);
            else
                gameStateHolder.ChangeState(GameStateHolder.GameState.PlayerOneTurn);
        }

        private void OnChangeToDuelPlayerTwoWin()
        {
            if (currentPlayer == PlayerType.PlayerTwo)
            {
                AddToCapturedPieces(PlayerType.PlayerTwo, board[savedToX, savedToY].Type);
                board[savedToX, savedToY] = board[savedFromX, savedFromY];
                RemovePiece(savedFromX, savedFromY);
                if (IsPromotableMove(savedFromY, savedToY, board[savedToX, savedToY]))
                {
                    board[savedToX, savedToY].Promote();
                }

                // 移動後の盤面をログ出力
                Debug.Log("[ShogiBoard] Move performed: " + savedFromX + "," + savedFromY + " -> " + savedToX + "," + savedToY);
                Debug.Log(BoardToString());
                Debug.Log(CapturedPiecesToString());
            }
            else
            {
                AddToCapturedPieces(PlayerType.PlayerTwo, board[savedFromX, savedFromY].Type);
                RemovePiece(savedFromX, savedFromY);

                // 移動後の盤面をログ出力
                Debug.Log("[ShogiBoard] Move performed: " + savedFromX + "," + savedFromY + " -> " + savedToX + "," + savedToY);
                Debug.Log(BoardToString());
                Debug.Log(CapturedPiecesToString());
            }

            if (capturedPieces[PlayerType.PlayerTwo].Contains(PieceType.King))
            {
                // 王が取られた場合、ゲーム終了
                gameStateHolder.ChangeState(GameStateHolder.GameState.PlayerTwoWin);
                return;
            }

            if (currentPlayer == PlayerType.PlayerTwo)
                gameStateHolder.ChangeState(GameStateHolder.GameState.PlayerOneTurn);
            else
                gameStateHolder.ChangeState(GameStateHolder.GameState.PlayerTwoTurn);
        }

        private void AddToCapturedPieces(PlayerType player, PieceType pieceType)
        {
            capturedPieces[player].Add(pieceType);
        }

        private void OnDestroy()
        {
            // イベントの購読解除
            gameStateHolder.UnsubscribeFromChangeToDuelPlayerOneWin(OnChangeToDuelPlayerOneWin);
            gameStateHolder.UnsubscribeFromChangeToDuelPlayerTwoWin(OnChangeToDuelPlayerTwoWin);
        }
    }
}
