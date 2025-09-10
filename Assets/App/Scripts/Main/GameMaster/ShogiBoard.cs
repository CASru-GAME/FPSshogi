using UnityEngine;
using App.Main.GameMaster;

namespace App.Main.GameMaster
{
    public class ShogiBoard : MonoBehaviour
    {
        [SerializeField] private GameStateHolder gameStateHolder;

        private void Start()
        {
            // GameStateHolderの参照を取得
            if (gameStateHolder == null)
                gameStateHolder = FindFirstObjectByType<GameStateHolder>();
        }

    }
}
