using UnityEngine;
using App.Common.Initialize;
using App.Main.Player;
using UnityEngine.InputSystem;
using App.Main.GameMaster;
using App.Main.ShogiThings;
using System.Collections.Generic;
using App.Main.Controller;

namespace App.Main.ShogiPointer
{
    public class ShogiPointer : MonoBehaviour, IInitializable
    {
        public int InitializationPriority => 0;
        public System.Type[] Dependencies => new System.Type[] { typeof(ViewShogiPointer), typeof(PlayerManager), typeof(ShogiBoard), typeof(GameStateHolder) };

        private PlayerManager playerManager;
        private ShogiBoard shogiBoard;
        private GameStateHolder gameStateHolder;
        bool isOpenCapturedPiecesPanel = false;
        bool isCapturedPiecesSelected = false;
        public Dictionary<PieceType, int> playerOneCapturedPieces = new Dictionary<PieceType, int>();
        public Dictionary<PieceType, int> playerTwoCapturedPieces = new Dictionary<PieceType, int>();
        CapturedPiecesPanelIndex capturedPiecesPanelIndex = new CapturedPiecesPanelIndex();
        private ViewShogiPointer viewShogiPointer;

        private int[] pointerPosition = new int[2];
        private int[] selectedPiecePosition = new int[2];
        public void Initialize(ReferenceHolder referenceHolder)
        {
            // 初期化処理をここに記述
            playerManager = referenceHolder.GetInitializable<PlayerManager>();
            shogiBoard = referenceHolder.GetInitializable<ShogiBoard>();
            gameStateHolder = referenceHolder.GetInitializable<GameStateHolder>();
            viewShogiPointer = referenceHolder.GetInitializable<ViewShogiPointer>();
            pointerPosition = new int[] { 0, 0 };
            selectedPiecePosition = new int[] { -1, -1 };

            playerManager.PlayerOne.GetComponent<PlayerInput>().onActionTriggered += ctx => OnActionTriggered(ctx, playerManager.PlayerOne.GetComponent<PlayerInput>());
            playerManager.PlayerTwo.GetComponent<PlayerInput>().onActionTriggered += ctx => OnActionTriggered(ctx, playerManager.PlayerTwo.GetComponent<PlayerInput>());
            gameStateHolder.SubscribeToChangeToPlayerOneTurn(OnPlayerOneTurn);
            gameStateHolder.SubscribeToChangeToPlayerTwoTurn(OnPlayerTwoTurn);
        }

        public void Update()
        {
            viewShogiPointer?.ChangePointerPosition(pointerPosition);
        }

        private void OnPlayerOneTurn()
        {
            pointerPosition = new int[] { 0, 0 };
            selectedPiecePosition = new int[] { -1, -1 };
        }

        private void OnPlayerTwoTurn()
        {
            pointerPosition = new int[] { 8, 8 };
            selectedPiecePosition = new int[] { -1, -1 };
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

            else if (context.action.name == "SelectUp")
            {
                if (isOpenCapturedPiecesPanel) return;
                else SelectPositionUp();
            }
            else if (context.action.name == "SelectDown")
            {
                if (isOpenCapturedPiecesPanel) return;
                else SelectPositionDown();
            }
            else if (context.action.name == "SelectLeft")
            {
                if (isOpenCapturedPiecesPanel)
                {
                    capturedPiecesPanelIndex.DecrementIndex();
                    Debug.Log("Captured Pieces Panel Index: " + capturedPiecesPanelIndex.GetCapturedPiecesType());
                }

                else SelectPositionLeft();
            }
            else if (context.action.name == "SelectRight")
            {
                if (isOpenCapturedPiecesPanel)
                {
                    capturedPiecesPanelIndex.IncrementIndex();
                    Debug.Log("Captured Pieces Panel Index: " + capturedPiecesPanelIndex.GetCapturedPiecesType());
                }

                else SelectPositionRight();
            }
            else if (context.action.name == "Select")
            {
                if (isCapturedPiecesSelected) SetCapturedPieceOnBoard();
                else if (isOpenCapturedPiecesPanel)
                {
                    isCapturedPiecesSelected = true;
                    ToggleCapturedPiecesPanel();
                    Debug.Log("Selected Captured Piece: " + capturedPiecesPanelIndex.GetCapturedPiecesType());
                }
                else Select();
            }
            else if (context.action.name == "Cancel")
            {
                if (isCapturedPiecesSelected)
                {
                    isCapturedPiecesSelected = false;
                    Debug.Log("Cancelled Captured Piece Selection");
                }
                else if (isOpenCapturedPiecesPanel) ToggleCapturedPiecesPanel();
                else Deselect();
            }
            else if (context.action.name == "UseCaptured")
            {
                ToggleCapturedPiecesPanel();
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

        public void SetCapturedPieceOnBoard()
        {
            if (gameStateHolder.CurrentState == GameStateHolder.GameState.PlayerOneTurn)
            {
                PieceType pieceType = capturedPiecesPanelIndex.GetCapturedPiecesType();
                ShogiBoard.MoveResult moveResult = shogiBoard.SetPiece(pointerPosition[0], pointerPosition[1], pieceType, PlayerType.PlayerOne);
                Debug.Log("Attempted to Place Captured Piece: " + pieceType + " at (" + pointerPosition[0] + ", " + pointerPosition[1] + ") Result: " + moveResult);
                if (moveResult == ShogiBoard.MoveResult.NormalMove)
                {
                    isCapturedPiecesSelected = false;
                    Debug.Log("Placed Captured Piece: " + pieceType + " at (" + pointerPosition[0] + ", " + pointerPosition[1] + ")");
                }
            }
            else if (gameStateHolder.CurrentState == GameStateHolder.GameState.PlayerTwoTurn)
            {
                PieceType pieceType = capturedPiecesPanelIndex.GetCapturedPiecesType();
                ShogiBoard.MoveResult moveResult = shogiBoard.SetPiece(pointerPosition[0], pointerPosition[1], pieceType, PlayerType.PlayerTwo);
                Debug.Log("Attempted to Place Captured Piece: " + pieceType + " at (" + pointerPosition[0] + ", " + pointerPosition[1] + ") Result: " + moveResult);
                if (moveResult == ShogiBoard.MoveResult.NormalMove)
                {
                    isCapturedPiecesSelected = false;
                    Debug.Log("Placed Captured Piece: " + pieceType + " at (" + pointerPosition[0] + ", " + pointerPosition[1] + ")");
                }
            }
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

        public void ToggleCapturedPiecesPanel()
        {
            isOpenCapturedPiecesPanel = !isOpenCapturedPiecesPanel;
            if (gameStateHolder.CurrentState == GameStateHolder.GameState.PlayerOneTurn)
                playerOneCapturedPieces = shogiBoard.GetCapturedPieces(PlayerType.PlayerOne);
            else if (gameStateHolder.CurrentState == GameStateHolder.GameState.PlayerTwoTurn)
                playerTwoCapturedPieces = shogiBoard.GetCapturedPieces(PlayerType.PlayerTwo);
            Debug.Log("Captured Pieces Panel is now " + (isOpenCapturedPiecesPanel ? "Open" : "Closed"));
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
