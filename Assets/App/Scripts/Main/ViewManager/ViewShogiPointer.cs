using UnityEngine;
using App.Common.Initialize;
using App.Main.ViewManager;

namespace App.Main.Controller
{
    public class ViewShogiPointer : MonoBehaviour, IInitializable
    {
        [SerializeField] private GameObject pointerPrefab;
        private GameObject pointerInstance;
        private ViewPiece viewPiece;
        [SerializeField] private float pointerHeight = 1;

        System.Type[] IInitializable.Dependencies => new System.Type[] { typeof(ViewPiece) };
        int IInitializable.InitializationPriority => 200;

        public void Initialize(ReferenceHolder referenceHolder)
        {
            // ポインターのインスタンスを生成
            pointerInstance = Instantiate(pointerPrefab, new Vector3(0, pointerHeight, 0), Quaternion.identity);
            viewPiece = referenceHolder.GetInitializable<ViewPiece>();
        }

        public void ChangePointerPosition(int[] position)
        {
            if (viewPiece == null || pointerInstance == null) return;

            Vector3 targetPosition = viewPiece.boardCellPositions[position[0], position[1]];

            // 現在の高さ（y値）を維持する
            float keepHeight = pointerInstance.transform.position.y;
            targetPosition.y = keepHeight;

            pointerInstance.transform.position = targetPosition;
        }
    }
}
