using App.Main.ShogiThings;

namespace App.Main.ShogiPointer
{
    public  class CapturedPiecesPanelIndex
    {
        private int capturedPiecesPanelIndex = 0;
        
        public PieceType GetCapturedPiecesType()
        {
            return (PieceType)capturedPiecesPanelIndex;
        }

        public void SetCapturedPiecesPanelIndex(int index)
        {
            capturedPiecesPanelIndex = index;
        }

        public void IncrementIndex()
        {
            if (capturedPiecesPanelIndex > 6) return;
            capturedPiecesPanelIndex++;
        }
        public void DecrementIndex()
        {
            if (capturedPiecesPanelIndex < 1) return;
            capturedPiecesPanelIndex--;
        }

        public void ResetIndex()
        {
            capturedPiecesPanelIndex = 0;
        }
    }
}