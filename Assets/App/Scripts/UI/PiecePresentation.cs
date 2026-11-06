using App.Main.ShogiThings;

namespace App.UI
{
    public static class PiecePresentation
    {
        public static string Name(PieceType type) => type switch
        {
            PieceType.King => "王", PieceType.Hisya => "飛車", PieceType.Kakugyo => "角",
            PieceType.Kin => "金", PieceType.Gin => "銀", PieceType.Keima => "桂馬",
            PieceType.Kyosya => "香車", PieceType.Fuhyo => "歩", _ => "—"
        };
        public static string Weapon(PieceType type) => type switch
        {
            PieceType.Kyosya => "短銃", PieceType.Kakugyo => "長銃", PieceType.Hisya => "弓", _ => "近接武器"
        };
        public static string Skill(PieceType type) => type switch
        {
            PieceType.Fuhyo => "跳び込み", PieceType.Kyosya => "閃光弾", PieceType.Keima => "突進攻撃",
            PieceType.Gin => "無敵防御", PieceType.Kin => "防御強化", PieceType.Kakugyo => "上昇・滞空",
            PieceType.Hisya => "減速フィールド", PieceType.King => "援護砲台", _ => "スキル"
        };
    }
}
