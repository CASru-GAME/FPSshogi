using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using App.Main.GameMaster;
using App.Main.ShogiPointer;
using App.Main.ShogiThings;
using App.Main.ViewManager;

namespace App.UI
{
    public sealed class BoardHud
    {
        readonly RectTransform root, selected, cursor;
        readonly Image[,] cells = new Image[9,9];
        readonly Text turn, feedback, pieceInfo, controls;
        readonly RectTransform feedbackBacking;
        readonly Text[] emptyTrays=new Text[2];
        readonly Text[] trayTitles = new Text[2];
        readonly Button[,] heldButtons = new Button[2,7];
        readonly Text[,] heldLabels = new Text[2,7];
        readonly PieceType[] types = { PieceType.Fuhyo, PieceType.Kyosya, PieceType.Keima, PieceType.Gin, PieceType.Kin, PieceType.Kakugyo, PieceType.Hisya };
        readonly ShogiBoard board;
        readonly ShogiPointer pointer;
        readonly ViewPiece pieces;
        readonly ViewCapturedPieces captured;
        readonly GameSessionUI session;
        readonly Camera camera;
        readonly Vector3 gridCenter, gridX, gridY;
        int lastInvalidFeedback;
        public BoardHud(RectTransform parent, GameSessionUI owner)
        {
            session = owner;
            board = Object.FindFirstObjectByType<ShogiBoard>(); pointer = Object.FindFirstObjectByType<ShogiPointer>();
            pieces = Object.FindFirstObjectByType<ViewPiece>(); captured = Object.FindFirstObjectByType<ViewCapturedPieces>(); camera = Camera.main;
            // Fit the hand-placed piece markers to a regular grid, rather than using
            // the uneven spacing between the first two pieces for every square.
            Vector3 center=Vector3.zero, basisX=Vector3.zero, basisY=Vector3.zero;
            for(int x=0;x<9;x++) for(int y=0;y<9;y++)
            {
                var point=pieces.boardCellPositions[x,y];
                center+=point; basisX+=point*(x-4); basisY+=point*(y-4);
            }
            gridCenter=center/81f; basisX/=540f; basisY/=540f;
            float cellSize=(basisX.magnitude+basisY.magnitude)*.5f;
            gridX=basisX.normalized*cellSize; gridY=basisY.normalized*cellSize;
            root = UiTheme.Rect(parent,"Board HUD",0,0,1920,1080);
            for(int x=0;x<9;x++) for(int y=0;y<9;y++)
            {
                int cx=x, cy=y;
                var img=UiTheme.Box(root,$"Square {x},{y}",0,0,70,70,Color.clear,true);
                var btn=img.gameObject.AddComponent<Button>(); btn.targetGraphic=img; btn.transition=Selectable.Transition.None;
                btn.navigation=new Navigation{mode=Navigation.Mode.None};
                btn.onClick.AddListener(()=> { if(GameSessionUI.BlocksGameplay) return; pointer.SetCursor(cx,cy); pointer.Select(); });
                cells[x,y]=img;
            }
            selected=UiTheme.Frame(root,"Selected tile",UiTheme.Gold);
            cursor=UiTheme.Frame(root,"Cursor tile",UiTheme.Blue);
            UiTheme.Box(root,"Turn backing",40,30,390,75,UiTheme.Panel);
            turn=UiTheme.Label(root,"Turn label","",60,36,350,63,34);
            UiTheme.Box(root,"Selected piece backing",40,780,350,145,UiTheme.Panel);
            pieceInfo=UiTheme.Label(root,"Piece info","",60,795,310,115,28);
            for(int p=0;p<2;p++) BuildTray(p,p==0?40:1530);
            feedbackBacking=UiTheme.Box(root,"Feedback backing",500,925,920,60,UiTheme.Panel).rectTransform;
            feedback=UiTheme.Label(feedbackBacking,"Board feedback","",15,5,890,50,26,UiTheme.Ink,TextAnchor.MiddleCenter);
            UiTheme.Box(root,"Controls backing",40,1000,1490,58,UiTheme.Panel);
            controls=UiTheme.Label(root,"Board controls","",60,1006,1450,42,25);
            UiTheme.Button(root,"Board menu button","メニュー",1640,35,235,64,session.OpenPause);
            UiTheme.Button(root,"Board guide button","駒の説明",1550,1000,325,58,session.OpenPieces);
        }
        void BuildTray(int player,float x)
        {
            var panel=UiTheme.Box(root,$"P{player+1} captured tray",x,300,350,395,UiTheme.Panel).rectTransform;
            UiTheme.Box(panel,"Player stripe",0,0,5,395,UiTheme.PlayerColor(player));
            trayTitles[player]=UiTheme.Label(panel,$"P{player+1} tray heading","",22,18,306,46,28,UiTheme.PlayerColor(player));
            emptyTrays[player]=UiTheme.Label(panel,$"P{player+1} no captured pieces","なし",22,97,306,60,27,UiTheme.Muted);
            for(int i=0;i<types.Length;i++)
            {
                int index=i, p=player;
                var btn=UiTheme.Button(panel,$"P{player+1} captured {types[i]}","",22+(i%2)*158,95+(i/2)*70,148,58,()=>
                {
                    if(GameSessionUI.BlocksGameplay || (int)pointer.ActivePlayer != p) return;
                    pointer.ChooseCaptured(types[index]);
                });
                btn.navigation=new Navigation{mode=Navigation.Mode.None};
                heldButtons[player,i]=btn; heldLabels[player,i]=btn.GetComponentInChildren<Text>();
            }
        }
        Vector2 Center(int x,int y)
        {
            var screen=camera.WorldToScreenPoint(gridCenter+gridX*(x-4)+gridY*(y-4));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root,screen,null,out var point);
            return point;
        }
        void Position(RectTransform rt,Vector2 center,Vector2 size)
        {
            rt.anchoredPosition=center+new Vector2(-size.x/2,size.y/2);
            rt.localScale=new Vector3(size.x/70,size.y/70,1);
        }
        public void Tick(bool visible)
        {
            root.gameObject.SetActive(visible);
            captured.SetVisible(false);
            if(!visible) return;
            int activePlayer=(int)pointer.ActivePlayer;
            turn.text=$"{activePlayer+1}P の手番";turn.color=UiTheme.PlayerColor(activePlayer);
            feedback.text=pointer.Feedback;
            if(pointer.InvalidFeedbackVersion!=lastInvalidFeedback){lastInvalidFeedback=pointer.InvalidFeedbackVersion;UiFeedback.Invalid();}
            feedbackBacking.gameObject.SetActive(!string.IsNullOrWhiteSpace(feedback.text));
            controls.text=session.Devices.IsGamepad(activePlayer)?"十字キー / 左スティック　選択　　 A　決定　　 B　取消　　 X　持ち駒　　 START　メニュー":"WASD　選択　　 F　決定　　 C　取消　　 E　持ち駒　　 Esc　メニュー";
            var origin=pointer.SelectedPosition; var pos=pointer.CursorPosition;
            var piece=board.GetPieceAt(origin.x>=0?origin.x:pos.x,origin.y>=0?origin.y:pos.y);
            PieceType type=pointer.IsPlacingCaptured||pointer.IsCapturedMode?pointer.CapturedType:piece?.Type??PieceType.None;
            var target=board.GetPieceAt(pos.x,pos.y);
            string action=pointer.IsCapturedMode?"持ち駒を選ぶ":pointer.IsPlacingCaptured?"持ち駒を打つ":origin.x>=0&&target!=null&&target.Player!=pointer.ActivePlayer?"決闘する":origin.x>=0?"移動先を選ぶ":"駒を選ぶ";
            pieceInfo.text=type==PieceType.None?action:PiecePresentation.Name(type,piece!=null&&piece.IsPromoted&&!pointer.IsCapturedMode&&!pointer.IsPlacingCaptured)+"\n"+action;
            Vector2 size=new Vector2(Mathf.Abs(Center(1,0).x-Center(0,0).x),Mathf.Abs(Center(0,1).y-Center(0,0).y))*.92f;
            for(int x=0;x<9;x++) for(int y=0;y<9;y++)
            {
                Position(cells[x,y].rectTransform,Center(x,y),size);
                bool legal=pointer.IsPlacingCaptured?board.CanDrop(x,y,pointer.CapturedType,pointer.ActivePlayer):origin.x>=0&&board.CanMove(origin.x,origin.y,x,y,pointer.ActivePlayer);
                cells[x,y].color=legal?new Color(UiTheme.Blue.r,UiTheme.Blue.g,UiTheme.Blue.b,.38f):Color.clear;
                cells[x,y].raycastTarget=!GameSessionUI.BlocksGameplay;
            }
            selected.gameObject.SetActive(origin.x>=0); if(origin.x>=0) Position(selected,Center(origin.x,origin.y),size);
            cursor.gameObject.SetActive(!pointer.IsCapturedMode); Position(cursor,Center(pos.x,pos.y),size);
            for(int p=0;p<2;p++)
            {
                bool active=(int)pointer.ActivePlayer==p;
                trayTitles[p].text=$"{p+1}P 持ち駒"+(active&&pointer.IsCapturedMode?"・選択中":"");
                trayTitles[p].color=UiTheme.PlayerColor(p);
                Dictionary<PieceType,int> counts=board.GetCapturedPieces((PlayerType)p);
                int shown=0;
                for(int i=0;i<7;i++)
                {
                    counts.TryGetValue(types[i],out int count);
                    heldButtons[p,i].gameObject.SetActive(count>0);if(count<=0)continue;
                    heldButtons[p,i].GetComponent<RectTransform>().anchoredPosition=new Vector2(22+(shown%2)*158,-95-(shown/2)*70);shown++;
                    heldLabels[p,i].text=$"{PiecePresentation.Name(types[i])} ×{count}";
                    bool chosen=active&&(pointer.IsCapturedMode||pointer.IsPlacingCaptured)&&pointer.CapturedType==types[i];
                    var colors=heldButtons[p,i].colors;colors.normalColor=chosen?UiTheme.FocusColor:UiTheme.Paper;colors.disabledColor=UiTheme.Panel;heldButtons[p,i].colors=colors;
                    heldLabels[p,i].color=active?UiTheme.Ink:UiTheme.Muted;
                    heldButtons[p,i].interactable=active&&count>0&&!GameSessionUI.BlocksGameplay;
                }
                emptyTrays[p].gameObject.SetActive(shown==0);
            }
        }
    }
}
