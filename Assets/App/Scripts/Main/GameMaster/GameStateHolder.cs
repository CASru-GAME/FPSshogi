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
            DuelPlayerOneWin,
            DuelPlayerTwoWin,
            Paused,
            PlayerOneWin,
            PlayerTwoWin
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
        public event Action OnChangeToDuelPlayerOneWin;
        public event Action OnChangeToDuelPlayerTwoWin;
        public event Action OnChangeToPaused;
        public event Action OnChangeToPlayerOneWin;
        public event Action OnChangeToPlayerTwoWin;
        
        // プロパティ
        public GameState CurrentState => currentState;
        
        /// <summary>
        /// ゲーム状態を変更する
        /// </summary>
        /// <param name="newState">新しい状態</param>
        public void ChangeState(GameState newState)
        {
            if (currentState == newState) return;
            
            // 許可された状態遷移かチェック
            if (!IsValidStateTransition(currentState, newState))
            {
                Debug.LogWarning($"Invalid state transition from {currentState} to {newState}");
                return;
            }
            
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
        /// 状態遷移が有効かどうかをチェックする
        /// </summary>
        /// <param name="from">遷移元の状態</param>
        /// <param name="to">遷移先の状態</param>
        /// <returns>有効な遷移の場合true</returns>
        private bool IsValidStateTransition(GameState from, GameState to)
        {
            switch (from)
            {
                case GameState.Starting:
                    return to == GameState.PlayerOneTurn;
                
                case GameState.PlayerOneTurn:
                    return to == GameState.PlayerTwoTurn || to == GameState.Duel || to == GameState.Paused;
                
                case GameState.PlayerTwoTurn:
                    return to == GameState.PlayerOneTurn || to == GameState.Duel || to == GameState.Paused;
                
                case GameState.Duel:
                    return to == GameState.DuelPlayerOneWin || to == GameState.DuelPlayerTwoWin || to == GameState.Paused;
                
                case GameState.DuelPlayerOneWin:
                    return to == GameState.PlayerOneWin;
                
                case GameState.DuelPlayerTwoWin:
                    return to == GameState.PlayerTwoWin;
                
                case GameState.Paused:
                    // ポーズから元の状態に戻ることを許可（実装に応じて調整）
                    return to == GameState.PlayerOneTurn || to == GameState.PlayerTwoTurn || to == GameState.Duel;
                
                case GameState.PlayerOneWin:
                case GameState.PlayerTwoWin:
                    // 勝利状態からは遷移不可（ゲーム終了）
                    return false;
                
                default:
                    return false;
            }
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
                case GameState.DuelPlayerOneWin:
                    OnChangeToDuelPlayerOneWin?.Invoke();
                    break;
                case GameState.DuelPlayerTwoWin:
                    OnChangeToDuelPlayerTwoWin?.Invoke();
                    break;
                case GameState.Paused:
                    OnChangeToPaused?.Invoke();
                    break;
                case GameState.PlayerOneWin:
                    OnChangeToPlayerOneWin?.Invoke();
                    break;
                case GameState.PlayerTwoWin:
                    OnChangeToPlayerTwoWin?.Invoke();
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
        /// PlayerOneWin状態への変化イベントを購読する
        /// </summary>
        /// <param name="onChangeToPlayerOneWin">PlayerOneWin状態への変化時のコールバック</param>
        public void SubscribeToChangeToPlayerOneWin(Action onChangeToPlayerOneWin)
        {
            OnChangeToPlayerOneWin += onChangeToPlayerOneWin;
        }
        
        /// <summary>
        /// PlayerOneWin状態への変化イベントの購読を解除する
        /// </summary>
        /// <param name="onChangeToPlayerOneWin">購読解除するコールバック</param>
        public void UnsubscribeFromChangeToPlayerOneWin(Action onChangeToPlayerOneWin)
        {
            OnChangeToPlayerOneWin -= onChangeToPlayerOneWin;
        }
        
        /// <summary>
        /// PlayerTwoWin状態への変化イベントを購読する
        /// </summary>
        /// <param name="onChangeToPlayerTwoWin">PlayerTwoWin状態への変化時のコールバック</param>
        public void SubscribeToChangeToPlayerTwoWin(Action onChangeToPlayerTwoWin)
        {
            OnChangeToPlayerTwoWin += onChangeToPlayerTwoWin;
        }
        
        /// <summary>
        /// PlayerTwoWin状態への変化イベントの購読を解除する
        /// </summary>
        /// <param name="onChangeToPlayerTwoWin">購読解除するコールバック</param>
        public void UnsubscribeFromChangeToPlayerTwoWin(Action onChangeToPlayerTwoWin)
        {
            OnChangeToPlayerTwoWin -= onChangeToPlayerTwoWin;
        }
        
        /// <summary>
        /// DuelPlayerOneWin状態への変化イベントを購読する
        /// </summary>
        /// <param name="onChangeToDuelPlayerOneWin">DuelPlayerOneWin状態への変化時のコールバック</param>
        public void SubscribeToChangeToDuelPlayerOneWin(Action onChangeToDuelPlayerOneWin)
        {
            OnChangeToDuelPlayerOneWin += onChangeToDuelPlayerOneWin;
        }
        
        /// <summary>
        /// DuelPlayerOneWin状態への変化イベントの購読を解除する
        /// </summary>
        /// <param name="onChangeToDuelPlayerOneWin">購読解除するコールバック</param>
        public void UnsubscribeFromChangeToDuelPlayerOneWin(Action onChangeToDuelPlayerOneWin)
        {
            OnChangeToDuelPlayerOneWin -= onChangeToDuelPlayerOneWin;
        }
        
        /// <summary>
        /// DuelPlayerTwoWin状態への変化イベントを購読する
        /// </summary>
        /// <param name="onChangeToDuelPlayerTwoWin">DuelPlayerTwoWin状態への変化時のコールバック</param>
        public void SubscribeToChangeToDuelPlayerTwoWin(Action onChangeToDuelPlayerTwoWin)
        {
            OnChangeToDuelPlayerTwoWin += onChangeToDuelPlayerTwoWin;
        }
        
        /// <summary>
        /// DuelPlayerTwoWin状態への変化イベントの購読を解除する
        /// </summary>
        /// <param name="onChangeToDuelPlayerTwoWin">購読解除するコールバック</param>
        public void UnsubscribeFromChangeToDuelPlayerTwoWin(Action onChangeToDuelPlayerTwoWin)
        {
            OnChangeToDuelPlayerTwoWin -= onChangeToDuelPlayerTwoWin;
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
            OnChangeToDuelPlayerOneWin = null;
            OnChangeToDuelPlayerTwoWin = null;
            OnChangeToPaused = null;
            OnChangeToPlayerOneWin = null;
            OnChangeToPlayerTwoWin = null;
        }
    }
}
