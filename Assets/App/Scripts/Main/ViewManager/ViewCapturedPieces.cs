using UnityEngine;
using App.Common.Initialize;
using App.Main.GameMaster;
using App.Main.ShogiThings;
using System.Collections.Generic;
using System.Linq;

namespace App.Main.ViewManager
{
    public class ViewCapturedPieces : MonoBehaviour, IInitializable
    {
        public int InitializationPriority => 90; // 優先度（低いほど先に初期化される）
        public System.Type[] Dependencies => new System.Type[] { typeof(ViewPiece), typeof(ShogiBoard), typeof(GameStateHolder) }; // 依存関係
        private ViewPiece viewPiece = null;
        private ShogiBoard shogiBoard = null;
        private GameStateHolder gameStateHolder = null;
        [SerializeField] private GameObject playerOneCapturedPiecePositionMarkers;
        [SerializeField] private GameObject playerTwoCapturedPiecePositionMarkers;
        private Dictionary<PieceType, int> playerOneCapturedPieces = new Dictionary<PieceType, int>();
        private Dictionary<PieceType, int> playerTwoCapturedPieces = new Dictionary<PieceType, int>();
        private Dictionary<PieceType, GameObject> playerOneCapturedPieceObjects = new Dictionary<PieceType, GameObject>();
        private Dictionary<PieceType, GameObject> playerTwoCapturedPieceObjects = new Dictionary<PieceType, GameObject>();
        private Vector3[] playerOneCapturedPiecePosition;
        private Vector3[] playerTwoCapturedPiecePosition;
        public void Initialize(ReferenceHolder referenceHolder)
        {
            viewPiece = referenceHolder.GetInitializable<ViewPiece>();
            shogiBoard = referenceHolder.GetInitializable<ShogiBoard>();
            gameStateHolder = referenceHolder.GetInitializable<GameStateHolder>();
            gameStateHolder.SubscribeToChangeToPlayerOneTurn(OnChangedTurn);
            gameStateHolder.SubscribeToChangeToPlayerTwoTurn(OnChangedTurn);
            CreateCapturedPiecePositions();
        }

        private void OnChangedTurn()
        {
            // ターン変更時の処理をここに実装
            Debug.Log("CapturedPieceCounts:");
            foreach (var kvp in playerOneCapturedPieces)
            {
                Debug.Log($"Player One - PieceType: {kvp.Key}, Count: {kvp.Value}");
            }
            foreach (var kvp in playerTwoCapturedPieces)
            {
                Debug.Log($"Player Two - PieceType: {kvp.Key}, Count: {kvp.Value}");
            }
            UpdateCapturedPieceCounts();
            ShowCapturedPieces();
            HideUsedCapturedPieces();
        }
        public Vector3 GetCapturedPiecePosition(int pieceTypeIndex, PlayerType playerType)
        {
            if (playerType == PlayerType.PlayerOne)
            {
                return playerOneCapturedPiecePosition[pieceTypeIndex];
            }
            else
            {
                return playerTwoCapturedPiecePosition[pieceTypeIndex];
            }
        }

        private void CreateCapturedPiecePositions()
        {
            playerOneCapturedPiecePosition = new Vector3[playerOneCapturedPiecePositionMarkers.transform.childCount];
            for (int i = 0; i < playerOneCapturedPiecePositionMarkers.transform.childCount; i++)
            {
                GameObject cell = playerOneCapturedPiecePositionMarkers.transform.GetChild(i).gameObject;
                playerOneCapturedPiecePosition[i] = cell.transform.position;
            }

            playerTwoCapturedPiecePosition = new Vector3[playerTwoCapturedPiecePositionMarkers.transform.childCount];
            for (int i = 0; i < playerTwoCapturedPiecePositionMarkers.transform.childCount; i++)
            {
                GameObject cell = playerTwoCapturedPiecePositionMarkers.transform.GetChild(i).gameObject;
                playerTwoCapturedPiecePosition[i] = cell.transform.position;
            }
        }

