using App.Main.ShogiThings;
using UnityEngine;

namespace App.Main.Player
{
    public class StatusParameter
    {
        [SerializeField] public PieceType pieceType;
        [SerializeField] public int hpMax;
        [SerializeField] public int attackPointDefault;
        [SerializeField] public float moveSpeedDefault;
        [SerializeField] public int hpMaxUra;
        [SerializeField] public int attackPointDefaultUra;
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
    }
}
