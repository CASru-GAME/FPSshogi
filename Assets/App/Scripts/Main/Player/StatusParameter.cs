using App.Main.ShogiThings;
using UnityEngine;

namespace App.Main.Player
{
    [CreateAssetMenu(fileName = "StatusParameter", menuName = "ScriptableObjects/Player/StatusParameter")]
    public class StatusParameter : ScriptableObject
    {
        [SerializeField] public int pieceTypeIndex;
        [SerializeField] public int hpMax;
        [SerializeField] public int attackPointDefault;
        [SerializeField] public int defensePointDefault;
        [SerializeField] public float moveSpeedDefault;
        [SerializeField] public int hpMaxUra;
        [SerializeField] public int attackPointDefaultUra;
        [SerializeField] public int defensePointDefaultUra;
        [SerializeField] public float moveSpeedDefaultUra;

        public PlayerStatus CreatePlayerStatus(bool isUra)
        {
            if (isUra)
            {
                return new PlayerStatus(hpMaxUra, attackPointDefaultUra, moveSpeedDefaultUra);
            }
            else
            {
                return new PlayerStatus(hpMax, attackPointDefault, moveSpeedDefault);
            }
        }

        public PieceType GetPieceType()
        {
            if (pieceTypeIndex < 0 || pieceTypeIndex >= System.Enum.GetValues(typeof(PieceType)).Length)
            {
                Debug.LogError("Invalid pieceTypeIndex: " + pieceTypeIndex);
                return PieceType.None;
            }
            return (PieceType)pieceTypeIndex;
        }
    }
}
