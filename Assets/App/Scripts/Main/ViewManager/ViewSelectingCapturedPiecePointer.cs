using UnityEngine;
using App.Common.Initialize;
using App.Main.ShogiThings;

namespace App.Main.ViewManager
{
    public class ViewSelectingCapturedPiecePointer : MonoBehaviour, IInitializable
    {
        [SerializeField] private GameObject pointerPrefab;
        private GameObject pointerInstance;
        public int InitializationPriority => 150; // 優先度（低いほど先に初期化される）
        public System.Type[] Dependencies => new System.Type[] { typeof(ViewCapturedPieces) }; // 依存関係

        private ViewCapturedPieces viewCapturedPieces;

        [SerializeField] private float pointerHeight = 1;

        public void Initialize(ReferenceHolder referenceHolder)
        {
            // ポインターのインスタンスを生成
            pointerInstance = Instantiate(pointerPrefab, new Vector3(0, pointerHeight, 0), Quaternion.identity);
            viewCapturedPieces = referenceHolder.GetInitializable<ViewCapturedPieces>();
        }

        public void ChangePointerPosition(int pieceTypeIndex, PlayerType playerType)
        {
            if (viewCapturedPieces == null) return;
            Vector3 targetPosition = viewCapturedPieces.GetCapturedPiecePosition(pieceTypeIndex, playerType);
            // 現在の高さ（y値）を維持する
            float keepHeight = pointerInstance.transform.position.y;
            targetPosition.y = keepHeight;
            pointerInstance.transform.position = targetPosition;
        }
    }
}
