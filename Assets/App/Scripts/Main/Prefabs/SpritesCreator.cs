using UnityEngine;
using App.Common.Initialize;
using App.Main.GameMaster;
using App.Main.ShogiThings;
using System.Collections.Generic;

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

        public int InitializationPriority => 80; // 優先度（低いほど先に初期化される）
        public System.Type[] Dependencies => new System.Type[] { typeof(ShogiBoard) }; // 依存関係
        public void Initialize(ReferenceHolder referenceHolder)
        {
            // 初期化処理
            InitiateUI();
        }

        private Dictionary<IPiece, GameObject> pieceOnBoard = new Dictionary<IPiece, GameObject>();
        private IPiece[,] previousBoardState = new IPiece[9, 9];


        Vector3 GetBoardCellPosition(int x, int y)
        {
            float cellSize = 5.225f;      // 1マスの大きさ
            float originX = 20.9f;     // board[0,0]のX座標
            float originY = 0.8706f;      // board[0,0]のY座標
            float originZ = -20.9f;        // board[0,0]のZ座標

            return new Vector3(originX - y * cellSize, originY , originZ + x * cellSize);
        }

        private void InitiateUI()
        {
            // ShogiBoardの参照を取得
            ShogiBoard shogiBoard = Object.FindFirstObjectByType<ShogiBoard>(); 
            IPiece[,] board = shogiBoard.GetBoard();

            // 盤面の初期化処理

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
                            prefab = (piece.Player == PlayerType.PlayerOne) ? ou : gyoku;

                        if (piece != null ) 
                        {
                            // 駒情報を使って処理
                            Vector3 position = GetBoardCellPosition(x, y);
                            float rotationZ = (piece.Player == PlayerType.PlayerOne) ? 90f : -90f;

                            GameObject pieceObject = Instantiate(prefab, position, Quaternion.Euler(-90, 0, rotationZ));
                            pieceOnBoard[piece] = pieceObject;

                        }
                    }
                }
            
        }


       private void changeUI()
        {
            // ShogiBoardの参照を取得
            ShogiBoard shogiBoard = Object.FindFirstObjectByType<ShogiBoard>(); 
            IPiece[,] currentBoardState = shogiBoard.GetBoard();

            for (int x = 0; x < 9; x++)
            {
                for (int y = 0; y < 9; y++)
                {
                    IPiece current = currentBoardState[x, y];
                    IPiece previous = previousBoardState[x, y];

                    if (current != previous)
                    {
                        // 駒が移動または変更された場合の処理
                        if (previous != null && pieceOnBoard.ContainsKey(previous))
                        {
                            // 前の位置に駒があった場合、その駒を削除
                            Destroy(pieceOnBoard[previous]);
                            pieceOnBoard.Remove(previous);
                        }

                        if (current != null)
                        {
                            // 新しい位置に駒がある場合、その駒を生成
                            GameObject prefab = null;
                            if (current is Kyosya)
                                prefab = kyosya;
                            else if (current is Keima)
                                prefab = keima;
                            else if (current is Gin)
                                prefab = gin;
                            else if (current is Kin)
                                prefab = kin;
                            else if (current is Kakugyo)
                                prefab = kakugyo;
                            else if (current is Hisya)
                                prefab = hisya;
                            else if (current is Fuhyo)
                                prefab = fuhyo;
                            else if (current is King)
                                prefab = (current.Player == PlayerType.PlayerOne) ? ou : gyoku;

                            Vector3 position = GetBoardCellPosition(x, y);
                            float rotationZ = (current.Player == PlayerType.PlayerOne) ? 90f : -90f;

                            GameObject pieceObject = Instantiate(prefab, position, Quaternion.Euler(-90, 0, rotationZ));
                            pieceOnBoard[current] = pieceObject;
                        }
                    }

                    if(current != null && previous != null && current == previous)
                    {
                        if(!previous.IsPromoted && current.IsPromoted)
                        {
                            // 駒が成った場合、ひっくり返す
                            pieceOnBoard[previous].transform.Rotate(180, 0, 0);
                        }
                    }
                }
            }

            // 現在の盤面状態を保存
            for (int x = 0; x < 9; x++)
            {
                for (int y = 0; y < 9; y++)
                {
                    previousBoardState[x, y] = currentBoardState[x, y];
                }
            }
        }

        void Update()
        {
            changeUI();
        }
    
    }
}
