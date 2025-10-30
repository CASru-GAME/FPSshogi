using App.Common.Initialize;
using UnityEngine;
using App.Main.GameMaster;
using UnityEngine.InputSystem;
using App.Main.ShogiThings;
using System;

namespace App.Main.Player
{
    public class PlayerManager : MonoBehaviour, IInitializable
    {
        public int InitializationPriority => 0;
        public System.Type[] Dependencies => new System.Type[] { typeof(GameStateHolder), typeof(ShogiBoard) };
        [SerializeField] private InputActionAsset inputActions;

        [SerializeField] private GameObject PlayerPrefab;
        [SerializeField] private GameObject PlayerOnePieceObject;
        [SerializeField] private GameObject PlayerTwoPieceObject;
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
        [SerializeField] private GameObject fuhyoPieceObject;
        [SerializeField] private GameObject kyosyaPieceObject;
        [SerializeField] private GameObject keimaPieceObject;
        [SerializeField] private GameObject ginPieceObject;
        [SerializeField] private GameObject kinPieceObject;
        [SerializeField] private GameObject kakugyoPieceObject;
        [SerializeField] private GameObject hisyaPieceObject;
        [SerializeField] private GameObject gyokuPieceObject;
        [SerializeField] private GameObject ouPieceObject;
        [SerializeField] private GameObject bomb;
        [SerializeField] private GameObject blackKakugyo;
        [SerializeField] private GameObject kingWeapon;
        [SerializeField] private GameObject fuhyoWeapon;
        [SerializeField] private GameObject kyosyaWeapon;
        [SerializeField] private GameObject keimaWeapon;
        [SerializeField] private GameObject ginWeapon;
        [SerializeField] private GameObject kinWeapon;
        [SerializeField] private GameObject kakugyoWeapon;
        [SerializeField] private GameObject hisyaWeapon;
        private Action OnUpdate;
        public void FixedUpdate()
        {
            // プレイヤーの物理演算更新処理をここに記述
            OnUpdate?.Invoke();
        }

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
            //CursorDisable();
            SetPlayerCondition();
        }

