using UnityEngine;
using App.Common.Initialize;
using App.Main.Player;
using UnityEngine.InputSystem;
using App.Main.GameMaster;
using App.Main.ShogiThings;

namespace App.Main.ShogiPointer
{
    public class ShogiPointer : MonoBehaviour, IInitializable
    {
        public int InitializationPriority => 0;
        public System.Type[] Dependencies => new System.Type[] { typeof(PlayerManager), typeof(ShogiBoard), typeof(GameStateHolder) };

        private PlayerManager playerManager;
        private ShogiBoard shogiBoard;
        private GameStateHolder gameStateHolder;
        private int[] pointerPosition = new int[2];
        private int[] selectedPiecePosition = new int[2];
        public void Initialize(ReferenceHolder referenceHolder)
        {
            // 初期化処理をここに記述
            playerManager = referenceHolder.GetInitializable<PlayerManager>();
            shogiBoard = referenceHolder.GetInitializable<ShogiBoard>();
            gameStateHolder = referenceHolder.GetInitializable<GameStateHolder>();
            pointerPosition = new int[] { 0, 0 };
            selectedPiecePosition = new int[] { -1, -1 };

            playerManager.PlayerOne.GetComponent<PlayerInput>().onActionTriggered += ctx => OnActionTriggered(ctx, playerManager.PlayerOne.GetComponent<PlayerInput>());
            playerManager.PlayerTwo.GetComponent<PlayerInput>().onActionTriggered += ctx => OnActionTriggered(ctx, playerManager.PlayerTwo.GetComponent<PlayerInput>());
        }

        private void OnActionTriggered(InputAction.CallbackContext context, PlayerInput sourcePlayerInput)
        {
            if (context.phase != InputActionPhase.Started) return;

            if (gameStateHolder.CurrentState == GameStateHolder.GameState.PlayerOneTurn && sourcePlayerInput.playerIndex != playerManager.PlayerIndexPlayerOne)
            {
                return;
            }
            if (gameStateHolder.CurrentState == GameStateHolder.GameState.PlayerTwoTurn && sourcePlayerInput.playerIndex != playerManager.PlayerIndexPlayerTwo)
            {
                return;
            }


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
            else if (context.action.name == "Cancel")
            {
                Deselect();
            }
        }

        public void SelectPositionUp()
        {
            if (pointerPosition[1] < 8) pointerPosition[1]++;
            Debug.Log("Selected Position: (" + pointerPosition[0] + ", " + pointerPosition[1] + ")");
        }
        public void SelectPositionDown()
        {
            if (pointerPosition[1] > 0) pointerPosition[1]--;
            Debug.Log("Selected Position: (" + pointerPosition[0] + ", " + pointerPosition[1] + ")");
        }
        public void SelectPositionLeft()
        {
            if (pointerPosition[0] > 0) pointerPosition[0]--;
            Debug.Log("Selected Position: (" + pointerPosition[0] + ", " + pointerPosition[1] + ")");
        }
        public void SelectPositionRight()
        {
            if (pointerPosition[0] < 8) pointerPosition[0]++;
            Debug.Log("Selected Position: (" + pointerPosition[0] + ", " + pointerPosition[1] + ")");
        }

        public void Select()
        {
            if (gameStateHolder.CurrentState == GameStateHolder.GameState.PlayerOneTurn)
            {
                if (selectedPiecePosition[0] != -1 && selectedPiecePosition[1] != -1)
                {
                    // すでに駒が選択されている場合、移動を試みる
                    ShogiBoard.MoveResult moveSuccessful = shogiBoard.MovePiece(selectedPiecePosition[0], selectedPiecePosition[1], pointerPosition[0], pointerPosition[1], PlayerType.PlayerOne);
                    Debug.Log("Attempted Move Result: " + moveSuccessful);
                    if (moveSuccessful == ShogiBoard.MoveResult.NormalMove)
                    {
                        Debug.Log("Moved Piece to: (" + pointerPosition[0] + ", " + pointerPosition[1] + ")");
                        selectedPiecePosition[0] = -1;
                        selectedPiecePosition[1] = -1;
                    }
                }
                else
                {
                    // 駒を選択する
                    IPiece piece = shogiBoard.GetPieceAt(pointerPosition[0], pointerPosition[1]);
                    if (piece != null && piece.Player == PlayerType.PlayerOne)
                    {
                        selectedPiecePosition[0] = pointerPosition[0];
                        selectedPiecePosition[1] = pointerPosition[1];
                        Debug.Log("Selected Piece at: (" + selectedPiecePosition[0] + ", " + selectedPiecePosition[1] + ")");
                        Debug.Log("Piece Info: " + piece.GetType().Name);
                    }
                }
            }
            else if (gameStateHolder.CurrentState == GameStateHolder.GameState.PlayerTwoTurn)
            {
                if (selectedPiecePosition[0] != -1 && selectedPiecePosition[1] != -1)
                {
                    // すでに駒が選択されている場合、移動を試みる
                    ShogiBoard.MoveResult moveSuccessful = shogiBoard.MovePiece(selectedPiecePosition[0], selectedPiecePosition[1], pointerPosition[0], pointerPosition[1], PlayerType.PlayerTwo);
                    Debug.Log("Attempted Move Result: " + moveSuccessful);
                    if (moveSuccessful == ShogiBoard.MoveResult.NormalMove)
                    {
                        selectedPiecePosition[0] = -1;
                        selectedPiecePosition[1] = -1;
                    }
                }
                else
                {
                    // 駒を選択する
                    IPiece piece = shogiBoard.GetPieceAt(pointerPosition[0], pointerPosition[1]);
                    if (piece != null && piece.Player == PlayerType.PlayerTwo)
                    {
                        selectedPiecePosition[0] = pointerPosition[0];
                        selectedPiecePosition[1] = pointerPosition[1];
                        Debug.Log("Selected Piece at: (" + selectedPiecePosition[0] + ", " + selectedPiecePosition[1] + ")");
                        Debug.Log("Piece Info: " + piece.GetType().Name);
                    }
                }
            }
        }

        public void Deselect()
        {
            selectedPiecePosition[0] = -1;
            selectedPiecePosition[1] = -1;

            Debug.Log("Deselected");
        }

        public void OnDestroy()
        {
            // クリーンアップ処理をここに記述
        }
    }
}
