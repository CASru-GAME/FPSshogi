using UnityEngine;
using App.Common.Initialize;
using App.Main.Player;

namespace App.Main.GameMaster
{
    public class DuelManager : MonoBehaviour, IInitializable
    {
        private ShogiBoard shogiBoard = null;
        private GameStateHolder gameStateHolder = null;
        private PlayerManager playerManager = null;
        public int InitializationPriority => 0;
        public System.Type[] Dependencies => new System.Type[] { typeof(ShogiBoard), typeof(GameStateHolder), typeof(PlayerManager) };
        public GameObject PlayerOne { get; private set; }
        public GameObject PlayerTwo { get; private set; }
        public void Initialize(ReferenceHolder referenceHolder)
        {
            shogiBoard = referenceHolder.GetInitializable<ShogiBoard>();
            gameStateHolder = referenceHolder.GetInitializable<GameStateHolder>();
            playerManager = referenceHolder.GetInitializable<PlayerManager>();
        }

        public void ChangeStateToPlayerOneWin()
        {
            gameStateHolder.ChangeStateIntoDuelPlayerOneWin();
        }

        public void ChangeStateToPlayerTwoWin()
        {
            Debug.Log("Player Two wins.");
            gameStateHolder.ChangeStateIntoDuelPlayerTwoWin();
        }

        // Physics, weapon Update and projectile coroutines may all deal damage
        // in this frame. Resolve only after those sources have had their turn.
        public void LateUpdate()
        {
            if (gameStateHolder.CurrentState != GameStateHolder.GameState.Duel || Time.timeScale<=0 || App.UI.GameSessionUI.BlocksGameplay) return;
            bool oneDead=playerManager.PlayerOne.GetComponent<Player.Player>().playerStatus.Hp.Current<=0;
            bool twoDead=playerManager.PlayerTwo.GetComponent<Player.Player>().playerStatus.Hp.Current<=0;
            if(oneDead&&twoDead)
            {
                gameStateHolder.ChangeState(GameStateHolder.GameState.DuelDraw);
            }
            else if (oneDead)
            {
                Debug.Log("Player One HP is 0 or less. Player Two wins.");
                ChangeStateToPlayerTwoWin();
            }
            else if (twoDead)
            {
                Debug.Log("Player Two HP is 0 or less. Player One wins.");
                ChangeStateToPlayerOneWin();
            }
        }

        public void OnDestroy()
        {
        }
    }
}
