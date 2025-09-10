using UnityEngine;
using App.Main.ShogiThings;

namespace App.Main.GameMaster
{
    public class ShogiBoard : MonoBehaviour
    {
        [SerializeField] private GameStateHolder gameStateHolder;
        private IPiece[,] board = new IPiece[9, 9]; // 9x9の将棋盤を表す多次元配列

        private void Start()
        {
            // GameStateHolderの参照を取得
            if (gameStateHolder == null)
                gameStateHolder = FindFirstObjectByType<GameStateHolder>();

            // 盤面の初期化
            InitiateBoard();
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

        // その他の将棋盤操作メソッドをここに追加
        public void MovePiece(int fromX, int fromY, int toX, int toY)
        {
            if (!IsValidMove(fromX, fromY, toX, toY)) return;

            // 駒の移動
            RemovePiece(fromX, fromY);
            
        }

        private bool IsValidMove(int fromX, int fromY, int toX, int toY)
        {
            // 駒の移動が有効かどうかを判定するロジックをここに実装
            if (toX < 0 || toX >= 9 || toY < 0 || toY >= 9)
                return false; // 盤外への移動は無効
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
    }
}
