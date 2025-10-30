using App.Main.ShogiThings;
using UnityEngine;
using System;

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

        public PlayerStatus CreatePlayerStatus(bool isUra, Player player)
        {
            if (isUra)
            {
                return new PlayerStatus(hpMaxUra, attackPointDefaultUra, moveSpeedDefaultUra, player);
            }
            else
            {
                return new PlayerStatus(hpMax, attackPointDefault, moveSpeedDefault, player);
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
