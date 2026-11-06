using System.Linq;
using App.Main.ShogiThings;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace App.UI
{
    public sealed class PieceGuidePanel
    {
        readonly RectTransform root;
        readonly GameSessionUI session;
        readonly PieceType[] types={PieceType.Fuhyo,PieceType.Kyosya,PieceType.Keima,PieceType.Gin,PieceType.Kin,PieceType.Kakugyo,PieceType.Hisya,PieceType.King};
        RectTransform detail;
        readonly Button[] choices=new Button[8];
        public PieceGuidePanel(RectTransform parent,GameSessionUI session,PieceType type,bool promoted)
        {
            root=UiTheme.Rect(parent,"Piece guide",60,180,1800,820);this.session=session;
            for(int i=0;i<types.Length;i++){
                var t=types[i];choices[i]=UiTheme.Button(root,"Guide "+t,PiecePresentation.Name(t),0,i*88,250,72,()=>Show(t,false));
            }
            Show(type,promoted);
        }
        void Show(PieceType type,bool promoted)
        {
            string focus=EventSystem.current?.currentSelectedGameObject?.name;
            session.GuidePiece=type;session.GuidePromoted=promoted&&PiecePresentation.CanPromote(type);promoted=session.GuidePromoted;
            if(detail!=null){detail.gameObject.SetActive(false);Object.Destroy(detail.gameObject);}
            detail=UiTheme.Box(root,"Piece details",290,0,1510,760,UiTheme.Panel).rectTransform;
            UiTheme.Portrait(detail,"Guide portrait",session.Presentation.Portrait(type,promoted),60,65,230);
            UiTheme.Label(detail,"Guide piece name",PiecePresentation.Name(type,promoted),60,325,260,68,46,UiTheme.Ink,TextAnchor.MiddleCenter);
            if(PiecePresentation.CanPromote(type)){
                var baseButton=UiTheme.Button(detail,"Guide base","成る前",48,440,140,64,()=>Show(type,false),!promoted);
                var promotedButton=UiTheme.Button(detail,"Guide promoted","成った後",205,440,160,64,()=>Show(type,true),promoted);
                if(focus=="Guide base")session.Focus(baseButton);else if(focus=="Guide promoted")session.Focus(promotedButton);
            }
            var piece=PiecePresentation.Create(type);var moves=promoted?piece.PromotedMovement:piece.Movement;
            UiTheme.Label(detail,"Move heading","盤面での動き",450,35,550,60,33);
            for(int y=0;y<5;y++)for(int x=0;x<5;x++){
                // Piece coordinates increase toward the opponent; the guide faces up.
                bool legal=moves.Any(m=>m[0]==x-2&&m[1]==2-y);
                var cell=UiTheme.Box(detail,$"Move {x},{y}",450+x*61,115+y*61,58,58,legal?UiTheme.FocusColor:UiTheme.Paper);
                if(legal)UiTheme.Box(cell.transform,"Reachable",24,24,10,10,UiTheme.Gold);
                if(x==2&&y==2)UiTheme.Label(cell.transform,"Origin","駒",0,0,58,58,27,UiTheme.Ink,TextAnchor.MiddleCenter);
            }
            UiTheme.Label(detail,"Move direction","相手側 ↑",450,430,305,46,23,UiTheme.Muted,TextAnchor.MiddleCenter);
            UiTheme.Label(detail,"Move description",PiecePresentation.Movement(type,promoted),440,500,440,136,27);
            UiTheme.Label(detail,"Combat heading","FPSでの操作",965,35,490,60,33);
            UiTheme.Symbol(detail,"Weapon icon",PiecePresentation.WeaponSymbol(type),965,145,65);UiTheme.Label(detail,"Weapon name",PiecePresentation.Weapon(type),1050,147,420,60,30);
            bool zoom=type==PieceType.Hisya||type==PieceType.Kakugyo||type==PieceType.Kyosya;
            UiTheme.Symbol(detail,"Secondary icon",zoom?UiSymbol.Kind.Zoom:UiSymbol.Kind.Shield,965,275,65);UiTheme.Label(detail,"Secondary name",zoom?"ズーム":"防御",1050,277,420,60,30);
            UiTheme.Symbol(detail,"Skill icon",PiecePresentation.SkillSymbol(type),965,405,65);UiTheme.Label(detail,"Skill name",PiecePresentation.Skill(type),1050,407,420,60,30);
            UiTheme.Label(detail,"Cooldown explanation","アイコンの円が消えると\n再び使えます。",965,535,490,110,27,UiTheme.Muted);
            UiTheme.Label(detail,"Promotion note","成れる駒は、敵陣に入る・敵陣から動くと自動で成ります。",48,675,1410,50,25,UiTheme.Muted);
            for(int i=0;i<choices.Length;i++){var colors=choices[i].colors;colors.normalColor=types[i]==type?UiTheme.FocusColor:UiTheme.Panel;choices[i].colors=colors;}
        }
    }
}
