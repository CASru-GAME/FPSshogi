using UnityEngine;
using App.Common.Initialize;

namespace App.Main.GameMaster
{
    public class DuelManager : MonoBehaviour, IInitializable
    {
        private ShogiBoard shogiBoard = null;
        private GameStateHolder gameStateHolder = null;
        public int InitializationPriority => 0;
        public System.Type[] Dependencies => new System.Type[] { typeof(ShogiBoard), typeof(GameStateHolder) };
        [SerializeField] public GameObject PlayerPrefab;
        public void Initialize(ReferenceHolder referenceHolder)
        {
            Debug.Log("DuelManager initialized.");
            shogiBoard = referenceHolder.GetInitializable<ShogiBoard>();
            gameStateHolder = referenceHolder.GetInitializable<GameStateHolder>();

            gameStateHolder.SubscribeToChangeToDuel(OnChangeToDuel);
        }

        private void OnChangeToDuel()
        {

        }

        public void ChangeStateToPlayerOneWin()
        {
            gameStateHolder.ChangeState(GameStateHolder.GameState.PlayerOneWin);
        }

        public void ChangeStateToPlayerTwoWin()
        {
            gameStateHolder.ChangeState(GameStateHolder.GameState.PlayerTwoWin);
        }

        private void CreatePlayer()
        {
            if (PlayerPrefab == null)
            {
                Debug.LogError("PlayerPrefab is not assigned in the inspector.");
                return;
            }
            GameObject player = Instantiate(PlayerPrefab, Vector3.zero, Quaternion.identity);
            if (player == null)
            {
                Debug.LogError("Failed to instantiate PlayerPrefab.");
                return;
            }
            var playerScript = player.GetComponent<Player.Player>();
            if (playerScript == null)
            {
                Debug.LogError("Player component not found on the instantiated prefab.");
                return;
            }
            playerScript.Initialize();
        }

        public void OnDestroy()
        {
            if (gameStateHolder != null)
            {
                gameStateHolder.UnsubscribeFromChangeToDuel(OnChangeToDuel);
            }
        }
    }
}
