using UnityEngine;
using App.Common.Initialize;
using App.Main.Player;
using UnityEngine.InputSystem;

namespace App.Main.ShogiPointer
{
    public class ShogiPointer : MonoBehaviour, IInitializable
    {
        public int InitializationPriority => 0;
        public System.Type[] Dependencies => new System.Type[] { typeof(PlayerManager) };

        private PlayerManager playerManager;

        private int[] selectedPositions = new int[2];
        public void Initialize(ReferenceHolder referenceHolder)
        {
            // 初期化処理をここに記述
            playerManager = referenceHolder.GetInitializable<PlayerManager>();
            selectedPositions = new int[] { 0, 0 };

            playerManager.PlayerOne.GetComponent<PlayerInput>().onActionTriggered += OnActionTriggered;
            playerManager.PlayerTwo.GetComponent<PlayerInput>().onActionTriggered += OnActionTriggered;
        }

        private void OnActionTriggered(InputAction.CallbackContext context)
        {            
            if (context.phase != InputActionPhase.Started) return;

            if (context.action.name == "SelectUp")
            {
                SelectPositionUp();
            }
            else if (context.action.name == "SelectDown")
            {
                SelectPositionDown();
            }
            else if (context.action.name == "SelectLeft")
            {
                SelectPositionLeft();
            }
            else if (context.action.name == "SelectRight")
            {
                SelectPositionRight();
            }
            else if (context.action.name == "Select")
            {
                Select();
            }
        }

        public void SelectPositionUp()
        {
            if (selectedPositions[1] < 8) selectedPositions[1]++;
            Debug.Log("Selected Position: (" + selectedPositions[0] + ", " + selectedPositions[1] + ")");
        }
        public void SelectPositionDown()
        {
            if (selectedPositions[1] > 0) selectedPositions[1]--;
            Debug.Log("Selected Position: (" + selectedPositions[0] + ", " + selectedPositions[1] + ")");
        }
        public void SelectPositionLeft()
        {
            if (selectedPositions[0] > 0) selectedPositions[0]--;
            Debug.Log("Selected Position: (" + selectedPositions[0] + ", " + selectedPositions[1] + ")");
        }
        public void SelectPositionRight()
        {
            if (selectedPositions[0] < 8) selectedPositions[0]++;  
            Debug.Log("Selected Position: (" + selectedPositions[0] + ", " + selectedPositions[1] + ")");
        }

        public void Select()
        {
            
        }

        public void OnDestroy()
        {
            // クリーンアップ処理をここに記述
        }
    }
}
