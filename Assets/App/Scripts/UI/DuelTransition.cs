using App.Main.ShogiThings;
using UnityEngine;
using UnityEngine.UI;

namespace App.UI
{
    public sealed class DuelTransition
    {
        readonly RectTransform root;
        readonly RoomPresentation presentation;
        readonly LocalMatchDevices devices;
        Text counter,nextTurn;
        readonly PieceType[] types=new PieceType[2];
        public DuelTransition(RectTransform parent,RoomPresentation presentation,LocalMatchDevices devices)
        {
            this.presentation=presentation;this.devices=devices;root=UiTheme.Rect(parent,"Transition",0,0,1920,1080);root.gameObject.SetActive(false);
        }
        void Clear(){foreach(Transform child in root){child.gameObject.SetActive(false);Object.Destroy(child.gameObject);}root.gameObject.SetActive(true);counter=null;nextTurn=null;}
        public void Duel(IPiece first,IPiece second)
        {
            Clear();UiTheme.ScreenDim(root,.68f);var pieces=new[]{first,second};
            for(int p=0;p<2;p++){
                var piece=pieces[p];types[p]=piece.Type;
                var panel=UiTheme.Rect(root,"Duel player "+p,130+p*960,330,700,380);
                UiTheme.Box(panel,"Player stripe",35,85,630,2,UiTheme.PlayerColor(p));
                UiTheme.Label(panel,"Player",$"{p+1}P",35,24,630,50,33,UiTheme.PlayerColor(p));
                UiTheme.Portrait(panel,"Duel piece",presentation.Portrait(piece.Type,piece.IsPromoted),35,110,200);
                UiTheme.Label(panel,"Duel piece name",PiecePresentation.Name(piece.Type,piece.IsPromoted),260,100,405,65,43);
                UiTheme.Symbol(panel,"Duel weapon",PiecePresentation.WeaponSymbol(piece.Type),260,187,40);
                UiTheme.Label(panel,"Duel weapon name",PiecePresentation.Weapon(piece.Type),322,185,340,46,28);
                UiTheme.Symbol(panel,"Duel skill",PiecePresentation.SkillSymbol(piece.Type),260,249,40);
                UiTheme.Label(panel,"Duel skill name",PiecePresentation.Skill(piece.Type),322,245,340,46,27);
                UiTheme.Label(panel,"Duel controls",devices.IsGamepad(p)?"RT 攻撃　LT 防御・ズーム　B スキル":"左 攻撃　右 防御・ズーム　E スキル",35,320,640,40,24,UiTheme.Muted);
            }
            var center=UiTheme.Rect(root,"Countdown backing",885,445,150,150);
            counter=UiTheme.Label(center,"Countdown","3",0,0,150,150,70,UiTheme.Ink,TextAnchor.MiddleCenter);
        }
        public void Result(int winner)
        {
            Clear();UiTheme.ScreenDim(root,.68f);var panel=UiTheme.Rect(root,"Duel result",540,350,840,380);
            UiTheme.Label(panel,"Transition title",winner<0?"相打ち":$"{winner+1}P 決闘勝利",35,30,770,76,46,UiTheme.Ink,TextAnchor.MiddleCenter);
            if(winner>=0){UiTheme.Portrait(panel,"Captured piece",presentation.Portrait(types[1-winner]),75,135,125);}
            UiTheme.Label(panel,"Transition detail",winner<0?"両方の駒が残ります。":PiecePresentation.Name(types[1-winner])+"が持ち駒に加わりました。",winner<0?35:225,150,winner<0?770:560,110,30,UiTheme.Muted,TextAnchor.MiddleCenter);
            nextTurn=UiTheme.Label(panel,"Next turn","",35,293,770,52,27,UiTheme.Muted,TextAnchor.MiddleCenter);
        }
        public void NextTurn(int player){if(nextTurn!=null)nextTurn.text=$"次は{player}Pの手番";}
        public void Tick(float remaining,bool visible){root.gameObject.SetActive(visible&&remaining>0);if(counter!=null)counter.text=Mathf.CeilToInt(remaining).ToString();}
    }
}
