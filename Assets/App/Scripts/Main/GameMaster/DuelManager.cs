using UnityEngine;
using App.Common.Initialize;

namespace App.Main.GameMaster
{
    public class DuelManager : MonoBehaviour, IInitializable
    {
        private ShogiBoard shogiBoard = null;
        private GameStateHolder gameStateHolder = null;
        public int InitializationPriority => 0;
        public System.Type[] Dependencies => new System.Type[] { };
        [SerializeField] public GameObject PlayerOne;
        [SerializeField] public GameObject PlayerTwo;
        public void Initialize(ReferenceHolder referenceHolder)
        {
            Debug.Log("DuelManager initialized.");
            shogiBoard = referenceHolder.GetInitializable<ShogiBoard>();
            gameStateHolder = referenceHolder.GetInitializable<GameStateHolder>();

            gameStateHolder.SubscribeToChangeToDuel(OnChangeToDuel);
        }

        private void OnChangeToDuel()
        {

        }

        public void OnDestroy()
        {
            if (gameStateHolder != null)
            {
                gameStateHolder.UnsubscribeFromChangeToDuel(OnChangeToDuel);
            }
        }
    }
}
