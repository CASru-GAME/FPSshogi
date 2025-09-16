using UnityEngine.InputSystem;
using UnityEngine;
using App.Common.Initialize;
using App.Common.Controller;
using System;
using System.Linq;

namespace App.Common.Controller
{
    /// <summary>
    /// プレイヤーコントローラー（ActionMap対応版）
    /// </summary>
    public class Controller : MonoBehaviour, IInitializable
    {
        [SerializeField] private InputSystemActionAssets inputActionAssets;
        [SerializeField] private bool enableInputLogging = false;

        // 初期化システム用プロパティ
        public int InitializationPriority => 100;
        public System.Type[] Dependencies => new System.Type[0];

        // ActionMap とアクション
        private InputActionMap playerActionMap;
        private InputActionMap shogiActionMap;
        private InputActionMap uiActionMap;
        private InputActionMap debugActionMap;
        private InputAction Look;
        private InputAction moveForward;
        private InputAction moveBackward;
        private InputAction moveLeft;
        private InputAction moveRight;
        private InputAction weaponActionMain;
        private InputAction weaponActionSub;
        private InputAction abilityOne;
        private InputAction abilityTwo;
        private InputAction climb;
        private InputAction selectUpShogi;
        private InputAction selectDownShogi;
        private InputAction selectLeftShogi;
        private InputAction selectRightShogi;
        private InputAction pointShogi;
        private InputAction selectShogi;
        private InputAction cancelShogi;
        private InputAction selectUpUI;
        private InputAction selectDownUI;
        private InputAction selectLeftUI;
        private InputAction selectRightUI;
        private InputAction pointUI;
        private InputAction selectUI;
        private InputAction cancelUI;
        private InputAction debug;

        public void Initialize(ReferenceHolder referenceHolder)
        {
            try
            {
                SetupInputActionMap();
                SetupInputActions();

                Debug.Log($"✅ Controller初期化完了: ActionMap={playerActionMap?.name}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Controller初期化エラー: {ex.Message}");
            }
        }

        public void SubscriveToLook(Action<InputAction.CallbackContext> callback)
        {
            Look.performed += callback;
        }

        public void UnsubscribeFromLook(Action<InputAction.CallbackContext> callback)
        {
            Look.performed -= callback;
        }

        public void SubscribeToMoveForward(Action<InputAction.CallbackContext> callback)
        {
            moveForward.performed += callback;
            moveForward.canceled += callback;
        }

        public void UnsubscribeFromMoveForward(Action<InputAction.CallbackContext> callback)
        {
            moveForward.performed -= callback;
            moveForward.canceled -= callback;
        }

        public void SubscribeToMoveBackward(Action<InputAction.CallbackContext> callback)
        {
            moveBackward.performed += callback;
            moveBackward.canceled += callback;
        }

        public void UnsubscribeFromMoveBackward(Action<InputAction.CallbackContext> callback)
        {
            moveBackward.performed -= callback;
            moveBackward.canceled -= callback;
        }

        public void SubscribeToMoveLeft(Action<InputAction.CallbackContext> callback)
        {
            moveLeft.performed += callback;
            moveLeft.canceled += callback;
        }

        public void UnsubscribeFromMoveLeft(Action<InputAction.CallbackContext> callback)
        {
            moveLeft.performed -= callback;
            moveLeft.canceled -= callback;
        }

        public void SubscribeToMoveRight(Action<InputAction.CallbackContext> callback)
        {
            moveRight.performed += callback;
            moveRight.canceled += callback;
        }

        public void UnsubscribeFromMoveRight(Action<InputAction.CallbackContext> callback)
        {
            moveRight.performed -= callback;
            moveRight.canceled -= callback;
        }

        public void SubscribeToWeaponActionMain(Action<InputAction.CallbackContext> callback)
        {
            weaponActionMain.performed += callback;
            weaponActionMain.canceled += callback;
        }

        public void UnsubscribeFromWeaponActionMain(Action<InputAction.CallbackContext> callback)
        {
            weaponActionMain.performed -= callback;
            weaponActionMain.canceled -= callback;
        }

