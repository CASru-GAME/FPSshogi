using UnityEngine;
using App.Common.Initialize;
using App.Main.Player;
using UnityEngine.InputSystem;
using App.Main.GameMaster;
using App.Main.ShogiThings;
using System.Collections.Generic;
using App.Main.Controller;
using App.Main.ViewManager;

namespace App.Main.ShogiPointer
{
    public class ShogiPointer : MonoBehaviour, IInitializable
    {
        public int InitializationPriority => 0;
        public System.Type[] Dependencies => new[] { typeof(ViewSelectingCapturedPiecePointer), typeof(ViewShogiPointer), typeof(PlayerManager), typeof(ShogiBoard), typeof(GameStateHolder) };
        private PlayerManager playerManager;
        private ShogiBoard shogiBoard;
        private GameStateHolder gameStateHolder;
        private ViewShogiPointer viewShogiPointer;
        private ViewSelectingCapturedPiecePointer viewCapturedPieces;
        private bool isOpenCapturedPiecesPanel;
        private bool isCapturedPiecesSelected;
        private int[] pointerPosition = { 0, 0 };
        private int[] selectedPiecePosition = { -1, -1 };
        private readonly CapturedPiecesPanelIndex capturedPiecesPanelIndex = new CapturedPiecesPanelIndex();
        public Dictionary<PieceType, int> playerOneCapturedPieces = new Dictionary<PieceType, int>();
        public Dictionary<PieceType, int> playerTwoCapturedPieces = new Dictionary<PieceType, int>();
        public Vector2Int CursorPosition => new Vector2Int(pointerPosition[0], pointerPosition[1]);
        public Vector2Int SelectedPosition => new Vector2Int(selectedPiecePosition[0], selectedPiecePosition[1]);
        public bool IsCapturedMode => isOpenCapturedPiecesPanel;
        public bool IsPlacingCaptured => isCapturedPiecesSelected;
        public PieceType CapturedType => capturedPiecesPanelIndex.GetCapturedPiecesType();
        public string Feedback { get; private set; } = "駒を選んでください";
        public int InvalidFeedbackVersion { get; private set; }
        void Reject(string reason){Feedback=reason;InvalidFeedbackVersion++;}
        public PlayerType ActivePlayer => gameStateHolder.CurrentState == GameStateHolder.GameState.PlayerTwoTurn ? PlayerType.PlayerTwo : PlayerType.PlayerOne;
        public bool IsBoardTurn => gameStateHolder != null && (gameStateHolder.CurrentState == GameStateHolder.GameState.PlayerOneTurn || gameStateHolder.CurrentState == GameStateHolder.GameState.PlayerTwoTurn);

