using UnityEngine;
using App.Main.GameMaster;

namespace App.Examples
{
    public class GameStateObserver : MonoBehaviour
    {
        [SerializeField] private GameStateHolder gameStateHolder;

        private void Start()
        {
            // GameStateHolderの参照を取得
            if (gameStateHolder == null)
                gameStateHolder = FindFirstObjectByType<GameStateHolder>();

            // イベントを購読
            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            // 状態変更イベントを購読
            gameStateHolder.SubscribeToStateChange(OnGameStateChanged);
            
            // 状態入場イベントを購読
            gameStateHolder.SubscribeToStateEnter(OnGameStateEnter);
            
            // 状態退出イベントを購読
            gameStateHolder.SubscribeToStateExit(OnGameStateExit);
            
            // 個別状態変化イベントを購読
            gameStateHolder.SubscribeToChangeToStarting(OnChangeToStarting);
            gameStateHolder.SubscribeToChangeToPlayerOneTurn(OnChangeToPlayerOneTurn);
            gameStateHolder.SubscribeToChangeToPlayerTwoTurn(OnChangeToPlayerTwoTurn);
            gameStateHolder.SubscribeToChangeToDuel(OnChangeToDuel);
            gameStateHolder.SubscribeToChangeToPaused(OnChangeToPaused);
            gameStateHolder.SubscribeToChangeToGameOver(OnChangeToGameOver);
        }

        private void UnsubscribeFromEvents()
        {
            // メモリリーク防止のため購読を解除
            if (gameStateHolder != null)
            {
                gameStateHolder.UnsubscribeFromStateChange(OnGameStateChanged);
                gameStateHolder.UnsubscribeFromStateEnter(OnGameStateEnter);
                gameStateHolder.UnsubscribeFromStateExit(OnGameStateExit);
                
                // 個別状態変化イベントの購読を解除
                gameStateHolder.UnsubscribeFromChangeToStarting(OnChangeToStarting);
                gameStateHolder.UnsubscribeFromChangeToPlayerOneTurn(OnChangeToPlayerOneTurn);
                gameStateHolder.UnsubscribeFromChangeToPlayerTwoTurn(OnChangeToPlayerTwoTurn);
                gameStateHolder.UnsubscribeFromChangeToDuel(OnChangeToDuel);
                gameStateHolder.UnsubscribeFromChangeToPaused(OnChangeToPaused);
                gameStateHolder.UnsubscribeFromChangeToGameOver(OnChangeToGameOver);
            }
        }

        // 状態変更時のコールバック
        private void OnGameStateChanged(GameStateHolder.GameState previousState, GameStateHolder.GameState newState)
        {
            Debug.Log($"[GameStateObserver] State changed: {previousState} -> {newState}");
            
            // 特定の状態変更に応じた処理
            switch (newState)
            {
                case GameStateHolder.GameState.PlayerOneTurn:
                    HandlePlayerOneTurnStart();
                    break;
                case GameStateHolder.GameState.PlayerTwoTurn:
                    HandlePlayerTwoTurnStart();
                    break;
                case GameStateHolder.GameState.Duel:
                    HandleDuelStart();
                    break;
                case GameStateHolder.GameState.GameOver:
                    HandleGameOver();
                    break;
            }
        }

        // 状態入場時のコールバック
        private void OnGameStateEnter(GameStateHolder.GameState state)
        {
            Debug.Log($"[GameStateObserver] Entered state: {state}");
            
            // 状態入場時の処理
            switch (state)
            {
                case GameStateHolder.GameState.Starting:
                    ShowGameStartUI();
                    break;
                case GameStateHolder.GameState.Paused:
                    ShowPauseUI();
                    break;
            }
        }

        // 状態退出時のコールバック
        private void OnGameStateExit(GameStateHolder.GameState state)
        {
            Debug.Log($"[GameStateObserver] Exited state: {state}");
            
            // 状態退出時の処理
            switch (state)
            {
                case GameStateHolder.GameState.Paused:
                    HidePauseUI();
                    break;
            }
        }

        // 個別の状態処理メソッド
        private void HandlePlayerOneTurnStart()
        {
            Debug.Log("プレイヤー1のターンが開始されました");
            // プレイヤー1のUI表示、入力有効化など
        }

        private void HandlePlayerTwoTurnStart()
        {
            Debug.Log("プレイヤー2のターンが開始されました");
            // プレイヤー2のUI表示、入力有効化など
        }

        private void HandleDuelStart()
        {
            Debug.Log("決闘が開始されました");
            // 決闘シーンの開始処理
        }

        private void HandleGameOver()
        {
            Debug.Log("ゲーム終了");
            // ゲーム終了処理、結果表示など
        }

        private void ShowGameStartUI()
        {
            Debug.Log("ゲーム開始UIを表示");
            // ゲーム開始時のUI表示
        }

        private void ShowPauseUI()
        {
            Debug.Log("ポーズUIを表示");
            // ポーズメニューの表示
        }

        private void HidePauseUI()
        {
            Debug.Log("ポーズUIを非表示");
            // ポーズメニューの非表示
        }

        // 個別状態変化イベントのコールバック
        private void OnChangeToStarting()
        {
            Debug.Log("[GameStateObserver] 個別イベント: Starting状態に変更されました");
            ShowGameStartUI();
        }

        private void OnChangeToPlayerOneTurn()
        {
            Debug.Log("[GameStateObserver] 個別イベント: プレイヤー1のターンに変更されました");
            HandlePlayerOneTurnStart();
        }

        private void OnChangeToPlayerTwoTurn()
        {
            Debug.Log("[GameStateObserver] 個別イベント: プレイヤー2のターンに変更されました");
            HandlePlayerTwoTurnStart();
        }

        private void OnChangeToDuel()
        {
            Debug.Log("[GameStateObserver] 個別イベント: 決闘状態に変更されました");
            HandleDuelStart();
        }

        private void OnChangeToPaused()
        {
            Debug.Log("[GameStateObserver] 個別イベント: 一時停止状態に変更されました");
            ShowPauseUI();
        }

        private void OnChangeToGameOver()
        {
            Debug.Log("[GameStateObserver] 個別イベント: ゲームオーバー状態に変更されました");
            HandleGameOver();
        }

        private void OnDestroy()
        {
            // オブジェクト破棄時にイベント購読を解除
            UnsubscribeFromEvents();
        }

        // テスト用：ボタンからの状態変更
        public void TestChangeToPlayerOneTurn()
        {
            gameStateHolder.ChangeState(GameStateHolder.GameState.PlayerOneTurn);
        }

        public void TestChangeToPlayerTwoTurn()
        {
            gameStateHolder.ChangeState(GameStateHolder.GameState.PlayerTwoTurn);
        }

        public void TestChangeToDuel()
        {
            gameStateHolder.ChangeState(GameStateHolder.GameState.Duel);
        }

        public void TestPauseGame()
        {
            gameStateHolder.ChangeState(GameStateHolder.GameState.Paused);
        }

        public void TestGameOver()
        {
            gameStateHolder.ChangeState(GameStateHolder.GameState.GameOver);
        }
    }
}
