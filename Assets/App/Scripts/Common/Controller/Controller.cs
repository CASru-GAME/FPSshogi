using UnityEngine.InputSystem;
using UnityEngine;
using App.Common.Initialize;
using System;

namespace App.Common.Controller
{
    /// <summary>
    /// プレイヤーコントローラー（ActionMap対応版）
    /// </summary>
    public class Controller : MonoBehaviour, IInitializable
    {
        [SerializeField] private _InputSystemActions inputActionAsset;
        [SerializeField] private bool enableInputLogging = false;

        // 初期化システム用プロパティ
        public int InitializationPriority => 100;
        public System.Type[] Dependencies => new System.Type[0];

        // ActionMap とアクション
        private InputActionMap playerActionMap;
        private InputAction debugAction => playerActionMap?.FindAction("Debug");

        public void Initialize()
        {
            try
            {
                SetupInputActionMap();
                SetupInputCallbacks();
                EnablePlayerInput();

                Debug.Log($"✅ Controller初期化完了: ActionMap={playerActionMap?.name}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Controller初期化エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// ActionMapとアクションの設定
        /// </summary>
        private void SetupInputActionMap()
        {
            // InputActionAssetが設定されていない場合、自動検索
            if (inputActionAsset == null)
            {
                inputActionAsset = new _InputSystemActions();
            }
            // Debug ActionMapの取得
            playerActionMap = inputActionAsset.Debug;
            if (playerActionMap == null)
            {
                Debug.LogError("Debug ActionMapが見つかりません！");
                return;
            }
            // 各アクションの取得
            var debugAction = playerActionMap.FindAction("Debug");
            if (debugAction == null)
            {
                Debug.LogError("Debugアクションが見つかりません！");
            }
            if (enableInputLogging)
            {
                Debug.Log($"ActionMap設定完了: {playerActionMap.actions.Count}個のアクション");
            }
        }

        /// <summary>
        /// 入力コールバックの設定
        /// </summary>
        private void SetupInputCallbacks()
        {
            if (debugAction != null)
            {
                debugAction.performed += ctx =>
                {
                    if (enableInputLogging)
                        Debug.Log("Debugアクションが実行されました");
                };
                if (enableInputLogging)
                    Debug.Log("Controller: 入力コールバックを登録完了");
            }
            else
            {
                Debug.LogWarning("Controller: debugActionがnullのため、コールバック登録をスキップ");
            }
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
        /// プレイヤー入力を無効化
        /// </summary>
        public void DisablePlayerInput()
        {
            playerActionMap?.Disable();

            if (enableInputLogging)
                Debug.Log("❌ プレイヤー入力を無効化");
        }

        /// <summary>
        /// ActionMapの状態を取得
        /// </summary>
        public bool IsPlayerInputEnabled()
        {
            return playerActionMap?.enabled ?? false;
        }

        void OnEnable()
        {
            inputActionAsset?.Enable();
        }

        void OnDisable()
        {
            inputActionAsset?.Disable();
        }

        void OnDestroy()
        {
            // コールバックの解除
            if (debugAction != null)
            {
                debugAction.performed -= ctx =>
                {
                    if (enableInputLogging)
                        Debug.Log("Debugアクションが実行されました");
                };
            }
            if (enableInputLogging)
                Debug.Log("Controller: 入力コールバックを登録解除");
        }
    }
}
