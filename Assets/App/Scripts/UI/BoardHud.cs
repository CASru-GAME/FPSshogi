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
        readonly Text turn, feedback, pieceInfo;
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
            UiTheme.Box(root,"Turn backing",480,22,960,118,UiTheme.Ink);
            turn=UiTheme.Label(root,"Turn label","",500,30,920,55,38,UiTheme.Gold,TextAnchor.MiddleCenter);
            pieceInfo=UiTheme.Label(root,"Piece info","",500,85,920,42,24,UiTheme.Paper,TextAnchor.MiddleCenter);
            for(int p=0;p<2;p++) BuildTray(p,p==0?1455:45);
            UiTheme.Box(root,"Feedback backing",430,942,1060,48,UiTheme.Ink);
            feedback=UiTheme.Label(root,"Board feedback","",445,947,1030,38,24,UiTheme.Paper,TextAnchor.MiddleCenter);
            UiTheme.Box(root,"Controls backing",45,1000,1830,58,UiTheme.Ink);
            UiTheme.Label(root,"Board controls","移動 WASD / 十字キー　決定 F / A　取消 C / B　持ち駒 E / X　　Esc / START：メニュー",65,1006,1790,42,25,UiTheme.Paper,TextAnchor.MiddleCenter);
            UiTheme.Button(root,"Board menu button","メニュー",1640,35,235,64,session.OpenPause);
        }
        void BuildTray(int player,float x)
        {
            var panel=UiTheme.Box(root,$"P{player+1} captured tray",x,290,420,430,UiTheme.Panel).rectTransform;
            trayTitles[player]=UiTheme.Label(panel,$"P{player+1} tray heading","",22,18,376,46,29,UiTheme.Gold);
            UiTheme.Label(panel,"Tray hint","駒を選ぶと、打てるマスが光ります",22,69,376,36,21,UiTheme.Muted);
            for(int i=0;i<types.Length;i++)
            {
                int index=i, p=player;
                var btn=UiTheme.Button(panel,$"P{player+1} captured {types[i]}","",22+(i%2)*193,120+(i/2)*70,182,58,()=>
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
            turn.text=$"{(pointer.ActivePlayer==PlayerType.PlayerOne?"1P / 先手":"2P / 後手")} の手番"+(pointer.IsCapturedMode?"　｜　持ち駒を選択":pointer.IsPlacingCaptured?"　｜　持ち駒を打つ":"");
            feedback.text=pointer.Feedback;
            var origin=pointer.SelectedPosition; var pos=pointer.CursorPosition;
            var piece=board.GetPieceAt(origin.x>=0?origin.x:pos.x,origin.y>=0?origin.y:pos.y);
            PieceType type=pointer.IsPlacingCaptured||pointer.IsCapturedMode?pointer.CapturedType:piece?.Type??PieceType.None;
            pieceInfo.text=type==PieceType.None?"自分の駒を選んでください":$"{PiecePresentation.Name(type)}　｜　{PiecePresentation.Weapon(type)}　｜　{PiecePresentation.Skill(type)}";
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
                trayTitles[p].text=$"{p+1}P の持ち駒"+(active&&pointer.IsCapturedMode?"　◀ 選択中":"");
                trayTitles[p].color=active?UiTheme.Gold:UiTheme.Muted;
                Dictionary<PieceType,int> counts=board.GetCapturedPieces((PlayerType)p);
                for(int i=0;i<7;i++)
                {
                    counts.TryGetValue(types[i],out int count);
                    heldLabels[p,i].text=$"{PiecePresentation.Name(types[i])} ×{count}";
                    bool chosen=active&&(pointer.IsCapturedMode||pointer.IsPlacingCaptured)&&pointer.CapturedType==types[i];
                    heldButtons[p,i].image.color=chosen?UiTheme.Gold:UiTheme.Ink;
                    heldLabels[p,i].color=chosen?UiTheme.Ink:count>0?UiTheme.Paper:UiTheme.Muted;
                    heldButtons[p,i].interactable=active&&count>0&&!GameSessionUI.BlocksGameplay;
                }
            }
        }
    }
}
