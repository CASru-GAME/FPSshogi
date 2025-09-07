using System;
using UnityEngine;

namespace App.Main.GameMaster
{
    public class GameStateHolder : MonoBehaviour
    {
        public enum GameState
        {
            Starting,
            PlayerOneTurn,
            PlayerTwoTurn,
            Duel,
            Paused,
            GameOver
        }

        [SerializeField] private GameState currentState = GameState.Starting;
        
        // イベント定義
        public event Action<GameState, GameState> OnGameStateChanged;
        public event Action<GameState> OnGameStateEnter;
        public event Action<GameState> OnGameStateExit;
        
        // 個別状態変化イベント
        public event Action OnChangeToStarting;
        public event Action OnChangeToPlayerOneTurn;
        public event Action OnChangeToPlayerTwoTurn;
        public event Action OnChangeToDuel;
        public event Action OnChangeToPaused;
        public event Action OnChangeToGameOver;
        
        // プロパティ
        public GameState CurrentState => currentState;
        
        /// <summary>
        /// ゲーム状態を変更する
        /// </summary>
        /// <param name="newState">新しい状態</param>
        public void ChangeState(GameState newState)
        {
            if (currentState == newState) return;
            
            GameState previousState = currentState;
            
            // 現在の状態から退出
            OnGameStateExit?.Invoke(currentState);
            
            // 状態を変更
            currentState = newState;
            
            // 新しい状態に入る
            OnGameStateEnter?.Invoke(currentState);
            
            // 状態変更イベントを発火
            OnGameStateChanged?.Invoke(previousState, currentState);
            
            // 個別状態変化イベントを発火
            InvokeSpecificStateChangeEvent(newState);
            
            Debug.Log($"GameState changed from {previousState} to {currentState}");
        }
        
        /// <summary>
        /// 特定の状態変化イベントを発火する
        /// </summary>
        /// <param name="newState">新しい状態</param>
        private void InvokeSpecificStateChangeEvent(GameState newState)
        {
            switch (newState)
            {
                case GameState.Starting:
                    OnChangeToStarting?.Invoke();
                    break;
                case GameState.PlayerOneTurn:
                    OnChangeToPlayerOneTurn?.Invoke();
                    break;
                case GameState.PlayerTwoTurn:
                    OnChangeToPlayerTwoTurn?.Invoke();
                    break;
                case GameState.Duel:
                    OnChangeToDuel?.Invoke();
                    break;
                case GameState.Paused:
                    OnChangeToPaused?.Invoke();
                    break;
                case GameState.GameOver:
                    OnChangeToGameOver?.Invoke();
                    break;
            }
        }
        
        /// <summary>
        /// 状態変更イベントを購読する
        /// </summary>
        /// <param name="onStateChanged">状態変更時のコールバック</param>
        public void SubscribeToStateChange(Action<GameState, GameState> onStateChanged)
        {
            OnGameStateChanged += onStateChanged;
        }
        
        /// <summary>
        /// 状態変更イベントの購読を解除する
        /// </summary>
        /// <param name="onStateChanged">購読解除するコールバック</param>
        public void UnsubscribeFromStateChange(Action<GameState, GameState> onStateChanged)
        {
            OnGameStateChanged -= onStateChanged;
        }
        
        /// <summary>
        /// 状態入場イベントを購読する
        /// </summary>
        /// <param name="onStateEnter">状態入場時のコールバック</param>
        public void SubscribeToStateEnter(Action<GameState> onStateEnter)
        {
            OnGameStateEnter += onStateEnter;
        }
        
        /// <summary>
        /// 状態入場イベントの購読を解除する
        /// </summary>
        /// <param name="onStateEnter">購読解除するコールバック</param>
        public void UnsubscribeFromStateEnter(Action<GameState> onStateEnter)
        {
            OnGameStateEnter -= onStateEnter;
        }
        
        /// <summary>
        /// 状態退出イベントを購読する
        /// </summary>
        /// <param name="onStateExit">状態退出時のコールバック</param>
        public void SubscribeToStateExit(Action<GameState> onStateExit)
        {
            OnGameStateExit += onStateExit;
        }
        
        /// <summary>
        /// 状態退出イベントの購読を解除する
        /// </summary>
        /// <param name="onStateExit">購読解除するコールバック</param>
        public void UnsubscribeFromStateExit(Action<GameState> onStateExit)
        {
            OnGameStateExit -= onStateExit;
        }
        
        // 個別状態変化イベントの購読メソッド
        /// <summary>
        /// Starting状態への変化イベントを購読する
        /// </summary>
        /// <param name="onChangeToStarting">Starting状態への変化時のコールバック</param>
        public void SubscribeToChangeToStarting(Action onChangeToStarting)
        {
            OnChangeToStarting += onChangeToStarting;
        }
        