        private void SetPlayerModel(GameObject player, GameObject pieceObject, bool isPromoted, bool isPlayerOne)
        {
            GameObject go = Instantiate(pieceObject);
            go.transform.SetParent(player.transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            if (isPromoted)
            {
                // 成り後のモデル調整処理
                go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            }
            if (isPlayerOne)
            {
                PlayerOnePieceObject = go;
            }
            else
            {
                // プレイヤー2用の向き調整
                PlayerTwoPieceObject = go;
            }
        }

        private void OnExitDuel()
        {
            EnableOnlyMap("Shogi");
            DisablePlayerCamera();
            //CursorEnable();
            Destroy(PlayerOnePieceObject);
            Destroy(PlayerTwoPieceObject);
        }

        public void AddEffectToAllPlayers()
        {
            if (gameStateHolder.CurrentState != GameStateHolder.GameState.Duel)
            {
                return;
            }
            PlayerOne.GetComponent<Player>().playerStatus.EffectList.AddEffect(new FlashBangEffect());
            PlayerTwo.GetComponent<Player>().playerStatus.EffectList.AddEffect(new FlashBangEffect());
        }



        private void SetPlayerCondition()
        {
            IPiece pieceTypePlayerOne = shogiBoard.GetDuelPiece()[PlayerType.PlayerOne];
            IPiece pieceTypePlayerTwo = shogiBoard.GetDuelPiece()[PlayerType.PlayerTwo];

            switch (pieceTypePlayerOne.Type)
            {
                case PieceType.Fuhyo:
                    SetPlayerModel(PlayerOne, fuhyoPieceObject, pieceTypePlayerOne.IsPromoted, true);
                    PlayerOne.GetComponent<Player>().SetPlayerStatus(fuhyoStatusParameter.CreatePlayerStatus(pieceTypePlayerOne.IsPromoted, PlayerOne.GetComponent<Player>()));
                    //PlayerOne.GetComponent<Player>().SetSecondaryAction(fuhyoSecondaryAction);
                    PlayerOne.GetComponent<Player>().SetSkill(new FuhyoSkill());
                    //PlayerOne.GetComponent<Player>().SetPrimaryAction(fuhyoPrimaryAction);
                    break;
                case PieceType.Kyosya:
                    SetPlayerModel(PlayerOne, kyosyaPieceObject, pieceTypePlayerOne.IsPromoted, true);
                    PlayerOne.GetComponent<Player>().SetPlayerStatus(kyosyaStatusParameter.CreatePlayerStatus(pieceTypePlayerOne.IsPromoted, PlayerOne.GetComponent<Player>()));
                    //PlayerOne.GetComponent<Player>().SetSecondaryAction(kyosyaSecondaryAction);
                    PlayerOne.GetComponent<Player>().SetSkill(new KyosyaSkill());
                    PlayerOne.GetComponent<Player>().SetSubWeaponObject(bomb);
                    //PlayerOne.GetComponent<Player>().SetPrimaryAction(kyosyaPrimaryAction);
                    break;
                case PieceType.Keima:
                    SetPlayerModel(PlayerOne, keimaPieceObject, pieceTypePlayerOne.IsPromoted, true);
                    PlayerOne.GetComponent<Player>().SetPlayerStatus(keimaStatusParameter.CreatePlayerStatus(pieceTypePlayerOne.IsPromoted, PlayerOne.GetComponent<Player>()));
                    //PlayerOne.GetComponent<Player>().SetSecondaryAction(keimaSecondaryAction);
                    PlayerOne.GetComponent<Player>().SetSkill(new KeimaSkill());
                    //PlayerOne.GetComponent<Player>().SetPrimaryAction(keimaPrimaryAction);
                    break;
                case PieceType.Gin:
                    SetPlayerModel(PlayerOne, ginPieceObject, pieceTypePlayerOne.IsPromoted, true);
                    PlayerOne.GetComponent<Player>().SetPlayerStatus(ginStatusParameter.CreatePlayerStatus(pieceTypePlayerOne.IsPromoted, PlayerOne.GetComponent<Player>()));
                    /* PlayerOne.GetComponent<Player>().SetSecondaryAction(ginSecondaryAction);
                    PlayerOne.GetComponent<Player>().SetSkill(ginSkill);
                    PlayerOne.GetComponent<Player>().SetPrimaryAction(ginPrimaryAction); */
                    break;
                case PieceType.Kin:
                    SetPlayerModel(PlayerOne, kinPieceObject, pieceTypePlayerOne.IsPromoted, true);
                    PlayerOne.GetComponent<Player>().SetPlayerStatus(kinStatusParameter.CreatePlayerStatus(pieceTypePlayerOne.IsPromoted, PlayerOne.GetComponent<Player>()));
                    /* PlayerOne.GetComponent<Player>().SetSecondaryAction(kinSecondaryAction);
                    PlayerOne.GetComponent<Player>().SetSkill(kinSkill);
                    PlayerOne.GetComponent<Player>().SetPrimaryAction(kinPrimaryAction); */
                    break;
                case PieceType.Kakugyo:
                    SetPlayerModel(PlayerOne, kakugyoPieceObject, pieceTypePlayerOne.IsPromoted, true);
                    PlayerOne.GetComponent<Player>().SetPlayerStatus(kakugyoStatusParameter.CreatePlayerStatus(pieceTypePlayerOne.IsPromoted, PlayerOne.GetComponent<Player>()));
                    /* PlayerOne.GetComponent<Player>().SetSecondaryAction(kakugyoSecondaryAction);
                    PlayerOne.GetComponent<Player>().SetSkill(kakugyoSkill);
                    PlayerOne.GetComponent<Player>().SetPrimaryAction(kakugyoPrimaryAction); */
                    break;
                case PieceType.Hisya:
                    SetPlayerModel(PlayerOne, hisyaPieceObject, pieceTypePlayerOne.IsPromoted, true);
                    PlayerOne.GetComponent<Player>().SetPlayerStatus(hisyaStatusParameter.CreatePlayerStatus(pieceTypePlayerOne.IsPromoted, PlayerOne.GetComponent<Player>()));
                    /* PlayerOne.GetComponent<Player>().SetSecondaryAction(hisyaSecondaryAction);
                    PlayerOne.GetComponent<Player>().SetSkill(hisyaSkill);
                    PlayerOne.GetComponent<Player>().SetPrimaryAction(hisyaPrimaryAction); */
                    break;
                case PieceType.King:
                    SetPlayerModel(PlayerOne, ouPieceObject, pieceTypePlayerOne.IsPromoted, true);
                    PlayerOne.GetComponent<Player>().SetPlayerStatus(kingStatusParameter.CreatePlayerStatus(pieceTypePlayerOne.IsPromoted, PlayerOne.GetComponent<Player>()));
                    /* PlayerOne.GetComponent<Player>().SetSecondaryAction(kingSecondaryAction);
                    PlayerOne.GetComponent<Player>().SetSkill(kingSkill);
                    PlayerOne.GetComponent<Player>().SetPrimaryAction(kingPrimaryAction); */
                    break;
            }
            switch (pieceTypePlayerTwo.Type)
            {
                case PieceType.Fuhyo:
                    SetPlayerModel(PlayerTwo, fuhyoPieceObject, pieceTypePlayerTwo.IsPromoted, false);
                    PlayerTwo.GetComponent<Player>().SetPlayerStatus(fuhyoStatusParameter.CreatePlayerStatus(pieceTypePlayerTwo.IsPromoted, PlayerTwo.GetComponent<Player>()));
                    //PlayerTwo.GetComponent<Player>().SetSecondaryAction(fuhyoSecondaryAction);
                    PlayerTwo.GetComponent<Player>().SetSkill(new FuhyoSkill());
                    //PlayerTwo.GetComponent<Player>().SetPrimaryAction(fuhyoPrimaryAction);
                    break;
                case PieceType.Kyosya:
                    SetPlayerModel(PlayerTwo, kyosyaPieceObject, pieceTypePlayerTwo.IsPromoted, false);
                    PlayerTwo.GetComponent<Player>().SetPlayerStatus(kyosyaStatusParameter.CreatePlayerStatus(pieceTypePlayerTwo.IsPromoted, PlayerTwo.GetComponent<Player>()));
                    /* PlayerTwo.GetComponent<Player>().SetSecondaryAction(kyosyaSecondaryAction);
                    PlayerTwo.GetComponent<Player>().SetSkill(kyosyaSkill);
                    PlayerTwo.GetComponent<Player>().SetPrimaryAction(kyosyaPrimaryAction); */
                    break;
                case PieceType.Keima:
                    SetPlayerModel(PlayerTwo, keimaPieceObject, pieceTypePlayerTwo.IsPromoted, false);
                    PlayerTwo.GetComponent<Player>().SetPlayerStatus(keimaStatusParameter.CreatePlayerStatus(pieceTypePlayerTwo.IsPromoted, PlayerTwo.GetComponent<Player>()));
                    // PlayerTwo.GetComponent<Player>().SetSecondaryAction(keimaSecondaryAction);
                    PlayerTwo.GetComponent<Player>().SetSkill(new KeimaSkill());
                    //PlayerTwo.GetComponent<Player>().SetPrimaryAction(keimaPrimaryAction);
                    break;
                case PieceType.Gin:
                    SetPlayerModel(PlayerTwo, ginPieceObject, pieceTypePlayerTwo.IsPromoted, false);
                    PlayerTwo.GetComponent<Player>().SetPlayerStatus(ginStatusParameter.CreatePlayerStatus(pieceTypePlayerTwo.IsPromoted, PlayerTwo.GetComponent<Player>()));
                    /* PlayerTwo.GetComponent<Player>().SetSecondaryAction(ginSecondaryAction);
                    PlayerTwo.GetComponent<Player>().SetSkill(ginSkill);
                    PlayerTwo.GetComponent<Player>().SetPrimaryAction(ginPrimaryAction); */
                    break;
                case PieceType.Kin:
                    SetPlayerModel(PlayerTwo, kinPieceObject, pieceTypePlayerTwo.IsPromoted, false);
                    PlayerTwo.GetComponent<Player>().SetPlayerStatus(kinStatusParameter.CreatePlayerStatus(pieceTypePlayerTwo.IsPromoted, PlayerTwo.GetComponent<Player>()));
                    /* PlayerTwo.GetComponent<Player>().SetSecondaryAction(kinSecondaryAction);
                    PlayerTwo.GetComponent<Player>().SetSkill(kinSkill);
                    PlayerTwo.GetComponent<Player>().SetPrimaryAction(kinPrimaryAction); */
                    break;
                case PieceType.Kakugyo:
                    SetPlayerModel(PlayerTwo, kakugyoPieceObject, pieceTypePlayerTwo.IsPromoted, false);
                    PlayerTwo.GetComponent<Player>().SetPlayerStatus(kakugyoStatusParameter.CreatePlayerStatus(pieceTypePlayerTwo.IsPromoted, PlayerTwo.GetComponent<Player>()));
                    /* PlayerTwo.GetComponent<Player>().SetSecondaryAction(kakugyoSecondaryAction);
                    PlayerTwo.GetComponent<Player>().SetSkill(kakugyoSkill);
                    PlayerTwo.GetComponent<Player>().SetPrimaryAction(kakugyoPrimaryAction); */
                    break;
                case PieceType.Hisya:
                    SetPlayerModel(PlayerTwo, hisyaPieceObject, pieceTypePlayerTwo.IsPromoted, false);
                    PlayerTwo.GetComponent<Player>().SetPlayerStatus(hisyaStatusParameter.CreatePlayerStatus(pieceTypePlayerTwo.IsPromoted, PlayerTwo.GetComponent<Player>()));
                    /* PlayerTwo.GetComponent<Player>().SetSecondaryAction(hisyaSecondaryAction);
                    PlayerTwo.GetComponent<Player>().SetSkill(hisyaSkill);
                    PlayerTwo.GetComponent<Player>().SetPrimaryAction(hisyaPrimaryAction); */
                    break;
                case PieceType.King:
                    SetPlayerModel(PlayerTwo, gyokuPieceObject, pieceTypePlayerTwo.IsPromoted, false);
                    PlayerTwo.GetComponent<Player>().SetPlayerStatus(kingStatusParameter.CreatePlayerStatus(pieceTypePlayerTwo.IsPromoted, PlayerTwo.GetComponent<Player>()));
                    /* PlayerTwo.GetComponent<Player>().SetSecondaryAction(kingSecondaryAction);
                    PlayerTwo.GetComponent<Player>().SetSkill(kingSkill);
                    PlayerTwo.GetComponent<Player>().SetPrimaryAction(kingPrimaryAction); */
                    break;
            }
        }
        
        public void SetPlayerForDebugFuhyo()
        {
            SetPlayerModel(PlayerOne, fuhyoPieceObject, false, true);
            PlayerOne.GetComponent<Player>().SetPlayerStatus(fuhyoStatusParameter.CreatePlayerStatus(false, PlayerOne.GetComponent<Player>()));
            //PlayerOne.GetComponent<Player>().SetSecondaryAction(fuhyoSecondaryAction);
            PlayerOne.GetComponent<Player>().SetSkill(new FuhyoSkill());
            //PlayerOne.GetComponent<Player>().SetPrimaryAction(fuhyoPrimaryAction);
            SetPlayerModel(PlayerTwo, fuhyoPieceObject, false, false);
            PlayerTwo.GetComponent<Player>().SetPlayerStatus(fuhyoStatusParameter.CreatePlayerStatus(false, PlayerTwo.GetComponent<Player>()));
            //PlayerTwo.GetComponent<Player>().SetSecondaryAction(fuhyoSecondaryAction);
            PlayerTwo.GetComponent<Player>().SetSkill(new FuhyoSkill());
            //PlayerTwo.GetComponent<Player>().SetPrimaryAction(fuhyoPrimaryAction);
        }

        public void SetPlayerForDebugKyosya()
        {
            SetPlayerModel(PlayerOne, kyosyaPieceObject, false, true);
            PlayerOne.GetComponent<Player>().SetPlayerStatus(kyosyaStatusParameter.CreatePlayerStatus(false, PlayerOne.GetComponent<Player>()));
            //PlayerOne.GetComponent<Player>().SetSecondaryAction(kyosyaSecondaryAction);
            PlayerOne.GetComponent<Player>().SetSkill(new KyosyaSkill());
            PlayerOne.GetComponent<Player>().SetSubWeaponObject(bomb);
            //PlayerOne.GetComponent<Player>().SetPrimaryAction(kyosyaPrimaryAction);
            SetPlayerModel(PlayerTwo, kyosyaPieceObject, false, false);
            PlayerTwo.GetComponent<Player>().SetPlayerStatus(kyosyaStatusParameter.CreatePlayerStatus(false, PlayerTwo.GetComponent<Player>()));
            //PlayerTwo.GetComponent<Player>().SetSecondaryAction(kyosyaSecondaryAction);
            PlayerTwo.GetComponent<Player>().SetSkill(new KyosyaSkill());
            PlayerTwo.GetComponent<Player>().SetSubWeaponObject(bomb);
            //PlayerTwo.GetComponent<Player>().SetPrimaryAction(kyosyaPrimaryAction);
        }
        
        public void SetPlayerForDebugKeima()
        {
            SetPlayerModel(PlayerOne, keimaPieceObject, false, true);
            PlayerOne.GetComponent<Player>().SetPlayerStatus(keimaStatusParameter.CreatePlayerStatus(false, PlayerOne.GetComponent<Player>()));
            // PlayerOne.GetComponent<Player>().SetSecondaryAction(keimaSecondaryAction);
            PlayerOne.GetComponent<Player>().SetSkill(new KeimaSkill());
            //PlayerOne.GetComponent<Player>().SetPrimaryAction(keimaPrimaryAction);
            SetPlayerModel(PlayerTwo, keimaPieceObject, false, false);
            PlayerTwo.GetComponent<Player>().SetPlayerStatus(keimaStatusParameter.CreatePlayerStatus(false, PlayerTwo.GetComponent<Player>()));
            // PlayerTwo.GetComponent<Player>().SetSecondaryAction(keimaSecondaryAction);
            PlayerTwo.GetComponent<Player>().SetSkill(new KeimaSkill());
            // PlayerTwo.GetComponent<Player>().SetPrimaryAction(keimaPrimaryAction);
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
