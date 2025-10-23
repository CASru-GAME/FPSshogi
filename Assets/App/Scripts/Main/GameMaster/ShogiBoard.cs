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
        public void MovePiece(int fromX, int fromY, int toX, int toY, PlayerType player)
        {
            if (!IsValidMove(fromX, fromY, toX, toY, player)) return;
            currentPlayer = player;

            if (IsDuel(fromX, fromY, toX, toY))
            {
                savedFromX = fromX;
                savedFromY = fromY;
                savedToX = toX;
                savedToY = toY;
                gameStateHolder.ChangeState(GameStateHolder.GameState.Duel);
                return;
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

        public void SetPiece(int x, int y, IPiece piece, PlayerType player)
        {
            if (!IsCaptured(player, piece.Type)) return; // 持ち駒にない場合は置けない
            if (!IsSettable(x, y, piece, player)) return;
            board[x, y] = piece;
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
            if (board[x, y] != null) return false; // 既に駒がある場合は置けない
            if (IsNifu(x, player)) return false; // 二歩の判定
            if (IsDeadEndPiece(x, y, piece)) return false; // 駒が詰む場合は置けない
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
            // 駒の移動が有効かどうかを判定するロジックをここに実装
            if (toX < 0 || toX >= 9 || toY < 0 || toY >= 9)
                return false; // 盤外への移動は無効
            if (board[fromX, fromY].Player != player)
                return false; // 自分の駒でない場合は無効
            if (board[toX, toY] != null && board[toX, toY].Player == board[fromX, fromY].Player)
                return false; // 自分の駒がある場所への移動は無効
            if (board[fromX, fromY] == null)
                return false; // 駒が存在しない場合は無効
            if (board[fromX, fromY].Movement == null || board[fromX, fromY].Movement.Length == 0)
                return false; // 駒の移動パターンが定義されていない場合は無効
            if (board[fromX, fromY].Movement.Length > 0)
            {
                foreach (var move in board[fromX, fromY].Movement)
                {
                    int newX = fromX + move[0];
                    int newY = fromY + move[1];
                    if (newX == toX && newY == toY)
                        return true; // 有効な移動パターンに一致
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

            gameStateHolder.ChangeState(GameStateHolder.GameState.PlayerTwoTurn);
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

            gameStateHolder.ChangeState(GameStateHolder.GameState.PlayerOneTurn);
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