        /// <summary>
        /// Starting状態への変化イベントの購読を解除する
        /// </summary>
        /// <param name="onChangeToStarting">購読解除するコールバック</param>
        public void UnsubscribeFromChangeToStarting(Action onChangeToStarting)
        {
            OnChangeToStarting -= onChangeToStarting;
        }
        
        /// <summary>
        /// PlayerOneTurn状態への変化イベントを購読する
        /// </summary>
        /// <param name="onChangeToPlayerOneTurn">PlayerOneTurn状態への変化時のコールバック</param>
        public void SubscribeToChangeToPlayerOneTurn(Action onChangeToPlayerOneTurn)
        {
            OnChangeToPlayerOneTurn += onChangeToPlayerOneTurn;
        }
        
        /// <summary>
        /// PlayerOneTurn状態への変化イベントの購読を解除する
        /// </summary>
        /// <param name="onChangeToPlayerOneTurn">購読解除するコールバック</param>
        public void UnsubscribeFromChangeToPlayerOneTurn(Action onChangeToPlayerOneTurn)
        {
            OnChangeToPlayerOneTurn -= onChangeToPlayerOneTurn;
        }
        
        /// <summary>
        /// PlayerTwoTurn状態への変化イベントを購読する
        /// </summary>
        /// <param name="onChangeToPlayerTwoTurn">PlayerTwoTurn状態への変化時のコールバック</param>
        public void SubscribeToChangeToPlayerTwoTurn(Action onChangeToPlayerTwoTurn)
        {
            OnChangeToPlayerTwoTurn += onChangeToPlayerTwoTurn;
        }
        
        /// <summary>
        /// PlayerTwoTurn状態への変化イベントの購読を解除する
        /// </summary>
        /// <param name="onChangeToPlayerTwoTurn">購読解除するコールバック</param>
        public void UnsubscribeFromChangeToPlayerTwoTurn(Action onChangeToPlayerTwoTurn)
        {
            OnChangeToPlayerTwoTurn -= onChangeToPlayerTwoTurn;
        }
        
        /// <summary>
        /// Duel状態への変化イベントを購読する
        /// </summary>
        /// <param name="onChangeToDuel">Duel状態への変化時のコールバック</param>
        public void SubscribeToChangeToDuel(Action onChangeToDuel)
        {
            OnChangeToDuel += onChangeToDuel;
        }
        
        /// <summary>
        /// Duel状態への変化イベントの購読を解除する
        /// </summary>
        /// <param name="onChangeToDuel">購読解除するコールバック</param>
        public void UnsubscribeFromChangeToDuel(Action onChangeToDuel)
        {
            OnChangeToDuel -= onChangeToDuel;
        }
        
        /// <summary>
        /// Paused状態への変化イベントを購読する
        /// </summary>
        /// <param name="onChangeToPaused">Paused状態への変化時のコールバック</param>
        public void SubscribeToChangeToPaused(Action onChangeToPaused)
        {
            OnChangeToPaused += onChangeToPaused;
        }
        
        /// <summary>
        /// Paused状態への変化イベントの購読を解除する
        /// </summary>
        /// <param name="onChangeToPaused">購読解除するコールバック</param>
        public void UnsubscribeFromChangeToPaused(Action onChangeToPaused)
        {
            OnChangeToPaused -= onChangeToPaused;
        }
        
        /// <summary>
        /// GameOver状態への変化イベントを購読する
        /// </summary>
        /// <param name="onChangeToGameOver">GameOver状態への変化時のコールバック</param>
        public void SubscribeToChangeToGameOver(Action onChangeToGameOver)
        {
            OnChangeToGameOver += onChangeToGameOver;
        }
        
        /// <summary>
        /// GameOver状態への変化イベントの購読を解除する
        /// </summary>
        /// <param name="onChangeToGameOver">購読解除するコールバック</param>
        public void UnsubscribeFromChangeToGameOver(Action onChangeToGameOver)
        {
            OnChangeToGameOver -= onChangeToGameOver;
        }
        
        private void Start()
        {
            // 初期状態に入るイベントを発火
            OnGameStateEnter?.Invoke(currentState);
        }
        
        private void OnDestroy()
        {
            // メモリリーク防止のため、すべてのイベントを解除
            OnGameStateChanged = null;
            OnGameStateEnter = null;
            OnGameStateExit = null;
            
            // 個別状態変化イベントを解除
            OnChangeToStarting = null;
            OnChangeToPlayerOneTurn = null;
            OnChangeToPlayerTwoTurn = null;
            OnChangeToDuel = null;
            OnChangeToPaused = null;
            OnChangeToGameOver = null;
        }
    }
}