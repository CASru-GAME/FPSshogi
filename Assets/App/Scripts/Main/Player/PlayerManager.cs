using App.Common.Initialize;
using UnityEngine;
using App.Main.GameMaster;
using UnityEngine.InputSystem;
using App.Main.ShogiThings;

namespace App.Main.Player
{
    public class PlayerManager : MonoBehaviour, IInitializable
    {
        public int InitializationPriority => 0;
        public System.Type[] Dependencies => new System.Type[] { typeof(GameStateHolder), typeof(ShogiBoard) };
        [SerializeField] private InputActionAsset inputActions;

        [SerializeField] private GameObject PlayerPrefab;
        public GameObject PlayerOne { get; private set; }
        public GameObject PlayerTwo { get; private set; }

        public int PlayerIndexPlayerOne = 0;
        public int PlayerIndexPlayerTwo = 1;

        private GameStateHolder gameStateHolder;
        private ShogiBoard shogiBoard;
        [SerializeField] GameObject PlayerOneSpawnPositionMarker;
        [SerializeField] GameObject PlayerTwoSpawnPositionMarker;
        private Vector3 PlayerOneSpawnPosition = new Vector3(-5f, 0f, 0f);
        private Vector3 PlayerTwoSpawnPosition = new Vector3(5f, 0f, 0f);
        private Quaternion PlayerOneSpawnRotation = Quaternion.Euler(0f, 90f, 0f);
        private Quaternion PlayerTwoSpawnRotation = Quaternion.Euler(0f, -90f, 0f);
        [SerializeField] private StatusParameter fuhyoStatusParameter;
        [SerializeField] private StatusParameter kyosyaStatusParameter;
        [SerializeField] private StatusParameter keimaStatusParameter;
        [SerializeField] private StatusParameter ginStatusParameter;
        [SerializeField] private StatusParameter kinStatusParameter;
        [SerializeField] private StatusParameter kakugyoStatusParameter;
        [SerializeField] private StatusParameter hisyaStatusParameter;
        [SerializeField] private StatusParameter kingStatusParameter;
        [SerializeField] private ISkill fuhyoSkill;
        [SerializeField] private ISkill kyosyaSkill;
        [SerializeField] private ISkill keimaSkill;
        [SerializeField] private ISkill ginSkill;
        [SerializeField] private ISkill kinSkill;
        [SerializeField] private ISkill kakugyoSkill;
        [SerializeField] private ISkill hisyaSkill;
        [SerializeField] private ISkill kingSkill;
        [SerializeField] private IPrimaryAction fuhyoPrimaryAction;
        [SerializeField] private IPrimaryAction kyosyaPrimaryAction;
        [SerializeField] private IPrimaryAction keimaPrimaryAction;
        [SerializeField] private IPrimaryAction ginPrimaryAction;
        [SerializeField] private IPrimaryAction kinPrimaryAction;
        [SerializeField] private IPrimaryAction kakugyoPrimaryAction;
        [SerializeField] private IPrimaryAction hisyaPrimaryAction;
        [SerializeField] private IPrimaryAction kingPrimaryAction;
        [SerializeField] private ISecondaryAction fuhyoSecondaryAction;
        [SerializeField] private ISecondaryAction kyosyaSecondaryAction;
        [SerializeField] private ISecondaryAction keimaSecondaryAction;
        [SerializeField] private ISecondaryAction ginSecondaryAction;
        [SerializeField] private ISecondaryAction kinSecondaryAction;
        [SerializeField] private ISecondaryAction kakugyoSecondaryAction;
        [SerializeField] private ISecondaryAction hisyaSecondaryAction;
        [SerializeField] private ISecondaryAction kingSecondaryAction;
        [SerializeField] private GameObject fuhyoPieceObject;
        [SerializeField] private GameObject kyosyaPieceObject;
        [SerializeField] private GameObject keimaPieceObject;
        [SerializeField] private GameObject ginPieceObject;
        [SerializeField] private GameObject kinPieceObject;
        [SerializeField] private GameObject kakugyoPieceObject;
        [SerializeField] private GameObject hisyaPieceObject;
        [SerializeField] private GameObject kingPieceObject;

        public void Initialize(ReferenceHolder referenceHolder)
        {
            gameStateHolder = referenceHolder.GetInitializable<GameStateHolder>();
            shogiBoard = referenceHolder.GetInitializable<ShogiBoard>();
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
            SetPlayerCondition();
        }