        public void Initialize(ReferenceHolder references)
        {
            playerManager = references.GetInitializable<PlayerManager>();
            shogiBoard = references.GetInitializable<ShogiBoard>();
            gameStateHolder = references.GetInitializable<GameStateHolder>();
            viewShogiPointer = references.GetInitializable<ViewShogiPointer>();
            viewCapturedPieces = references.GetInitializable<ViewSelectingCapturedPiecePointer>();
            playerManager.PlayerOne.GetComponent<PlayerInput>().onActionTriggered += OnPlayerOneInput;
            playerManager.PlayerTwo.GetComponent<PlayerInput>().onActionTriggered += OnPlayerTwoInput;
            gameStateHolder.SubscribeToChangeToPlayerOneTurn(OnPlayerOneTurn);
            gameStateHolder.SubscribeToChangeToPlayerTwoTurn(OnPlayerTwoTurn);
            gameStateHolder.ChangeIntoPlayerOneTurn();
        }
        private void OnPlayerOneInput(InputAction.CallbackContext ctx) => OnAction(ctx, PlayerType.PlayerOne);
        private void OnPlayerTwoInput(InputAction.CallbackContext ctx) => OnAction(ctx, PlayerType.PlayerTwo);
        private void OnAction(InputAction.CallbackContext ctx, PlayerType source)
        {
            if (!IsBoardTurn || source != ActivePlayer || ctx.phase != InputActionPhase.Started || App.UI.GameSessionUI.BlocksGameplay) return;
            switch (ctx.action.name)
            {
                case "SelectUp": if (!IsCapturedMode) SelectPositionUp(); break;
                case "SelectDown": if (!IsCapturedMode) SelectPositionDown(); break;
                case "SelectLeft": if (IsCapturedMode) CycleCaptured(-1); else SelectPositionLeft(); break;
                case "SelectRight": if (IsCapturedMode) CycleCaptured(1); else SelectPositionRight(); break;
                case "Select": Select(); break;
                case "Cancel": Deselect(); break;
                case "UseCaptured": ToggleCapturedPiecesPanel(); break;
            }
        }
        private void ResetSelection(int cursor)
        {
            pointerPosition = new[] { cursor, cursor };
            selectedPiecePosition = new[] { -1, -1 };
            isCapturedPiecesSelected = isOpenCapturedPiecesPanel = false;
            Feedback = "駒を選んでください";
        }
        private void OnPlayerOneTurn() => ResetSelection(0);
        private void OnPlayerTwoTurn() => ResetSelection(8);
        public void SelectPositionUp() => SetCursor(pointerPosition[0], pointerPosition[1] + 1);
        public void SelectPositionDown() => SetCursor(pointerPosition[0], pointerPosition[1] - 1);
        public void SelectPositionLeft() => SetCursor(pointerPosition[0] - 1, pointerPosition[1]);
        public void SelectPositionRight() => SetCursor(pointerPosition[0] + 1, pointerPosition[1]);
        public void SetCursor(int x, int y)
        {
            pointerPosition[0] = Mathf.Clamp(x, 0, 8);
            pointerPosition[1] = Mathf.Clamp(y, 0, 8);
        }
        public void Select()
        {
            if (!IsBoardTurn) return;
            if (IsPlacingCaptured) { SetCapturedPieceOnBoard(); return; }
            if (IsCapturedMode) { ChooseCaptured(CapturedType); return; }
            var piece = shogiBoard.GetPieceAt(pointerPosition[0], pointerPosition[1]);
            if (piece != null && piece.Player == ActivePlayer)
            {
                selectedPiecePosition = (int[])pointerPosition.Clone();
                Feedback = "光っているマスへ移動できます。決定で移動 / 取消で解除";
                return;
            }
            if (selectedPiecePosition[0] < 0) { Reject("自分の駒を選んでください"); return; }
            var result = shogiBoard.MovePiece(selectedPiecePosition[0], selectedPiecePosition[1], pointerPosition[0], pointerPosition[1], ActivePlayer);
            if (result == ShogiBoard.MoveResult.InvalidMove) Reject("そこへは移動できません。光っているマスを選んでください");
        }
        public void SetCapturedPieceOnBoard()
        {
            if (!IsBoardTurn) return;
            var result = shogiBoard.SetPiece(pointerPosition[0], pointerPosition[1], CapturedType, ActivePlayer);
            if (result == ShogiBoard.MoveResult.InvalidMove) Reject(shogiBoard.GetDropProblem(pointerPosition[0],pointerPosition[1],CapturedType,ActivePlayer) switch {
                ShogiBoard.DropProblem.Nifu=>"同じ筋に歩があります（二歩）",ShogiBoard.DropProblem.Occupied=>"駒のないマスを選んでください",ShogiBoard.DropProblem.DeadEnd=>"その位置では、次に動けなくなります",ShogiBoard.DropProblem.Unavailable=>"その持ち駒はありません",ShogiBoard.DropProblem.Outside=>"盤の中を選んでください",_=>"今は持ち駒を打てません"
            });
            else { isCapturedPiecesSelected = isOpenCapturedPiecesPanel = false; }
        }
        public void ChooseCaptured(PieceType type)
        {
            if (!IsBoardTurn) return;
            var counts = shogiBoard.GetCapturedPieces(ActivePlayer);
            if (!counts.TryGetValue(type, out int count) || count <= 0) { Reject("その持ち駒はありません"); return; }
            capturedPiecesPanelIndex.SetCapturedPiecesPanelIndex((int)type);
            isCapturedPiecesSelected = true;
            isOpenCapturedPiecesPanel = false;
            selectedPiecePosition = new[] { -1, -1 };
            Feedback = "持ち駒を打つマスを選んでください / 取消で解除";
        }
        public void ToggleCapturedPiecesPanel()
        {
            if (!IsBoardTurn) return;
            if (IsCapturedMode || IsPlacingCaptured) { Deselect(); return; }
            var counts = shogiBoard.GetCapturedPieces(ActivePlayer);
            if (counts.Count == 0) { Reject("持ち駒はまだありません"); return; }
            isOpenCapturedPiecesPanel = true;
            selectedPiecePosition = new[] { -1, -1 };
            capturedPiecesPanelIndex.SetCapturedPiecesPanelIndex(0);
            CycleCaptured(1);
            Feedback = "持ち駒を選択中：左右で選択 / 決定で打つ / 取消で戻る";
        }
        private void CycleCaptured(int direction)
        {
            var counts = shogiBoard.GetCapturedPieces(ActivePlayer);
            int index = capturedPiecesPanelIndex.GetIndex();
            for (int i = 0; i < 7; i++)
            {
                index += direction;
                if (index > 7) index = 1;
                if (index < 1) index = 7;
                if (counts.TryGetValue((PieceType)index, out int count) && count > 0) { capturedPiecesPanelIndex.SetCapturedPiecesPanelIndex(index); break; }
            }
        }
        public void Deselect()
        {
            selectedPiecePosition = new[] { -1, -1 };
            isCapturedPiecesSelected = isOpenCapturedPiecesPanel = false;
            Feedback = "選択を解除しました。駒を選んでください";
        }
        private void Update()
        {
            // The screen-space board view supplies accessible selection outlines.
            viewShogiPointer?.SetVisible(false);
            viewCapturedPieces?.SetVisible(false);
        }
        private void OnDestroy()
        {
            if (playerManager != null)
            {
                if (playerManager.PlayerOne != null) playerManager.PlayerOne.GetComponent<PlayerInput>().onActionTriggered -= OnPlayerOneInput;
                if (playerManager.PlayerTwo != null) playerManager.PlayerTwo.GetComponent<PlayerInput>().onActionTriggered -= OnPlayerTwoInput;
            }
            if (gameStateHolder != null)
            {
                gameStateHolder.UnsubscribeFromChangeToPlayerOneTurn(OnPlayerOneTurn);
                gameStateHolder.UnsubscribeFromChangeToPlayerTwoTurn(OnPlayerTwoTurn);
            }
        }
    }
}