        public void SubscribeToWeaponActionSub(Action<InputAction.CallbackContext> callback)
        {
            weaponActionSub.performed += callback;
            weaponActionSub.canceled += callback;
        }

        public void UnsubscribeFromWeaponActionSub(Action<InputAction.CallbackContext> callback)
        {
            weaponActionSub.performed -= callback;
            weaponActionSub.canceled -= callback;
        }

        public void SubscribeToAbilityOne(Action<InputAction.CallbackContext> callback)
        {
            abilityOne.performed += callback;
            abilityOne.canceled += callback;
        }

        public void UnsubscribeFromAbilityOne(Action<InputAction.CallbackContext> callback)
        {
            abilityOne.performed -= callback;
            abilityOne.canceled -= callback;
        }

        public void SubscribeToAbilityTwo(Action<InputAction.CallbackContext> callback)
        {
            abilityTwo.performed += callback;
            abilityTwo.canceled += callback;
        }

        public void UnsubscribeFromAbilityTwo(Action<InputAction.CallbackContext> callback)
        {
            abilityTwo.performed -= callback;
            abilityTwo.canceled -= callback;
        }

        public void SubscribeToClimb(Action<InputAction.CallbackContext> callback)
        {
            climb.performed += callback;
            climb.canceled += callback;
        }

        public void UnsubscribeFromClimb(Action<InputAction.CallbackContext> callback)
        {
            climb.performed -= callback;
            climb.canceled -= callback;
        }

        public void SubscribeToSelectUpShogi(Action<InputAction.CallbackContext> callback)
        {
            selectUpShogi.performed += callback;
            selectUpShogi.canceled += callback;
        }

        public void UnsubscribeFromSelectUpShogi(Action<InputAction.CallbackContext> callback)
        {
            selectUpShogi.performed -= callback;
            selectUpShogi.canceled -= callback;
        }

        public void SubscribeToSelectDownShogi(Action<InputAction.CallbackContext> callback)
        {
            selectDownShogi.performed += callback;
            selectDownShogi.canceled += callback;
        }

        public void UnsubscribeFromSelectDownShogi(Action<InputAction.CallbackContext> callback)
        {
            selectDownShogi.performed -= callback;
            selectDownShogi.canceled -= callback;
        }

        public void SubscribeToSelectLeftShogi(Action<InputAction.CallbackContext> callback)
        {
            selectLeftShogi.performed += callback;
            selectLeftShogi.canceled += callback;
        }

        public void UnsubscribeFromSelectLeftShogi(Action<InputAction.CallbackContext> callback)
        {
            selectLeftShogi.performed -= callback;
            selectLeftShogi.canceled -= callback;
        }

        public void SubscribeToSelectRightShogi(Action<InputAction.CallbackContext> callback)
        {
            selectRightShogi.performed += callback;
            selectRightShogi.canceled += callback;
        }

        public void UnsubscribeFromSelectRightShogi(Action<InputAction.CallbackContext> callback)
        {
            selectRightShogi.performed -= callback;
            selectRightShogi.canceled -= callback;
        }

        public void UnsubscribeFromPointShogi(Action<InputAction.CallbackContext> callback)
        {
            pointShogi.performed -= callback;
            pointShogi.canceled -= callback;
        }

        public void SubscribeToSelectShogi(Action<InputAction.CallbackContext> callback)
        {
            selectShogi.performed += callback;
            selectShogi.canceled += callback;
        }

        public void UnsubscribeFromSelectShogi(Action<InputAction.CallbackContext> callback)
        {
            selectShogi.performed -= callback;
            selectShogi.canceled -= callback;
        }

        public void SubscribeToCancelShogi(Action<InputAction.CallbackContext> callback)
        {
            cancelShogi.performed += callback;
            cancelShogi.canceled += callback;
        }

        public void UnsubscribeFromCancelShogi(Action<InputAction.CallbackContext> callback)
        {
            cancelShogi.performed -= callback;
            cancelShogi.canceled -= callback;
        }