        private void OnExitDuel()
        {
            EnableOnlyMap("Shogi");
            DisablePlayerCamera();
            CursorEnable();
        }

        private void SetPlayerCondition()
        {
            IPiece pieceTypePlayerOne = shogiBoard.GetDuelPiece()[PlayerType.PlayerOne];
            IPiece pieceTypePlayerTwo = shogiBoard.GetDuelPiece()[PlayerType.PlayerTwo];

            switch (pieceTypePlayerOne.Type)
            {
                case PieceType.Fuhyo:
                    PlayerOne.GetComponent<Player>().SetPlayerStatus(fuhyoStatusParameter.CreatePlayerStatus(pieceTypePlayerOne.IsPromoted));
                    PlayerOne.GetComponent<Player>().SetSecondaryAction(fuhyoSecondaryAction);
                    PlayerOne.GetComponent<Player>().SetSkill(fuhyoSkill);
                    PlayerOne.GetComponent<Player>().SetPrimaryAction(fuhyoPrimaryAction);
                    break;
                case PieceType.Kyosya:
                    PlayerOne.GetComponent<Player>().SetPlayerStatus(kyosyaStatusParameter.CreatePlayerStatus(pieceTypePlayerOne.IsPromoted));
                    PlayerOne.GetComponent<Player>().SetSecondaryAction(kyosyaSecondaryAction);
                    PlayerOne.GetComponent<Player>().SetSkill(kyosyaSkill);
                    PlayerOne.GetComponent<Player>().SetPrimaryAction(kyosyaPrimaryAction);
                    break;
                case PieceType.Keima:
                    PlayerOne.GetComponent<Player>().SetPlayerStatus(keimaStatusParameter.CreatePlayerStatus(pieceTypePlayerOne.IsPromoted));
                    PlayerOne.GetComponent<Player>().SetSecondaryAction(keimaSecondaryAction);
                    PlayerOne.GetComponent<Player>().SetSkill(keimaSkill);
                    PlayerOne.GetComponent<Player>().SetPrimaryAction(keimaPrimaryAction);
                    break;
                case PieceType.Gin:
                    PlayerOne.GetComponent<Player>().SetPlayerStatus(ginStatusParameter.CreatePlayerStatus(pieceTypePlayerOne.IsPromoted));
                    PlayerOne.GetComponent<Player>().SetSecondaryAction(ginSecondaryAction);
                    PlayerOne.GetComponent<Player>().SetSkill(ginSkill);
                    PlayerOne.GetComponent<Player>().SetPrimaryAction(ginPrimaryAction);
                    break;
                case PieceType.Kin:
                    PlayerOne.GetComponent<Player>().SetPlayerStatus(kinStatusParameter.CreatePlayerStatus(pieceTypePlayerOne.IsPromoted));
                    PlayerOne.GetComponent<Player>().SetSecondaryAction(kinSecondaryAction);
                    PlayerOne.GetComponent<Player>().SetSkill(kinSkill);
                    PlayerOne.GetComponent<Player>().SetPrimaryAction(kinPrimaryAction);
                    break;
                case PieceType.Kakugyo:
                    PlayerOne.GetComponent<Player>().SetPlayerStatus(kakugyoStatusParameter.CreatePlayerStatus(pieceTypePlayerOne.IsPromoted));
                    PlayerOne.GetComponent<Player>().SetSecondaryAction(kakugyoSecondaryAction);
                    PlayerOne.GetComponent<Player>().SetSkill(kakugyoSkill);
                    PlayerOne.GetComponent<Player>().SetPrimaryAction(kakugyoPrimaryAction);
                    break;
                case PieceType.Hisya:
                    PlayerOne.GetComponent<Player>().SetPlayerStatus(hisyaStatusParameter.CreatePlayerStatus(pieceTypePlayerOne.IsPromoted));
                    PlayerOne.GetComponent<Player>().SetSecondaryAction(hisyaSecondaryAction);
                    PlayerOne.GetComponent<Player>().SetSkill(hisyaSkill);
                    PlayerOne.GetComponent<Player>().SetPrimaryAction(hisyaPrimaryAction);
                    break;
                case PieceType.King:
                    PlayerOne.GetComponent<Player>().SetPlayerStatus(kingStatusParameter.CreatePlayerStatus(pieceTypePlayerOne.IsPromoted));
                    PlayerOne.GetComponent<Player>().SetSecondaryAction(kingSecondaryAction);
                    PlayerOne.GetComponent<Player>().SetSkill(kingSkill);
                    PlayerOne.GetComponent<Player>().SetPrimaryAction(kingPrimaryAction);
                    break;
            }
            switch (pieceTypePlayerTwo.Type)
            {
                case PieceType.Fuhyo:
                    PlayerTwo.GetComponent<Player>().SetPlayerStatus(fuhyoStatusParameter.CreatePlayerStatus(pieceTypePlayerTwo.IsPromoted));
                    PlayerTwo.GetComponent<Player>().SetSecondaryAction(fuhyoSecondaryAction);
                    PlayerTwo.GetComponent<Player>().SetSkill(fuhyoSkill);
                    PlayerTwo.GetComponent<Player>().SetPrimaryAction(fuhyoPrimaryAction);
                    break;
                case PieceType.Kyosya:
                    PlayerTwo.GetComponent<Player>().SetPlayerStatus(kyosyaStatusParameter.CreatePlayerStatus(pieceTypePlayerTwo.IsPromoted));
                    PlayerTwo.GetComponent<Player>().SetSecondaryAction(kyosyaSecondaryAction);
                    PlayerTwo.GetComponent<Player>().SetSkill(kyosyaSkill);
                    PlayerTwo.GetComponent<Player>().SetPrimaryAction(kyosyaPrimaryAction);
                    break;
                case PieceType.Keima:
                    PlayerTwo.GetComponent<Player>().SetPlayerStatus(keimaStatusParameter.CreatePlayerStatus(pieceTypePlayerTwo.IsPromoted));
                    PlayerTwo.GetComponent<Player>().SetSecondaryAction(keimaSecondaryAction);
                    PlayerTwo.GetComponent<Player>().SetSkill(keimaSkill);
                    PlayerTwo.GetComponent<Player>().SetPrimaryAction(keimaPrimaryAction);
                    break;
                case PieceType.Gin:
                    PlayerTwo.GetComponent<Player>().SetPlayerStatus(ginStatusParameter.CreatePlayerStatus(pieceTypePlayerTwo.IsPromoted));
                    PlayerTwo.GetComponent<Player>().SetSecondaryAction(ginSecondaryAction);
                    PlayerTwo.GetComponent<Player>().SetSkill(ginSkill);
                    PlayerTwo.GetComponent<Player>().SetPrimaryAction(ginPrimaryAction);
                    break;
                case PieceType.Kin:
                    PlayerTwo.GetComponent<Player>().SetPlayerStatus(kinStatusParameter.CreatePlayerStatus(pieceTypePlayerTwo.IsPromoted));
                    PlayerTwo.GetComponent<Player>().SetSecondaryAction(kinSecondaryAction);
                    PlayerTwo.GetComponent<Player>().SetSkill(kinSkill);
                    PlayerTwo.GetComponent<Player>().SetPrimaryAction(kinPrimaryAction);
                    break;
                case PieceType.Kakugyo:
                    PlayerTwo.GetComponent<Player>().SetPlayerStatus(kakugyoStatusParameter.CreatePlayerStatus(pieceTypePlayerTwo.IsPromoted));
                    PlayerTwo.GetComponent<Player>().SetSecondaryAction(kakugyoSecondaryAction);
                    PlayerTwo.GetComponent<Player>().SetSkill(kakugyoSkill);
                    PlayerTwo.GetComponent<Player>().SetPrimaryAction(kakugyoPrimaryAction);
                    break;
                case PieceType.Hisya:
                    PlayerTwo.GetComponent<Player>().SetPlayerStatus(hisyaStatusParameter.CreatePlayerStatus(pieceTypePlayerTwo.IsPromoted));
                    PlayerTwo.GetComponent<Player>().SetSecondaryAction(hisyaSecondaryAction);
                    PlayerTwo.GetComponent<Player>().SetSkill(hisyaSkill);
                    PlayerTwo.GetComponent<Player>().SetPrimaryAction(hisyaPrimaryAction);
                    break;
                case PieceType.King:
                    PlayerTwo.GetComponent<Player>().SetPlayerStatus(kingStatusParameter.CreatePlayerStatus(pieceTypePlayerTwo.IsPromoted));
                    PlayerTwo.GetComponent<Player>().SetSecondaryAction(kingSecondaryAction);
                    PlayerTwo.GetComponent<Player>().SetSkill(kingSkill);
                    PlayerTwo.GetComponent<Player>().SetPrimaryAction(kingPrimaryAction);
                    break;
            }
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
