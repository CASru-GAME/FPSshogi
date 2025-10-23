using App.Common.Initialize;
using UnityEngine;
using App.Main.GameMaster;
using UnityEngine.InputSystem;

namespace App.Main.Player
{
    class PlayerManager : MonoBehaviour, IInitializable
    {
        public int InitializationPriority => 0;
        public System.Type[] Dependencies => new System.Type[] { typeof(GameStateHolder) };
        [SerializeField] private InputActionAsset inputActions;

        [SerializeField] private GameObject PlayerPrefab;
        public GameObject PlayerOne { get; private set; }
        public GameObject PlayerTwo { get; private set; }

        [SerializeField] private Vector3 PlayerOneSpawnPosition = new Vector3(-5f, 0f, 0f);
        [SerializeField] private Vector3 PlayerTwoSpawnPosition = new Vector3(5f, 0f, 0f);

        public void Initialize(ReferenceHolder referenceHolder)
        {
            var gameStateHolder = referenceHolder.GetInitializable<GameStateHolder>();
            PlayerOne = CreatePlayer(PlayerOneSpawnPosition);
            PlayerTwo = CreatePlayer(PlayerTwoSpawnPosition);
            gameStateHolder.SubscribeToChangeToDuel(OnChangedToDuel);
            EnableOnlyMap("Shogi");
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
            var playerScript = player.GetComponent<Player>();
            if (playerScript == null)
            {
                Debug.LogError("Player component not found on the instantiated prefab.");
                return null;
            }
            playerScript.Initialize();
            return player;
        }

        private void OnChangedToDuel()
        {
            EnableOnlyMap("Player");
        }

        /// <summary>
        /// すべての入力を切ってから指定した ActionMap 名だけを有効化する
        /// </summary>
        private void EnableOnlyMap(string mapName)
        {
            DisableAllInput();

            // InputActionAsset 側の ActionMap を有効化（asset を直接使っている場合）
            if (inputActions != null)
            {
                var map = inputActions.FindActionMap(mapName, true);
                map?.Enable();
            }

            PlayerOne.GetComponent<PlayerInput>().actions.FindActionMap(mapName, true)?.Enable();
            PlayerTwo.GetComponent<PlayerInput>().actions.FindActionMap(mapName, true)?.Enable();

            // 各 PlayerInput 側の ActionMap を有効化（PlayerInput 経由で使っている場合）   
            PlayerOne.GetComponent<PlayerInput>().ActivateInput();
            PlayerTwo.GetComponent<PlayerInput>().ActivateInput();
        }
        private void DisableAllInput()
        {
            // アセット単位で全無効化（通常これで十分）
            inputActions?.Disable();

            PlayerOne.GetComponent<PlayerInput>().DeactivateInput();
            PlayerTwo.GetComponent<PlayerInput>().DeactivateInput();

            PlayerOne.GetComponent<PlayerInput>().actions.Disable();
            PlayerTwo.GetComponent<PlayerInput>().actions.Disable();
        }
    }
}
