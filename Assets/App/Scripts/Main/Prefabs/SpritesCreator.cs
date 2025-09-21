using UnityEngine;
using App.Common.Initialize;
using App.Main.GameMaster;
using App.Main.ShogiThings;

namespace App.Main.Prefabs
{

    public class SpritesCreator : MonoBehaviour, IInitializable
    {
        public GameObject fuhyo;
        public GameObject kyosya;
        public GameObject keima;
        public GameObject gin; 
        public GameObject kin;
        public GameObject kakugyo;
        public GameObject hisya;
        public GameObject ou;
        public GameObject gyoku;

        public GameObject board;

        public int InitializationPriority => 80; // 優先度（低いほど先に初期化される）
        public System.Type[] Dependencies => new System.Type[] { typeof(ShogiBoard) }; // 依存関係
        public void Initialize(ReferenceHolder referenceHolder)
        {
            // 初期化処理
            InitiateBoard();

        }


        Vector3 GetBoardCellPosition(int x, int y)
        {
            float cellSize = 5.225f;      // 1マスの大きさ
            float originX = 20.9f;     // board[0,0]のX座標
            float originY = 0.8706f;      // board[0,0]のY座標
            float originZ = -20.9f;        // board[0,0]のZ座標

            return new Vector3(originX - y * cellSize, originY , originZ + x * cellSize);
        }

        private void InitiateBoard()
        {
            // 盤面の初期化処理

            // ShogiBoardの参照を取得
            ShogiBoard shogiBoard = FindObjectOfType<ShogiBoard>();
            IPiece[,] board = shogiBoard.GetBoard();
            

                for(int x = 0; x < 9; x++) 
                { 
                    for (int y = 0; y < 9; y++) 
                    {
                        IPiece piece = board[x, y];
                        if (piece == null) continue;

                        GameObject prefab = null;
                        if (piece is Kyosya)
                            prefab = kyosya;
                        else if (piece is Keima)
                        prefab = keima;
                        else if (piece is Gin)
                            prefab = gin;
                        else if (piece is Kin)
                            prefab = kin;
                        else if (piece is Kakugyo)
                            prefab = kakugyo;
                        else if (piece is Hisya)
                            prefab = hisya;
                        else if (piece is Fuhyo)
                            prefab = fuhyo;
                        else if (piece is King)
                        {
                            if (piece.Player == PlayerType.PlayerOne)
                                prefab = ou;
                            else
                                prefab = gyoku;
                        }

                        if (piece != null ) 
                        {
                            // 駒情報を使って処理
                            Vector3 position = GetBoardCellPosition(x, y);
                            if(board[x,y].Player == PlayerType.PlayerOne)
                            {
                                Instantiate(prefab, position, Quaternion.Euler(-90,0,90));
                            }
                            else
                            {
                                Instantiate(prefab, position, Quaternion.Euler(-90,0,-90));
                            }

                        }
                    }
                }
            
        }
    }
}