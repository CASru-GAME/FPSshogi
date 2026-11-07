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
            PieceType.Fuhyo => "槍", PieceType.Keima => "刀", PieceType.Gin or PieceType.Kin => "薙刀",
            PieceType.King => "短刀", PieceType.Kyosya => "短銃", PieceType.Kakugyo => "長銃", PieceType.Hisya => "弓", _ => "—"
        };
        public static string Skill(PieceType type) => type switch
        {
            PieceType.Fuhyo => "跳び込み", PieceType.Kyosya => "閃光弾", PieceType.Keima => "突進攻撃",
            PieceType.Gin => "無敵防御", PieceType.Kin => "防御強化", PieceType.Kakugyo => "上昇・滞空",
            PieceType.Hisya => "減速フィールド", PieceType.King => "援護砲台", _ => "スキル"
        };
        public static bool CanPromote(PieceType type) => type!=PieceType.King && type!=PieceType.Kin && type!=PieceType.None;
        public static string SkillDescription(PieceType type) => type switch {
            PieceType.Fuhyo => "向いている方向へ跳び込む。",
            PieceType.Kyosya => "閃光弾を投げる。爆発範囲にいる駒は視界が白くなり、移動が遅くなる。",
            PieceType.Keima => "前方へ12m突進し、進路上と終点の周囲にいる相手を一度だけ攻撃する。",
            PieceType.Gin => "1.5秒間、受けるダメージを無効にする。",
            PieceType.Kin => "3.5秒間、受けるダメージを1回につき16減らす。",
            PieceType.Kakugyo => "約8m上昇し、2.4秒間空中にとどまる。",
            PieceType.Hisya => "照準の先に4秒間、駒の移動を遅くするフィールドを置く。",
            PieceType.King => "自動で射撃する砲台を3基設置する。砲台は3.5秒間作動する。",
            _ => ""
        };
        public static string Name(PieceType type,bool promoted) => !promoted?Name(type):type switch {
            PieceType.Fuhyo=>"と金",PieceType.Kyosya=>"成香",PieceType.Keima=>"成桂",PieceType.Gin=>"成銀",PieceType.Hisya=>"龍王",PieceType.Kakugyo=>"龍馬",_=>Name(type)
        };
        public static IPiece Create(PieceType type) => type switch {
            PieceType.King=>new King(PlayerType.PlayerOne),PieceType.Hisya=>new Hisya(PlayerType.PlayerOne),PieceType.Kakugyo=>new Kakugyo(PlayerType.PlayerOne),PieceType.Kin=>new Kin(PlayerType.PlayerOne),PieceType.Gin=>new Gin(PlayerType.PlayerOne),PieceType.Keima=>new Keima(PlayerType.PlayerOne),PieceType.Kyosya=>new Kyosya(PlayerType.PlayerOne),_=>new Fuhyo(PlayerType.PlayerOne)
        };
        public static string Movement(PieceType type,bool promoted) => promoted && type!=PieceType.Hisya && type!=PieceType.Kakugyo?"斜め後ろ以外の隣のマスへ。":type switch {
            PieceType.Fuhyo=>"前へ1マス。",PieceType.Kyosya=>"前へ何マスでも。駒は飛び越せません。",PieceType.Keima=>"前に2マス、左右に1マス。途中の駒を飛び越せます。",PieceType.Gin=>"前と斜めの隣のマスへ。",PieceType.Kin=>"斜め後ろ以外の隣のマスへ。",PieceType.Kakugyo=>promoted?"斜めに何マスでも。縦横にも1マス動けます。":"斜めに何マスでも。駒は飛び越せません。",PieceType.Hisya=>promoted?"縦横に何マスでも。斜めにも1マス動けます。":"縦横に何マスでも。駒は飛び越せません。",_=>"周囲の8方向に1マス。"
        };
        public static UiSymbol.Kind WeaponSymbol(PieceType type) => type==PieceType.Hisya?UiSymbol.Kind.Bow:type==PieceType.Kakugyo||type==PieceType.Kyosya?UiSymbol.Kind.Gun:UiSymbol.Kind.Sword;
        public static UiSymbol.Kind SkillSymbol(PieceType type) => type switch {
            PieceType.Kyosya=>UiSymbol.Kind.Flash,PieceType.Keima=>UiSymbol.Kind.Dash,PieceType.Gin or PieceType.Kin=>UiSymbol.Kind.Shield,PieceType.Hisya=>UiSymbol.Kind.Slow,PieceType.King=>UiSymbol.Kind.Turret,_=>UiSymbol.Kind.Jump
        };
    }
}
