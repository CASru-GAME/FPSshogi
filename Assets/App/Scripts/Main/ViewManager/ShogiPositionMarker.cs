using UnityEngine;

namespace App.Main.ViewManager
{

    public class ShogiPositionMarker : MonoBehaviour
    {
        [SerializeField] public int[] position = new int[2]; // 盤面上の位置を表す配列（例: [x, y]）
    }
}
