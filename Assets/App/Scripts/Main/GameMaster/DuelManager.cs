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

        public void Update()
        {
            if (gameStateHolder.CurrentState != GameStateHolder.GameState.Duel) return;
            if (playerManager.PlayerOne.GetComponent<Player.Player>().playerStatus.Hp.Current <= 0)
            {
                Debug.Log("Player One HP is 0 or less. Player Two wins.");
                ChangeStateToPlayerTwoWin();
            }
            else if (playerManager.PlayerTwo.GetComponent<Player.Player>().playerStatus.Hp.Current <= 0)
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
