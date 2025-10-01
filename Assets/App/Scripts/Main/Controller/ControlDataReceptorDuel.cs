using App.Common.Initialize;
using UnityEngine;
using UnityEngine.InputSystem;
using App.Main.GameMaster;
using App.Main.Player;

namespace App.Main.Controller
{
    public class ControlDataReceptorDuel : MonoBehaviour, IInitializable
    {
        private Common.Controller.Controller controller;
        private DuelManager duelManager;
        private Player.Player playerOne;
        private Player.Player playerTwo;
        public int InitializationPriority => 0;
        public System.Type[] Dependencies => new System.Type[] { typeof(Common.Controller.Controller), typeof(DuelManager) };
        public void Initialize(ReferenceHolder referenceHolder)
        {
            controller = referenceHolder.GetInitializable<Common.Controller.Controller>();
            duelManager = referenceHolder.GetInitializable<DuelManager>();
            playerOne = duelManager.PlayerOne.GetComponent<Player.Player>();
            playerTwo = duelManager.PlayerTwo.GetComponent<Player.Player>();
            controller.EnablePlayerInput();
            controller.SubscribeToMove(OnMove);
        }

        private void OnMove(InputAction.CallbackContext context)
        {
            Vector2 move = context.ReadValue<Vector2>();
            playerOne.SetMoveVelocity(move);
            Debug.Log(move); // 例: (0, 1) なら「上」入力
        }
    }
}
