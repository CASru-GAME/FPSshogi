using App.Common.Initialize;
using App.Main.GameMaster;
using UnityEngine;

namespace App.Main.Controller
{
    public class ControlDataReceptorPlayerOneTurn : MonoBehaviour, IInitializable
    {
        private Common.Controller.Controller controller;
        private GameStateHolder gameStateHolder;
        public int InitializationPriority => 0;
        public System.Type[] Dependencies => new System.Type[] { typeof(Common.Controller.Controller), typeof(GameStateHolder) };
        public void Initialize(ReferenceHolder referenceHolder)
        {
            controller = referenceHolder.GetInitializable<Common.Controller.Controller>();
            gameStateHolder = referenceHolder.GetInitializable<GameStateHolder>();
            controller.EnableUIInput();
        }
    }
}