        public void SubscribeToSelectUpUI(Action<InputAction.CallbackContext> callback)
        {
            selectUpUI.performed += callback;
            selectUpUI.canceled += callback;
        }

        public void UnsubscribeFromSelectUpUI(Action<InputAction.CallbackContext> callback)
        {
            selectUpUI.performed -= callback;
            selectUpUI.canceled -= callback;
        }

        public void SubscribeToSelectDownUI(Action<InputAction.CallbackContext> callback)
        {
            selectDownUI.performed += callback;
            selectDownUI.canceled += callback;
        }

        public void UnsubscribeFromSelectDownUI(Action<InputAction.CallbackContext> callback)
        {
            selectDownUI.performed -= callback;
            selectDownUI.canceled -= callback;
        }

        public void SubscribeToSelectLeftUI(Action<InputAction.CallbackContext> callback)
        {
            selectLeftUI.performed += callback;
            selectLeftUI.canceled += callback;
        }

        public void UnsubscribeFromSelectLeftUI(Action<InputAction.CallbackContext> callback)
        {
            selectLeftUI.performed -= callback;
            selectLeftUI.canceled -= callback;
        }

        public void SubscribeToSelectRightUI(Action<InputAction.CallbackContext> callback)
        {
            selectRightUI.performed += callback;
            selectRightUI.canceled += callback;
        }

        public void UnsubscribeFromSelectRightUI(Action<InputAction.CallbackContext> callback)
        {
            selectRightUI.performed -= callback;
            selectRightUI.canceled -= callback;
        }
        
        public void SubscribeToPointUI(Action<InputAction.CallbackContext> callback)
        {
            pointUI.performed += callback;
            pointUI.canceled += callback;
        }

        public void UnsubscribeFromPointUI(Action<InputAction.CallbackContext> callback)
        {
            pointUI.performed -= callback;
            pointUI.canceled -= callback;
        }

        public void SubscribeToSelectUI(Action<InputAction.CallbackContext> callback)
        {
            selectUI.performed += callback;
            selectUI.canceled += callback;
        }

        public void UnsubscribeFromSelectUI(Action<InputAction.CallbackContext> callback)
        {
            selectUI.performed -= callback;
            selectUI.canceled -= callback;
        }

        public void SubscribeToCancelUI(Action<InputAction.CallbackContext> callback)
        {
            cancelUI.performed += callback;
            cancelUI.canceled += callback;
        }

        public void UnsubscribeFromCancelUI(Action<InputAction.CallbackContext> callback)
        {
            cancelUI.performed -= callback;
            cancelUI.canceled -= callback;
        }

        public void SubscribeToDebug(Action<InputAction.CallbackContext> callback)
        {
            debug.performed += callback;
            debug.canceled += callback;
        }

        /// <summary>
        /// ActionMapとアクションの設定
        /// </summary>
        private void SetupInputActionMap()
        {
            // InputActionAssetが設定されていない場合、自動検索
            if (inputActionAssets == null)
            {
                inputActionAssets = new InputSystemActionAssets();
            }
            playerActionMap = inputActionAssets.Player;
            if (playerActionMap == null)
            {
                Debug.LogError("Player ActionMapが見つかりません！");
                return;
            }
            shogiActionMap = inputActionAssets.Shogi;
            if (shogiActionMap == null)
            {
                Debug.LogError("Shogi ActionMapが見つかりません！");
                return;
            }
            uiActionMap = inputActionAssets.UI;
            if (uiActionMap == null)
            {
                Debug.LogError("UI ActionMapが見つかりません！");
                return;
            }
            debugActionMap = inputActionAssets.Debug;
            if (debugActionMap == null)
            {
                Debug.LogError("Debug ActionMapが見つかりません！");
                return;
            }
        }

