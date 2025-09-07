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
            
            Debug.Log($"GameState changed from {previousState} to {currentState}");
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
        }
    }
}