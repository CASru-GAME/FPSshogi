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
        [SerializeField] private GameObject Arrow;
        [SerializeField] private GameObject Gunbai;
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
        private bool uiBlocked;
        private bool menuOpen;
        private string activeMap = "Shogi";
        public void SetUiBlocked(bool blocked)
        {
            if (uiBlocked == blocked) return;
            uiBlocked = blocked;
            if (blocked)
            {
                DisableAllInput();
            }
            else if (gameStateHolder.CurrentState != GameStateHolder.GameState.PlayerOneWin && gameStateHolder.CurrentState != GameStateHolder.GameState.PlayerTwoWin)
                EnableOnlyMap(activeMap);
        }
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
            RefreshDuelCameras();
            gameStateHolder.SubscribeToChangeToDuel(OnChangedToDuel);
            gameStateHolder.SubscribeToExitDuel(OnExitDuel);
            gameStateHolder.SubscribeToChangeToPlayerOneWin(OnEndGame);
            gameStateHolder.SubscribeToChangeToPlayerTwoWin(OnEndGame);
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
            ResetPlayerPrefabPosition();
            ResetPlayerPrefabRotation();
            PlayerOne.GetComponent<Player>().ResetDuelPresentation();
            PlayerTwo.GetComponent<Player>().ResetDuelPresentation();
            RefreshDuelCameras();
            CursorDisable();
            SetPlayerCondition();
            App.PlayData.PlayRecorder.Current?.BeginDuel(shogiBoard, PlayerOne.GetComponent<Player>(), PlayerTwo.GetComponent<Player>());
        }

        //プレイヤーのプレハブをスポーンポジションに戻す
        private void ResetPlayerPrefabPosition()
        {
            if (PlayerOne != null)
            {
                PlayerOne.transform.position = PlayerOneSpawnPosition;
                PlayerOne.transform.rotation = PlayerOneSpawnRotation;
            }
            if (PlayerTwo != null)
            {
                PlayerTwo.transform.position = PlayerTwoSpawnPosition;
                PlayerTwo.transform.rotation = PlayerTwoSpawnRotation;
            }
        }

        private void ResetPlayerPrefabRotation()
        {
            if (PlayerOne != null)
            {
                PlayerOne.transform.rotation = PlayerOneSpawnRotation;
            }
            if (PlayerTwo != null)
            {
                PlayerTwo.transform.rotation = PlayerTwoSpawnRotation;
            }
        }

        public void ChangeToPlayerOneWinForDebug()
        {
            gameStateHolder.ChangeStateIntoDuelPlayerOneWin();
            Debug.Log("Changed to Duel Player One Win state for debug.");
            gameStateHolder.ChangeStateIntoPlayerOneWin();
            Debug.Log("Changed to Player One Win state for debug.");
        }

        private void SetPlayerModel(GameObject player, GameObject pieceObject, bool isPromoted, bool isPlayerOne)
        {
            GameObject go = Instantiate(pieceObject);
            go.transform.SetParent(player.transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            // Each split-screen view hides its own body; the opponent still sees it.
            int bodyLayer=isPlayerOne?30:31;
            player.GetComponent<Player>().BodyLayer=bodyLayer;
            foreach(var node in go.GetComponentsInChildren<Transform>(true))node.gameObject.layer=bodyLayer;
            var ownCamera=player.GetComponentInChildren<Camera>(true);
            if(ownCamera!=null)ownCamera.cullingMask&=~(1<<bodyLayer);
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
            PlayerOne.GetComponent<Player>().ResetDuelPresentation();
            PlayerTwo.GetComponent<Player>().ResetDuelPresentation();
            EnableOnlyMap("Shogi");
            RefreshDuelCameras();
            CursorEnable();

            // PlayerOne/Two に割り当てたピースオブジェクトを削除（子オブジェクトも含む）
            if (PlayerOnePieceObject != null)
            {
                DestroyObjectWithChildren(PlayerOnePieceObject);
                PlayerOnePieceObject = null;
            }
            if (PlayerTwoPieceObject != null)
            {
                DestroyObjectWithChildren(PlayerTwoPieceObject);
                PlayerTwoPieceObject = null;
            }

            DestroyChildrenSafe(PlayerOne);
            DestroyChildrenSafe(PlayerTwo);
        }

        private void OnEndGame()
        {
            PlayerOne.GetComponent<Player>().ResetDuelPresentation();
            PlayerTwo.GetComponent<Player>().ResetDuelPresentation();
            DisableAllInput();
            RefreshDuelCameras();
            CursorEnable();
        }

        // GameObject とその子を安全に破棄（Editor/PlayMode に対応）
        private void DestroyObjectWithChildren(GameObject go)
        {
            if (go == null) return;
            if (go.GetComponent<Camera>() != null)
            {
                go.GetComponent<Camera>().enabled = false;
            }
            else if (Application.isPlaying)
                Destroy(go);
            else
                DestroyImmediate(go);
        }

        // 指定 GameObject の直下にある全子オブジェクトを破棄（null チェックあり）
        private void DestroyChildrenSafe(GameObject parent)
        {
            if (parent == null) return;

            // Transform を直接列挙すると破棄でコレクションが変わるため一旦収集する
            var children = new System.Collections.Generic.List<GameObject>();
            foreach (Transform t in parent.transform)
            {
                if (t != null && t.gameObject != null)
                    children.Add(t.gameObject);
            }

            foreach (var c in children)
            {
                DestroyObjectWithChildren(c);
            }
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
            PlayerOne.GetComponent<Player>().PieceType = pieceTypePlayerOne.Type;
            PlayerTwo.GetComponent<Player>().PieceType = pieceTypePlayerTwo.Type;

            switch (pieceTypePlayerOne.Type)
            {
                case PieceType.Fuhyo:
                    SetPlayerModel(PlayerOne, fuhyoPieceObject, pieceTypePlayerOne.IsPromoted, true);
                    PlayerOne.GetComponent<Player>().SetPlayerStatus(fuhyoStatusParameter.CreatePlayerStatus(pieceTypePlayerOne.IsPromoted, PlayerOne.GetComponent<Player>()));
                    PlayerOne.GetComponent<Player>().SetSecondaryAction(new SecondaryGuard());
                    PlayerOne.GetComponent<Player>().SetSkill(new FuhyoSkill());
                    PlayerOne.GetComponent<Player>().SetPrimaryAction(new CloseWeaponPrimary());
                    PlayerOne.GetComponent<Player>().SetWeaponObject(fuhyoWeapon);
                    break;
                case PieceType.Kyosya:
                    SetPlayerModel(PlayerOne, kyosyaPieceObject, pieceTypePlayerOne.IsPromoted, true);
                    PlayerOne.GetComponent<Player>().SetPlayerStatus(kyosyaStatusParameter.CreatePlayerStatus(pieceTypePlayerOne.IsPromoted, PlayerOne.GetComponent<Player>()));
                    PlayerOne.GetComponent<Player>().SetSecondaryAction(new SecondaryZoom());
                    PlayerOne.GetComponent<Player>().SetSkill(new KyosyaSkill());
                    PlayerOne.GetComponent<Player>().SetSubWeaponObject(bomb);
                    PlayerOne.GetComponent<Player>().SetPrimaryAction(new ShortGunPrimary());
                    PlayerOne.GetComponent<Player>().SetWeaponObject(kyosyaWeapon);
                    break;
                case PieceType.Keima:
                    SetPlayerModel(PlayerOne, keimaPieceObject, pieceTypePlayerOne.IsPromoted, true);
                    PlayerOne.GetComponent<Player>().SetPlayerStatus(keimaStatusParameter.CreatePlayerStatus(pieceTypePlayerOne.IsPromoted, PlayerOne.GetComponent<Player>()));
                    PlayerOne.GetComponent<Player>().SetSecondaryAction(new SecondaryGuard());
                    PlayerOne.GetComponent<Player>().SetSkill(new KeimaSkill());
                    PlayerOne.GetComponent<Player>().SetPrimaryAction(new CloseWeaponPrimary());
                    PlayerOne.GetComponent<Player>().SetWeaponObject(keimaWeapon);
                    break;
                case PieceType.Gin:
                    SetPlayerModel(PlayerOne, ginPieceObject, pieceTypePlayerOne.IsPromoted, true);
                    PlayerOne.GetComponent<Player>().SetPlayerStatus(ginStatusParameter.CreatePlayerStatus(pieceTypePlayerOne.IsPromoted, PlayerOne.GetComponent<Player>()));
                    PlayerOne.GetComponent<Player>().SetSecondaryAction(new SecondaryGuard());
                    PlayerOne.GetComponent<Player>().SetSkill(new GinSkill());
                    PlayerOne.GetComponent<Player>().SetPrimaryAction(new CloseWeaponPrimary());
                    PlayerOne.GetComponent<Player>().SetWeaponObject(ginWeapon);
                    break;
                case PieceType.Kin:
                    SetPlayerModel(PlayerOne, kinPieceObject, pieceTypePlayerOne.IsPromoted, true);
                    PlayerOne.GetComponent<Player>().SetPlayerStatus(kinStatusParameter.CreatePlayerStatus(pieceTypePlayerOne.IsPromoted, PlayerOne.GetComponent<Player>()));
                    PlayerOne.GetComponent<Player>().SetSecondaryAction(new SecondaryGuard());
                    PlayerOne.GetComponent<Player>().SetSkill(new KinSkill());
                    PlayerOne.GetComponent<Player>().SetPrimaryAction(new CloseWeaponPrimary());
                    PlayerOne.GetComponent<Player>().SetWeaponObject(kinWeapon);
                    break;
                case PieceType.Kakugyo:
                    SetPlayerModel(PlayerOne, kakugyoPieceObject, pieceTypePlayerOne.IsPromoted, true);
                    PlayerOne.GetComponent<Player>().SetPlayerStatus(kakugyoStatusParameter.CreatePlayerStatus(pieceTypePlayerOne.IsPromoted, PlayerOne.GetComponent<Player>()));
                    PlayerOne.GetComponent<Player>().SetSecondaryAction(new SecondaryZoom());
                    PlayerOne.GetComponent<Player>().SetSkill(new KakugyoSkill());
                    PlayerOne.GetComponent<Player>().SetPrimaryAction(new LongGunPrimary());
                    PlayerOne.GetComponent<Player>().SetWeaponObject(kakugyoWeapon);
                    break;
                case PieceType.Hisya:
                    SetPlayerModel(PlayerOne, hisyaPieceObject, pieceTypePlayerOne.IsPromoted, true);
                    PlayerOne.GetComponent<Player>().SetPlayerStatus(hisyaStatusParameter.CreatePlayerStatus(pieceTypePlayerOne.IsPromoted, PlayerOne.GetComponent<Player>()));
                    PlayerOne.GetComponent<Player>().SetSecondaryAction(new SecondaryZoom());
                    PlayerOne.GetComponent<Player>().SetSkill(new HisyaSkill());
                    PlayerOne.GetComponent<Player>().SetPrimaryAction(new BowPrimary());
                    PlayerOne.GetComponent<Player>().SetWeaponObject(hisyaWeapon);
                    PlayerOne.GetComponent<Player>().SetSubWeaponObject(Arrow);
                    break;
                case PieceType.King:
                    SetPlayerModel(PlayerOne, ouPieceObject, pieceTypePlayerOne.IsPromoted, true);
                    PlayerOne.GetComponent<Player>().SetPlayerStatus(kingStatusParameter.CreatePlayerStatus(pieceTypePlayerOne.IsPromoted, PlayerOne.GetComponent<Player>()));
                    PlayerOne.GetComponent<Player>().SetSecondaryAction(new SecondaryGuard());
                    PlayerOne.GetComponent<Player>().SetSkill(new KingSkill { gunbaiPrefab = Gunbai, turretPrefab = blackKakugyo });
                    PlayerOne.GetComponent<Player>().SetPrimaryAction(new CloseWeaponPrimary());
                    PlayerOne.GetComponent<Player>().SetWeaponObject(kingWeapon);
                    PlayerOne.GetComponent<Player>().SetObjectForKingSkill(blackKakugyo);
                    break;
            }
            switch (pieceTypePlayerTwo.Type)
            {
                case PieceType.Fuhyo:
                    SetPlayerModel(PlayerTwo, fuhyoPieceObject, pieceTypePlayerTwo.IsPromoted, false);
                    PlayerTwo.GetComponent<Player>().SetPlayerStatus(fuhyoStatusParameter.CreatePlayerStatus(pieceTypePlayerTwo.IsPromoted, PlayerTwo.GetComponent<Player>()));
                    PlayerTwo.GetComponent<Player>().SetSecondaryAction(new SecondaryGuard());
                    PlayerTwo.GetComponent<Player>().SetSkill(new FuhyoSkill());
                    PlayerTwo.GetComponent<Player>().SetPrimaryAction(new CloseWeaponPrimary());
                    PlayerTwo.GetComponent<Player>().SetWeaponObject(fuhyoWeapon);
                    break;
                case PieceType.Kyosya:
                    SetPlayerModel(PlayerTwo, kyosyaPieceObject, pieceTypePlayerTwo.IsPromoted, false);
                    PlayerTwo.GetComponent<Player>().SetPlayerStatus(kyosyaStatusParameter.CreatePlayerStatus(pieceTypePlayerTwo.IsPromoted, PlayerTwo.GetComponent<Player>()));
                    PlayerTwo.GetComponent<Player>().SetSecondaryAction(new SecondaryZoom());
                    PlayerTwo.GetComponent<Player>().SetSkill(new KyosyaSkill());
                    PlayerTwo.GetComponent<Player>().SetSubWeaponObject(bomb);
                    PlayerTwo.GetComponent<Player>().SetPrimaryAction(new ShortGunPrimary());
                    PlayerTwo.GetComponent<Player>().SetWeaponObject(kyosyaWeapon);
                    break;
                case PieceType.Keima:
                    SetPlayerModel(PlayerTwo, keimaPieceObject, pieceTypePlayerTwo.IsPromoted, false);
                    PlayerTwo.GetComponent<Player>().SetPlayerStatus(keimaStatusParameter.CreatePlayerStatus(pieceTypePlayerTwo.IsPromoted, PlayerTwo.GetComponent<Player>()));
                    PlayerTwo.GetComponent<Player>().SetSecondaryAction(new SecondaryGuard());
                    PlayerTwo.GetComponent<Player>().SetSkill(new KeimaSkill());
                    PlayerTwo.GetComponent<Player>().SetPrimaryAction(new CloseWeaponPrimary());
                    PlayerTwo.GetComponent<Player>().SetWeaponObject(keimaWeapon);
                    break;
                case PieceType.Gin:
                    SetPlayerModel(PlayerTwo, ginPieceObject, pieceTypePlayerTwo.IsPromoted, false);
                    PlayerTwo.GetComponent<Player>().SetPlayerStatus(ginStatusParameter.CreatePlayerStatus(pieceTypePlayerTwo.IsPromoted, PlayerTwo.GetComponent<Player>()));
                    PlayerTwo.GetComponent<Player>().SetSecondaryAction(new SecondaryGuard());
                    PlayerTwo.GetComponent<Player>().SetSkill(new GinSkill());
                    PlayerTwo.GetComponent<Player>().SetPrimaryAction(new CloseWeaponPrimary());
                    PlayerTwo.GetComponent<Player>().SetWeaponObject(ginWeapon);
                    break;
                case PieceType.Kin:
                    SetPlayerModel(PlayerTwo, kinPieceObject, pieceTypePlayerTwo.IsPromoted, false);
                    PlayerTwo.GetComponent<Player>().SetPlayerStatus(kinStatusParameter.CreatePlayerStatus(pieceTypePlayerTwo.IsPromoted, PlayerTwo.GetComponent<Player>()));
                    PlayerTwo.GetComponent<Player>().SetSecondaryAction(new SecondaryGuard());
                    PlayerTwo.GetComponent<Player>().SetSkill(new KinSkill());
                    PlayerTwo.GetComponent<Player>().SetPrimaryAction(new CloseWeaponPrimary());
                    PlayerTwo.GetComponent<Player>().SetWeaponObject(kinWeapon);
                    break;
                case PieceType.Kakugyo:
                    SetPlayerModel(PlayerTwo, kakugyoPieceObject, pieceTypePlayerTwo.IsPromoted, false);
                    PlayerTwo.GetComponent<Player>().SetPlayerStatus(kakugyoStatusParameter.CreatePlayerStatus(pieceTypePlayerTwo.IsPromoted, PlayerTwo.GetComponent<Player>()));
                    PlayerTwo.GetComponent<Player>().SetSecondaryAction(new SecondaryZoom());
                    PlayerTwo.GetComponent<Player>().SetSkill(new KakugyoSkill());
                    PlayerTwo.GetComponent<Player>().SetPrimaryAction(new LongGunPrimary());
                    PlayerTwo.GetComponent<Player>().SetWeaponObject(kakugyoWeapon);
                    break;
                case PieceType.Hisya:
                    SetPlayerModel(PlayerTwo, hisyaPieceObject, pieceTypePlayerTwo.IsPromoted, false);
                    PlayerTwo.GetComponent<Player>().SetPlayerStatus(hisyaStatusParameter.CreatePlayerStatus(pieceTypePlayerTwo.IsPromoted, PlayerTwo.GetComponent<Player>()));
                    PlayerTwo.GetComponent<Player>().SetSecondaryAction(new SecondaryZoom());
                    PlayerTwo.GetComponent<Player>().SetSkill(new HisyaSkill());
                    PlayerTwo.GetComponent<Player>().SetPrimaryAction(new BowPrimary());
                    PlayerTwo.GetComponent<Player>().SetWeaponObject(hisyaWeapon);
                    PlayerTwo.GetComponent<Player>().SetSubWeaponObject(Arrow);
                    break;
                case PieceType.King:
                    SetPlayerModel(PlayerTwo, gyokuPieceObject, pieceTypePlayerTwo.IsPromoted, false);
                    PlayerTwo.GetComponent<Player>().SetPlayerStatus(kingStatusParameter.CreatePlayerStatus(pieceTypePlayerTwo.IsPromoted, PlayerTwo.GetComponent<Player>()));
                    PlayerTwo.GetComponent<Player>().SetSecondaryAction(new SecondaryGuard());
                    PlayerTwo.GetComponent<Player>().SetSkill(new KingSkill { gunbaiPrefab = Gunbai, turretPrefab = blackKakugyo });
                    PlayerTwo.GetComponent<Player>().SetPrimaryAction(new CloseWeaponPrimary());
                    PlayerTwo.GetComponent<Player>().SetWeaponObject(kingWeapon);
                    PlayerTwo.GetComponent<Player>().SetObjectForKingSkill(blackKakugyo);
                    break;
            }
        }

        public void SetPlayerForDebugFuhyo()
        {
            SetPlayerModel(PlayerOne, fuhyoPieceObject, false, true);
            PlayerOne.GetComponent<Player>().SetPlayerStatus(fuhyoStatusParameter.CreatePlayerStatus(false, PlayerOne.GetComponent<Player>()));
            PlayerOne.GetComponent<Player>().SetSecondaryAction(new SecondaryGuard());
            PlayerOne.GetComponent<Player>().SetSkill(new FuhyoSkill());
            PlayerOne.GetComponent<Player>().SetPrimaryAction(new CloseWeaponPrimary());
            PlayerOne.GetComponent<Player>().SetWeaponObject(fuhyoWeapon);
            SetPlayerModel(PlayerTwo, fuhyoPieceObject, false, false);
            PlayerTwo.GetComponent<Player>().SetPlayerStatus(fuhyoStatusParameter.CreatePlayerStatus(false, PlayerTwo.GetComponent<Player>()));
            PlayerTwo.GetComponent<Player>().SetSecondaryAction(new SecondaryGuard());
            PlayerTwo.GetComponent<Player>().SetSkill(new FuhyoSkill());
            PlayerTwo.GetComponent<Player>().SetPrimaryAction(new CloseWeaponPrimary());
            PlayerTwo.GetComponent<Player>().SetWeaponObject(fuhyoWeapon);
        }

        public void SetPlayerForDebugKyosya()
        {
            SetPlayerModel(PlayerOne, kyosyaPieceObject, false, true);
            PlayerOne.GetComponent<Player>().SetPlayerStatus(kyosyaStatusParameter.CreatePlayerStatus(false, PlayerOne.GetComponent<Player>()));
            PlayerTwo.GetComponent<Player>().SetSecondaryAction(new SecondaryZoom());
            PlayerOne.GetComponent<Player>().SetSkill(new KyosyaSkill());
            PlayerOne.GetComponent<Player>().SetSubWeaponObject(bomb);
            PlayerOne.GetComponent<Player>().SetPrimaryAction(new ShortGunPrimary());
            PlayerOne.GetComponent<Player>().SetWeaponObject(kyosyaWeapon);
            SetPlayerModel(PlayerTwo, kyosyaPieceObject, false, false);
            PlayerTwo.GetComponent<Player>().SetPlayerStatus(kyosyaStatusParameter.CreatePlayerStatus(false, PlayerTwo.GetComponent<Player>()));
            PlayerTwo.GetComponent<Player>().SetSecondaryAction(new SecondaryZoom());
            PlayerTwo.GetComponent<Player>().SetSkill(new KyosyaSkill());
            PlayerTwo.GetComponent<Player>().SetSubWeaponObject(bomb);
            PlayerTwo.GetComponent<Player>().SetPrimaryAction(new ShortGunPrimary());
            PlayerTwo.GetComponent<Player>().SetWeaponObject(kyosyaWeapon);
        }

        public void SetPlayerForDebugKeima()
        {
            SetPlayerModel(PlayerOne, keimaPieceObject, false, true);
            PlayerOne.GetComponent<Player>().SetPlayerStatus(keimaStatusParameter.CreatePlayerStatus(false, PlayerOne.GetComponent<Player>()));
            PlayerOne.GetComponent<Player>().SetSecondaryAction(new SecondaryGuard());
            PlayerOne.GetComponent<Player>().SetSkill(new KeimaSkill());
            PlayerOne.GetComponent<Player>().SetPrimaryAction(new CloseWeaponPrimary());
            PlayerOne.GetComponent<Player>().SetWeaponObject(keimaWeapon);
            SetPlayerModel(PlayerTwo, keimaPieceObject, false, false);
            PlayerTwo.GetComponent<Player>().SetPlayerStatus(keimaStatusParameter.CreatePlayerStatus(false, PlayerTwo.GetComponent<Player>()));
            PlayerTwo.GetComponent<Player>().SetSecondaryAction(new SecondaryGuard());
            PlayerTwo.GetComponent<Player>().SetSkill(new KeimaSkill());
            PlayerTwo.GetComponent<Player>().SetPrimaryAction(new CloseWeaponPrimary());
            PlayerTwo.GetComponent<Player>().SetWeaponObject(keimaWeapon);
        }

        public void SetPlayerForDebugGin()
        {
            SetPlayerModel(PlayerOne, ginPieceObject, false, true);
            PlayerOne.GetComponent<Player>().SetPlayerStatus(ginStatusParameter.CreatePlayerStatus(false, PlayerOne.GetComponent<Player>()));
            PlayerOne.GetComponent<Player>().SetSecondaryAction(new SecondaryGuard());
            PlayerOne.GetComponent<Player>().SetSkill(new GinSkill());
            PlayerOne.GetComponent<Player>().SetPrimaryAction(new CloseWeaponPrimary());
            PlayerOne.GetComponent<Player>().SetWeaponObject(ginWeapon);
            SetPlayerModel(PlayerTwo, ginPieceObject, false, false);
            PlayerTwo.GetComponent<Player>().SetPlayerStatus(ginStatusParameter.CreatePlayerStatus(false, PlayerTwo.GetComponent<Player>()));
            PlayerTwo.GetComponent<Player>().SetSecondaryAction(new SecondaryGuard());
            PlayerTwo.GetComponent<Player>().SetSkill(new GinSkill());
            PlayerTwo.GetComponent<Player>().SetPrimaryAction(new CloseWeaponPrimary());
            PlayerTwo.GetComponent<Player>().SetWeaponObject(ginWeapon);
        }

        public void SetPlayerForDebugKin()
        {
            SetPlayerModel(PlayerOne, kinPieceObject, false, true);
            PlayerOne.GetComponent<Player>().SetPlayerStatus(kinStatusParameter.CreatePlayerStatus(false, PlayerOne.GetComponent<Player>()));
            PlayerOne.GetComponent<Player>().SetSecondaryAction(new SecondaryGuard());
            PlayerOne.GetComponent<Player>().SetSkill(new KinSkill());
            PlayerOne.GetComponent<Player>().SetPrimaryAction(new CloseWeaponPrimary());
            PlayerOne.GetComponent<Player>().SetWeaponObject(kinWeapon);
            SetPlayerModel(PlayerTwo, kinPieceObject, false, false);
            PlayerTwo.GetComponent<Player>().SetPlayerStatus(kinStatusParameter.CreatePlayerStatus(false, PlayerTwo.GetComponent<Player>()));
            PlayerTwo.GetComponent<Player>().SetSecondaryAction(new SecondaryGuard());
            PlayerTwo.GetComponent<Player>().SetSkill(new KinSkill());
            PlayerTwo.GetComponent<Player>().SetPrimaryAction(new CloseWeaponPrimary());
            PlayerTwo.GetComponent<Player>().SetWeaponObject(kinWeapon);
        }

        public void SetPlayerForDebugKakugyo()
        {
            SetPlayerModel(PlayerOne, kakugyoPieceObject, false, true);
            PlayerOne.GetComponent<Player>().SetPlayerStatus(kakugyoStatusParameter.CreatePlayerStatus(false, PlayerOne.GetComponent<Player>()));
            PlayerOne.GetComponent<Player>().SetSecondaryAction(new SecondaryZoom());
            PlayerOne.GetComponent<Player>().SetSkill(new KakugyoSkill());
            PlayerOne.GetComponent<Player>().SetPrimaryAction(new LongGunPrimary());
            PlayerOne.GetComponent<Player>().SetWeaponObject(kakugyoWeapon);
            SetPlayerModel(PlayerTwo, kakugyoPieceObject, false, false);
            PlayerTwo.GetComponent<Player>().SetPlayerStatus(kakugyoStatusParameter.CreatePlayerStatus(false, PlayerTwo.GetComponent<Player>()));
            PlayerTwo.GetComponent<Player>().SetSecondaryAction(new SecondaryZoom());
            PlayerTwo.GetComponent<Player>().SetSkill(new KakugyoSkill());
            PlayerTwo.GetComponent<Player>().SetPrimaryAction(new LongGunPrimary());
            PlayerTwo.GetComponent<Player>().SetWeaponObject(kakugyoWeapon);
        }

        public void SetPlayerForDebugHisya()
        {
            SetPlayerModel(PlayerOne, hisyaPieceObject, false, true);
            PlayerOne.GetComponent<Player>().SetPlayerStatus(hisyaStatusParameter.CreatePlayerStatus(false, PlayerOne.GetComponent<Player>()));
            PlayerOne.GetComponent<Player>().SetSecondaryAction(new SecondaryZoom());
            PlayerOne.GetComponent<Player>().SetSkill(new HisyaSkill());
            PlayerOne.GetComponent<Player>().SetPrimaryAction(new BowPrimary());
            PlayerOne.GetComponent<Player>().SetWeaponObject(hisyaWeapon);
            PlayerOne.GetComponent<Player>().SetSubWeaponObject(Arrow);
            SetPlayerModel(PlayerTwo, hisyaPieceObject, false, false);
            PlayerTwo.GetComponent<Player>().SetPlayerStatus(hisyaStatusParameter.CreatePlayerStatus(false, PlayerTwo.GetComponent<Player>()));
            PlayerTwo.GetComponent<Player>().SetSecondaryAction(new SecondaryZoom());
            PlayerTwo.GetComponent<Player>().SetSkill(new HisyaSkill());
            PlayerTwo.GetComponent<Player>().SetPrimaryAction(new BowPrimary());
            PlayerTwo.GetComponent<Player>().SetWeaponObject(hisyaWeapon);
            PlayerTwo.GetComponent<Player>().SetSubWeaponObject(Arrow);
        }

        public void SetPlayerForDebugKing()
        {
            SetPlayerModel(PlayerOne, ouPieceObject, false, true);
            PlayerOne.GetComponent<Player>().SetPlayerStatus(kingStatusParameter.CreatePlayerStatus(false, PlayerOne.GetComponent<Player>()));
            PlayerOne.GetComponent<Player>().SetSecondaryAction(new SecondaryGuard());
            PlayerOne.GetComponent<Player>().SetSkill(new KingSkill { gunbaiPrefab = Gunbai, turretPrefab = blackKakugyo });
            PlayerOne.GetComponent<Player>().SetPrimaryAction(new CloseWeaponPrimary());
            PlayerOne.GetComponent<Player>().SetWeaponObject(kingWeapon);
            PlayerOne.GetComponent<Player>().SetObjectForKingSkill(blackKakugyo);
            SetPlayerModel(PlayerTwo, gyokuPieceObject, false, false);
            PlayerTwo.GetComponent<Player>().SetPlayerStatus(kingStatusParameter.CreatePlayerStatus(false, PlayerTwo.GetComponent<Player>()));
            PlayerTwo.GetComponent<Player>().SetSecondaryAction(new SecondaryGuard());
            PlayerTwo.GetComponent<Player>().SetSkill(new KingSkill { gunbaiPrefab = Gunbai, turretPrefab = blackKakugyo });
            PlayerTwo.GetComponent<Player>().SetPrimaryAction(new CloseWeaponPrimary());
            PlayerTwo.GetComponent<Player>().SetWeaponObject(kingWeapon);
            PlayerTwo.GetComponent<Player>().SetObjectForKingSkill(blackKakugyo);
        }

        /// <summary>
        /// すべての入力を切ってから指定した ActionMap 名だけを有効化する
        /// </summary>
        private void EnableOnlyMap(string mapName)
        {
            activeMap = mapName;
            DisableAllInput();
            if (uiBlocked) return;
            foreach (var player in new[] { PlayerOne, PlayerTwo })
            {
                var input = player.GetComponent<PlayerInput>();
                input.ActivateInput();
                input.SwitchCurrentActionMap(mapName);
            }
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

        public void SetMenuOpen(bool open)
        {
            if (menuOpen == open) return;
            menuOpen = open;
            RefreshDuelCameras();
        }

        private void RefreshDuelCameras()
        {
            // The full-screen board camera already renders the room beneath these two views.
            bool showDuel = !menuOpen && gameStateHolder.CurrentState == GameStateHolder.GameState.Duel;
            PlayerOne.GetComponentInChildren<Camera>().enabled = showDuel;
            PlayerTwo.GetComponentInChildren<Camera>().enabled = showDuel;
        }

        private void DisableAllInput()
        {
            // Disabling an action map sends Canceled even for a held trigger.
            PlayerOne.GetComponent<Player>().ResetInput();
            PlayerTwo.GetComponent<Player>().ResetInput();
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