        private void SetupInputActions()
        {
            // 各アクションの取得
            Look = inputActionAssets.Player.Look;
            moveForward = inputActionAssets.Player.MoveForward;
            moveBackward = inputActionAssets.Player.MoveBackward;
            moveLeft = inputActionAssets.Player.MoveLeft;
            moveRight = inputActionAssets.Player.MoveRight;
            weaponActionMain = inputActionAssets.Player.WeaponActionMain;
            weaponActionSub = inputActionAssets.Player.WeaponActionSub;
            abilityOne = inputActionAssets.Player.AbilityOne;
            abilityTwo = inputActionAssets.Player.AbilityTwo;
            climb = inputActionAssets.Player.Climb;
            selectUpShogi = inputActionAssets.Shogi.SelectUp;
            selectDownShogi = inputActionAssets.Shogi.SelectDown;
            selectLeftShogi = inputActionAssets.Shogi.SelectLeft;
            selectRightShogi = inputActionAssets.Shogi.SelectRight;
            pointShogi = inputActionAssets.Shogi.Point;
            selectShogi = inputActionAssets.Shogi.Select;
            cancelShogi = inputActionAssets.Shogi.Cancel;
            selectUpUI = inputActionAssets.UI.SelectUp;
            selectDownUI = inputActionAssets.UI.SelectDown;
            selectLeftUI = inputActionAssets.UI.SelectLeft;
            selectRightUI = inputActionAssets.UI.SelectRight;
            selectUI = inputActionAssets.UI.Select;
            cancelUI = inputActionAssets.UI.Cancel;
            debug = inputActionAssets.Debug.Debug;
            pointUI = inputActionAssets.UI.Point;

            if (enableInputLogging)
                Debug.Log("✅ 入力アクションを設定完了");
        }

        public void EnableAllInput()
        {
            playerActionMap?.Enable();
            shogiActionMap?.Enable();
            uiActionMap?.Enable();
            debugActionMap?.Enable();

            if (enableInputLogging)
                Debug.Log("✅ 全ての入力を有効化");
        }

        /// <summary>
        /// プレイヤー入力を有効化
        /// </summary>
        public void EnablePlayerInput()
        {
            playerActionMap?.Enable();

            if (enableInputLogging)
                Debug.Log("✅ プレイヤー入力を有効化");
        }

        /// <summary>
        /// 将棋入力を有効化
        /// </summary>
        public void EnableShogiInput()
        {
            shogiActionMap?.Enable();

            if (enableInputLogging)
                Debug.Log("✅ 将棋入力を有効化");
        }

        /// <summary>
        /// UI入力を有効化
        /// </summary>
        public void EnableUIInput()
        {
            uiActionMap?.Enable();

            if (enableInputLogging)
                Debug.Log("✅ UI入力を有効化");
        }
        
        public void EnableDebugInput()
        {
            debugActionMap?.Enable();

            if (enableInputLogging)
                Debug.Log("✅ デバッグ入力を有効化");
        }

        public void DisableAllInput()
        {
            playerActionMap?.Disable();
            shogiActionMap?.Disable();
            uiActionMap?.Disable();
            debugActionMap?.Disable();

            if (enableInputLogging)
                Debug.Log("❌ 全ての入力を無効化");
        }

        /// <summary>
        /// プレイヤー入力を無効化
        /// </summary>
        public void DisablePlayerInput()
        {
            playerActionMap?.Disable();

            if (enableInputLogging)
                Debug.Log("❌ プレイヤー入力を無効化");
        }

        public void DisableShogiInput()
        {
            shogiActionMap?.Disable();

            if (enableInputLogging)
                Debug.Log("❌ 将棋入力を無効化");
        }

        public void DisableUIInput()
        {
            uiActionMap?.Disable();

            if (enableInputLogging)
                Debug.Log("❌ UI入力を無効化");
        }

        public void DisableDebugInput()
        {
            debugActionMap?.Disable();

            if (enableInputLogging)
                Debug.Log("❌ デバッグ入力を無効化");
        }

        void OnDestroy()
        {
            if (enableInputLogging)
                Debug.Log("Controller: 入力コールバックを登録解除");
        }
    }
}
