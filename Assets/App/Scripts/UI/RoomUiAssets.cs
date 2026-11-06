using UnityEngine;
using App.Main.ShogiThings;

namespace App.UI
{
    public sealed class RoomUiAssets : ScriptableObject
    {
        public GameObject pawn,lance,knight,silver,gold,bishop,rook,king;
        public static RoomUiAssets Load()=>Resources.Load<RoomUiAssets>("UI/RoomUiAssets");
        public GameObject Prefab(PieceType type)=>type switch {
            PieceType.Fuhyo=>pawn,PieceType.Kyosya=>lance,PieceType.Keima=>knight,PieceType.Gin=>silver,
            PieceType.Kin=>gold,PieceType.Kakugyo=>bishop,PieceType.Hisya=>rook,PieceType.King=>king,_=>null
        };
    }
}
