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

        public int PlayerIndexPlayerOne = 0;
        public int PlayerIndexPlayerTwo = 1;

        private GameStateHolder gameStateHolder;
        [SerializeField] GameObject PlayerOneSpawnPositionMarker;
        [SerializeField] GameObject PlayerTwoSpawnPositionMarker;
        private Vector3 PlayerOneSpawnPosition = new Vector3(-5f, 0f, 0f);
        private Vector3 PlayerTwoSpawnPosition = new Vector3(5f, 0f, 0f);
        private Quaternion PlayerOneSpawnRotation = Quaternion.Euler(0f, 90f, 0f);
        private Quaternion PlayerTwoSpawnRotation = Quaternion.Euler(0f, -90f, 0f);

        public void Initialize(ReferenceHolder referenceHolder)
        {
            gameStateHolder = referenceHolder.GetInitializable<GameStateHolder>();
            SetSpawnPositions();
            PlayerOne = CreatePlayer(PlayerOneSpawnPosition, PlayerOneSpawnRotation);
            PlayerTwo = CreatePlayer(PlayerTwoSpawnPosition, PlayerTwoSpawnRotation);
            PlayerIndexPlayerOne = PlayerOne.GetComponent<PlayerInput>().playerIndex;
            PlayerIndexPlayerTwo = PlayerTwo.GetComponent<PlayerInput>().playerIndex;
            DisablePlayerCamera();
            gameStateHolder.SubscribeToChangeToDuel(OnChangedToDuel);
            gameStateHolder.SubscribeToExitDuel(OnExitDuel);
            EnableOnlyMap("Shogi");
        }

        private void SetSpawnPositions()
        {
            if (PlayerOneSpawnPositionMarker != null)
            {
                PlayerOneSpawnPosition = PlayerOneSpawnPositionMarker.transform.position;
                float y = PlayerOneSpawnPositionMarker.transform.eulerAngles.y;
                PlayerOneSpawnRotation = Quaternion.Euler(0f, y, 0f);
            }
            if (PlayerTwoSpawnPositionMarker != null)
            {
                PlayerTwoSpawnPosition = PlayerTwoSpawnPositionMarker.transform.position;
                float y = PlayerTwoSpawnPositionMarker.transform.eulerAngles.y;
                PlayerTwoSpawnRotation = Quaternion.Euler(0f, y, 0f);
            }
        }

        private GameObject CreatePlayer(Vector3 spawnPosition, Quaternion spawnRotation)
        {
            if (PlayerPrefab == null)
            {
                Debug.LogError("PlayerPrefab is not assigned in the inspector.");
                return null;
            }
            GameObject player = Instantiate(PlayerPrefab, spawnPosition, spawnRotation);
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
            EnablePlayerCamera();
            CursorDisable();
        }

        private void OnExitDuel()
        {
            EnableOnlyMap("Shogi");
            DisablePlayerCamera();
            CursorEnable();
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

        private void CursorEnable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void CursorDisable()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void DisablePlayerCamera()
        {
            PlayerOne.GetComponentInChildren<Camera>().enabled = false;
            PlayerTwo.GetComponentInChildren<Camera>().enabled = false;
        }

        private void EnablePlayerCamera()
        {
            PlayerOne.GetComponentInChildren<Camera>().enabled = true;
            PlayerTwo.GetComponentInChildren<Camera>().enabled = true;
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

        public void OnDestroy()
        {
            // クリーンアップ処理をここに記述
            gameStateHolder.UnsubscribeFromChangeToDuel(OnChangedToDuel);
        }
    }
}