        public GameObject GetPieceGameObject(PieceType pieceType, PlayerType playerType)
        {
            switch (pieceType)
            {
                case PieceType.Fuhyo:
                    return viewPiece.fuhyo;
                case PieceType.Kyosya:
                    return viewPiece.kyosya;
                case PieceType.Keima:
                    return viewPiece.keima;
                case PieceType.Gin:
                    return viewPiece.gin;
                case PieceType.Kin:
                    return viewPiece.kin;
                case PieceType.Kakugyo:
                    return viewPiece.kakugyo;
                case PieceType.Hisya:
                    return viewPiece.hisya;
                case PieceType.King:
                    return (playerType == PlayerType.PlayerOne) ? viewPiece.ou : viewPiece.gyoku;
                default:
                    return null;
            }
        }

        private void UpdateCapturedPieceCounts()
        {
            // 駒の捕獲数を更新するロジックをここに実装
            // 例: playerOneCapturedPieces[PieceType.Fuhyo] = 2;
            playerOneCapturedPieces = shogiBoard.GetCapturedPieces(PlayerType.PlayerOne);
            playerTwoCapturedPieces = shogiBoard.GetCapturedPieces(PlayerType.PlayerTwo);
            Debug.Log("playerOneCapturedPieces count: " + playerOneCapturedPieces.Count);
            Debug.Log("playerTwoCapturedPieces count: " + playerTwoCapturedPieces.Count);
        }

        private void ShowCapturedPieces()
        {
            foreach (PieceType pieceType in playerOneCapturedPieces.Keys)
            {
                int count = playerOneCapturedPieces[pieceType];
                for (int j = 0; j < count; j++)
                {
                    if (playerOneCapturedPieceObjects.ContainsKey(pieceType)) continue;
                    Vector3 position = GetCapturedPiecePosition((int)pieceType, PlayerType.PlayerOne);
                    GameObject pieceObject = Instantiate(GetPieceGameObject(pieceType, PlayerType.PlayerOne), position, Quaternion.identity);
                    // 駒の向きを調整
                    pieceObject.transform.rotation = Quaternion.Euler(-90, -90, 0);
                    playerOneCapturedPieceObjects[pieceType] = pieceObject;
                    Debug.Log("playerOneCapturedPieceObjects: " + playerOneCapturedPieceObjects.Count);
                }
            }
            foreach (PieceType pieceType in playerTwoCapturedPieces.Keys)
            {
                int count = playerTwoCapturedPieces[pieceType];
                for (int j = 0; j < count; j++)
                {
                    if (playerTwoCapturedPieceObjects.ContainsKey(pieceType)) continue;
                    Vector3 position = GetCapturedPiecePosition((int)pieceType, PlayerType.PlayerTwo);
                    GameObject pieceObject = Instantiate(GetPieceGameObject(pieceType, PlayerType.PlayerTwo), position, Quaternion.identity);
                    // 駒の向きを調整
                    pieceObject.transform.rotation = Quaternion.Euler(-90, -90, 0);
                    playerTwoCapturedPieceObjects[pieceType] = pieceObject;
                    Debug.Log("playerTwoCapturedPieceObjects: " + playerTwoCapturedPieceObjects.Count);
                }
            }
        }

        private void HideUsedCapturedPieces()
        {
            Debug.Log("Hiding used captured pieces...");
            // 辞書の値を順に破棄する（安全）
            foreach (PieceType pieceType in playerOneCapturedPieceObjects.Keys.ToList())
            {
                Debug.Log("Checking captured piece: " + pieceType);
                Debug.Log("Count in capturedPieces: " + (playerOneCapturedPieces.ContainsKey(pieceType) ? playerOneCapturedPieces[pieceType].ToString() : "0"));
                if (!playerOneCapturedPieces.ContainsKey(pieceType)|| playerOneCapturedPieces[pieceType] == 0)
                {
                    Debug.Log("Destroying captured piece: " + pieceType);
                    var pieceObject = playerOneCapturedPieceObjects[pieceType];
                    if (pieceObject != null) Destroy(pieceObject);
                    playerOneCapturedPieceObjects.Remove(pieceType);
                }
            }
            foreach (PieceType pieceType in playerTwoCapturedPieces.Keys.ToList())
            {
                Debug.Log("Checking captured piece: " + pieceType);
                if (!playerTwoCapturedPieces.ContainsKey(pieceType)|| playerTwoCapturedPieces[pieceType] == 0)
                {
                    Debug.Log("Destroying captured piece: " + pieceType);
                    var pieceObject = playerTwoCapturedPieceObjects[pieceType];
                    if (pieceObject != null) Destroy(pieceObject);
                    playerTwoCapturedPieceObjects.Remove(pieceType);
                }
            }
        }
    }
}
