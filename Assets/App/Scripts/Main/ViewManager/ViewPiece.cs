using UnityEngine;
using App.Common.Initialize;
using App.Main.GameMaster;
using App.Main.ShogiThings;
using System.Collections.Generic;

namespace App.Main.ViewManager
{

    public class ViewPiece : MonoBehaviour, IInitializable
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

        [SerializeField] private GameObject boardCellPositionMarkers;
        private Vector3[,] boardCellPositions = new Vector3[9, 9];

        public int InitializationPriority => 80; // 優先度（低いほど先に初期化される）
        public System.Type[] Dependencies => new System.Type[] { typeof(ShogiBoard) }; // 依存関係
        private ShogiBoard shogiBoard = null;
        public void Initialize(ReferenceHolder referenceHolder)
        {
            // ShogiBoardの参照を取得
            shogiBoard = referenceHolder.GetInitializable<ShogiBoard>();

            CreateBoardCellPositions();
            // 初期化処理
            SetPieces();
        }

        private GameObject[,] pieceObjects = new GameObject[9, 9];
        private IPiece[,] previousBoardState = new IPiece[9, 9];

        private Vector3 GetBoardCellPosition(int x, int y)
        {
            return boardCellPositions[x, y];
        }

        public void CreateBoardCellPositions()
        {
            for (int i = 0; i < boardCellPositionMarkers.transform.childCount; i++)
            {
                for (int j = 0; j < boardCellPositionMarkers.transform.GetChild(i).childCount; j++)
                {
                    GameObject cell = boardCellPositionMarkers.transform.GetChild(i).GetChild(j).gameObject;
                    boardCellPositions[cell.GetComponent<ShogiPositionMarker>().position[0], cell.GetComponent<ShogiPositionMarker>().position[1]] = cell.transform.position;
                }
            }
        }

        private void SetPieces()
        {
            IPiece[,] board = shogiBoard.GetBoard();
            // 盤面の初期化処理

            for (int x = 0; x < 9; x++)
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

                    if (prefab == null) continue;

                    Vector3 position = GetBoardCellPosition(x, y);
                    float rotationZ = (piece.Player == PlayerType.PlayerOne) ? 0f : -180f;

                    GameObject pieceObject = Instantiate(prefab, position, Quaternion.Euler(-90, 0, rotationZ));
                    pieceObjects[x, y] = pieceObject;
                }
            }

            // 現在の盤面状態を保存
            for (int x = 0; x < 9; x++)
                for (int y = 0; y < 9; y++)
                    previousBoardState[x, y] = board[x, y];
        }


        private void changeUI()
        {
            IPiece[,] currentBoardState = shogiBoard.GetBoard();

            for (int x = 0; x < 9; x++)
            {
                for (int y = 0; y < 9; y++)
                {
                    IPiece current = currentBoardState[x, y];
                    IPiece previous = previousBoardState[x, y];

                    if (current != previous)
                    {
                        // 以前の位置にオブジェクトがあれば削除
                        if (pieceObjects[x, y] != null)
                        {
                            Destroy(pieceObjects[x, y]);
                            pieceObjects[x, y] = null;
                        }

                        if (current != null)
                        {
                            // 新しい位置に駒を生成
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
                            float rotationZ = (current.Player == PlayerType.PlayerOne) ? 0f : -180f;

                            GameObject pieceObject = Instantiate(prefab, position, Quaternion.Euler(-90, 0, rotationZ));
                            pieceObjects[x, y] = pieceObject;
                        }
                    }
                    else if (current != null && previous != null)
                    {
                        // 成り判定：前は成っていなくて今成っているなら駒をひっくり返す
                        if (!previous.IsPromoted && current.IsPromoted && pieceObjects[x, y] != null)
                        {
                            pieceObjects[x, y].transform.Rotate(180, 0, 0);
                        }
                    }
                }
            }

            // 現在の盤面状態を保存
            for (int x = 0; x < 9; x++)
                for (int y = 0; y < 9; y++)
                    previousBoardState[x, y] = currentBoardState[x, y];
        }

        void Update()
        {
            changeUI();
        }

    }
}
