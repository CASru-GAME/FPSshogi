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
        [SerializeField] public Vector3 PlayerOneSpawnPosition;
        [SerializeField] public Vector3 PlayerTwoSpawnPosition;
        public GameObject PlayerOne { get; private set; }
        public GameObject PlayerTwo { get; private set; }
        public void Initialize(ReferenceHolder referenceHolder)
        {
            shogiBoard = referenceHolder.GetInitializable<ShogiBoard>();
            gameStateHolder = referenceHolder.GetInitializable<GameStateHolder>();

            gameStateHolder.SubscribeToChangeToDuel(OnChangeToDuel);
        }

        private void OnChangeToDuel()
        {
            PlayerOne = CreatePlayer(PlayerOneSpawnPosition);
            PlayerTwo = CreatePlayer(PlayerTwoSpawnPosition);
        }

        public void ChangeStateToPlayerOneWin()
        {
            gameStateHolder.ChangeState(GameStateHolder.GameState.PlayerOneWin);
        }

        public void ChangeStateToPlayerTwoWin()
        {
            gameStateHolder.ChangeState(GameStateHolder.GameState.PlayerTwoWin);
        }

        private GameObject CreatePlayer(Vector3 spawnPosition)
        {
            if (PlayerPrefab == null)
            {
                Debug.LogError("PlayerPrefab is not assigned in the inspector.");
                return null;
            }
            GameObject player = Instantiate(PlayerPrefab, spawnPosition, Quaternion.identity);
            if (player == null)
            {
                Debug.LogError("Failed to instantiate PlayerPrefab.");
                return null;
            }
            var playerScript = player.GetComponent<Player.Player>();
            if (playerScript == null)
            {
                Debug.LogError("Player component not found on the instantiated prefab.");
                return null;
            }
            playerScript.Initialize();
            return player;
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
