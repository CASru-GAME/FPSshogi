using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using App.Main.ShogiThings;

namespace App.UI
{
    public sealed class MenuPanels
    {
        readonly RectTransform root;
        readonly GameSessionUI session;
        PlayerSelectionPanel selection;
        readonly Text[] connectionStates=new Text[2];
        Text connection;
        PieceGuidePanel guide;
        public bool Visible=>root.gameObject.activeSelf;
        public MenuPanels(RectTransform parent,GameSessionUI session){this.session=session;root=UiTheme.Rect(parent,"Menus",0,0,1920,1080);}
        void Clear(bool shade=true)
        {
            foreach(Transform child in root){child.gameObject.SetActive(false);Object.Destroy(child.gameObject);}
            root.gameObject.SetActive(true);selection=null;connection=null;connectionStates[0]=connectionStates[1]=null;
            if(shade)UiTheme.Box(root,"Menu shade",0,0,1920,1080,new Color(.08f,.1f,.06f,.22f),true);
        }
        RectTransform Modal(string title,float height=540,float width=700)
        {
            Clear();var panel=UiTheme.Box(root,"Menu panel",(1920-width)*.5f,(1080-height)*.5f,width,height,UiTheme.Panel,true).rectTransform;
            UiTheme.Label(panel,"Menu title",title,44,28,width-88,72,42);return panel;
        }
        void Header(string title,string backName="Back button",UnityEngine.Events.UnityAction back=null)
        {
            var label=UiTheme.Box(root,"Page title backing",60,45,450,78,UiTheme.Panel).rectTransform;
            UiTheme.Label(label,"Menu title",title,20,4,410,70,42);
            session.Focus(UiTheme.Button(root,backName,"戻る",1650,51,210,65,back??session.Back));
        }
        void Owner(RectTransform panel,float y,float width=612){UiTheme.Label(panel,"Menu owner",session.MenuOwnerLabel,0,y,width,40,23,UiTheme.Muted,TextAnchor.MiddleCenter);}
        public void Hide(){root.gameObject.SetActive(false);selection=null;connection=null;}
        public void FocusNamed(string name)
        {
            if(string.IsNullOrEmpty(name))return;
            foreach(var selectable in root.GetComponentsInChildren<Selectable>())if(selectable.name==name&&selectable.interactable){session.Focus(selectable);return;}
        }
        public void Title()
        {
            Clear(false);var shade=UiTheme.Rect(root,"Title shade",0,0,1050,1080).gameObject.AddComponent<UiSymbol>();shade.Shape=UiSymbol.Kind.LeftShade;shade.color=new Color(UiTheme.Panel.r,UiTheme.Panel.g,UiTheme.Panel.b,.86f);shade.raycastTarget=false;
            UiTheme.Heading(root,"Game title","FPS将棋",85,86,650,140,96);
            string[] names={"Start button","Help button","Settings button","Exit button"},labels={"対局","遊び方","設定","終了"};
            UnityEngine.Events.UnityAction[] actions={session.StartMatch,session.OpenHelp,session.OpenSettings,session.ConfirmQuit};var buttons=new Button[4];
            for(int i=0;i<4;i++){
                var button=UiTheme.Button(root,names[i],labels[i],84,285+i*100,350,84,actions[i]);var colors=button.colors;colors.normalColor=Color.clear;colors.highlightedColor=UiTheme.Panel;colors.selectedColor=UiTheme.Panel;button.colors=colors;
                var text=button.GetComponentInChildren<Text>();text.alignment=TextAnchor.MiddleLeft;text.fontSize=39;text.rectTransform.anchoredPosition=new Vector2(25,-4);buttons[i]=button;
            }
            UiTheme.VerticalNavigation(buttons);UiTheme.Key(root,"Title confirm","A / Enter",84,970,135,42);UiTheme.Label(root,"Menu controls","決定",235,970,300,42,26);session.Focus(buttons[0]);
        }
        public void Pause()
        {
            var panel=Modal("一時停止",625,700);
            var a=UiTheme.Button(panel,"Resume button","再開",44,135,612,75,session.Resume,true);
            var b=UiTheme.Button(panel,"Settings button","設定",44,229,612,72,session.OpenSettings);
            var c=UiTheme.Button(panel,"Help button","操作・遊び方",44,320,612,72,session.OpenHelp);
            var d=UiTheme.Button(panel,"Return title button","タイトルへ戻る",44,411,612,72,session.ConfirmReturn);
            Owner(panel,531,700);UiTheme.VerticalNavigation(a,b,c,d);session.Focus(a);
        }
        public void Help(bool controls=false)
        {
            Clear();Header("遊び方");
            UiTheme.Button(root,"Basics tab","ルール",60,150,210,65,()=>session.SetHelpTab(false),!controls);
            UiTheme.Button(root,"Controls tab","操作",290,150,210,65,()=>session.SetHelpTab(true),controls);
            UiTheme.Button(root,"Abilities tab","駒の説明",520,150,270,65,session.OpenPieces);
            if(!controls){
                var photo=UiTheme.Rect(root,"Board reference",60,270,900,506).gameObject.AddComponent<RawImage>();photo.texture=Resources.Load<Texture2D>("UI/BoardReference");photo.raycastTarget=false;
                var panel=UiTheme.Box(root,"Rules",1010,270,850,660,UiTheme.Panel).rectTransform;
                string[] titles={"1. 駒を動かす","2. 駒が重なるとFPSで対戦","3. 王を倒すと勝利"};
                string[] details={"自分の駒を選び、移動先を決めます。","倒した駒は持ち駒になり、空きマスへ打てます。","盤面とFPSを繰り返して対局します。"};
                for(int i=0;i<3;i++){UiTheme.Label(panel,"Rule "+i,titles[i],32,22+i*135,786,56,33);UiTheme.Label(panel,"Rule detail "+i,details[i],32,81+i*135,786,58,26,UiTheme.Muted);}
                UiTheme.Label(panel,"Rule exceptions","王手・詰み判定はなく、成りは自動です。\n相打ちは両方の駒を残し、攻めた側の手番が終わります。\n二歩・行き場のない駒は置けません。",32,446,786,178,25,UiTheme.Muted);
            }else for(int p=0;p<2;p++){
                bool assigned=session.Devices.Connected(p);bool pad=assigned?session.Devices.IsGamepad(p):session.Devices.HasParticipants||p==0;
                var sheet=UiTheme.Box(root,"Controls sheet "+p,190+p*800,265,740,685,UiTheme.Panel).rectTransform;
                UiTheme.Symbol(sheet,"Device",pad?UiSymbol.Kind.Gamepad:UiSymbol.Kind.Keyboard,26,24,60);
                UiTheme.Label(sheet,"Player controls title",(assigned?$"{p+1}P　":"")+(pad?"コントローラー":"キーボード＋マウス"),105,25,610,70,30);
                UiTheme.Label(sheet,"Board controls heading","盤面",30,125,680,50,30);
                UiTheme.Label(sheet,"Board controls",pad?"十字キー / 左スティック　選択\nA　決定　　B　取消　　X　持ち駒":"WASD　選択\nF　決定　　C　取消　　E　持ち駒",30,185,680,110,27);
                UiTheme.Label(sheet,"FPS controls heading","FPS",30,315,680,50,30);
                UiTheme.Label(sheet,"FPS controls",pad?"左スティック　移動 / 右スティック　視点\nRT　攻撃　　LT　防御・ズーム\nB　スキル　　A　登る\nSTART　メニュー":"WASD　移動 / マウス　視点\n左クリック　攻撃 / 右クリック　防御・ズーム\nE　スキル　　Space　登る\nEsc　メニュー",30,375,680,224,26);
                if(pad)UiTheme.Label(sheet,"Button convention","Xbox表記",30,620,680,36,23,UiTheme.Muted);
            }
        }
        public void Pieces()
        {
            Clear();Header("駒の説明");guide=new PieceGuidePanel(root,session,session.GuidePiece,session.GuidePromoted);
        }
        public void Settings()
        {
            var panel=Modal("設定",650,790);
            for(int p=0;p<2;p++){
                int index=p;float y=135+p*150;var value=UiTheme.Label(panel,"Sensitivity value "+p,$"{p+1}P 視点感度　{session.Sensitivity(p):0.0}",44,y,702,48,29);
                var track=UiTheme.Box(panel,"Sensitivity slider "+p,54,y+70,680,16,UiTheme.Track,true).rectTransform;
                var slider=track.gameObject.AddComponent<Slider>();var handle=UiTheme.Box(track,"Handle",0,-12,28,40,UiTheme.Gold,true).rectTransform;
                slider.handleRect=handle;slider.targetGraphic=handle.GetComponent<Image>();slider.minValue=.5f;slider.maxValue=5;slider.value=session.Sensitivity(p);
                slider.onValueChanged.AddListener(v=>{session.SetSensitivity(index,v);value.text=$"{index+1}P 視点感度　{v:0.0}";});
            }
            var hp=UiTheme.Button(panel,"HP number setting","",44,435,702,65,null);
            void SetLabel()=>hp.GetComponentInChildren<Text>().text="HPの数値表示　"+(session.ShowHpNumbers?"オン":"オフ");SetLabel();
            hp.onClick.AddListener(()=>{session.ShowHpNumbers=!session.ShowHpNumbers;SetLabel();});
            var back=UiTheme.Button(panel,"Back button","戻る",44,540,310,65,session.Back,true);
            UiTheme.Button(panel,"Reset settings button","初期値に戻す",376,540,370,65,()=>{session.ResetSettings();Settings();});session.Focus(back);
        }
        public void Selection(){Clear();Header("プレイヤー選択","Selection back button",session.CancelSelection);selection=new PlayerSelectionPanel(root,session);}
        public void Connection()
        {
            var panel=Modal("接続確認",700,900);
            connection=UiTheme.Label(panel,"Connection message","",44,117,812,102,30);
            for(int p=0;p<2;p++){
                UiTheme.Box(panel,"Player stripe "+p,44,250+p*135,5,110,UiTheme.PlayerColor(p));
                connectionStates[p]=UiTheme.Label(panel,"P"+(p+1)+" reconnect state","",66,243+p*135,746,110,28);
            }
            var keyboard=UiTheme.Button(panel,"Reconnect keyboard button","キーマウ：再開OK",44,535,490,65,()=>session.ConfirmReconnect(Keyboard.current));
            keyboard.interactable=session.Devices.PlayerFor(Keyboard.current)>=0;
            UiTheme.Button(panel,"Connection back button","タイトルへ",556,535,300,65,session.ConfirmReturn);
            UiTheme.Label(panel,"Reconnect controls","A / Enter：再開OK　　B / Esc：取消",44,625,812,42,25,UiTheme.Muted);UpdateDevices();
        }
        public void UpdateDevices()
        {
            selection?.Tick();if(connection==null)return;
            connection.text=session.Devices.Ready?"接続を確認しました。\n両者の確認後に再開します。":"コントローラーの接続を確認してください。\n対局は一時停止しています。";
            for(int p=0;p<2;p++)if(connectionStates[p]!=null)connectionStates[p].text=$"{p+1}P　"+(!session.Devices.Connected(p)?"未接続":session.Devices.IsPrepared(p)?"再開OK":"接続済み　"+(session.Devices.IsGamepad(p)?"A":"Enter")+"で確認");
        }
        public void Result(int winner)
        {
            var panel=Modal($"{winner}P 勝利",610,720);UiTheme.Portrait(panel,"Winning king",session.Presentation.Portrait(PieceType.King),260,115,200);
            UiTheme.Label(panel,"Win reason","王を倒しました",44,320,632,55,29,UiTheme.Muted,TextAnchor.MiddleCenter);
            var a=UiTheme.Button(panel,"Rematch button","もう一局",44,410,632,70,session.StartMatch,true);var b=UiTheme.Button(panel,"Result title button","タイトルへ",44,503,632,65,session.ReturnTitle);UiTheme.VerticalNavigation(a,b);session.Focus(a);
        }
        public void ConfirmReturn(bool quit=false)
        {
            var panel=Modal(quit?"ゲームを終了しますか？":"対局を終了しますか？",380,900);
            if(!quit)UiTheme.Label(panel,"Return warning","この対局の途中経過は保存されません。",44,135,812,80,29);
            var cancel=UiTheme.Button(panel,"Cancel return button","キャンセル",44,260,390,70,session.Back,true);
            UiTheme.Button(panel,"Confirm return button",quit?"終了":"タイトルへ",466,260,390,70,quit?(UnityEngine.Events.UnityAction)session.Quit:session.ReturnTitle);session.Focus(cancel);
        }
        public void Loading(string error=null)
        {
            Clear(false);
            if(error==null){var p=UiTheme.Box(root,"Loading",1480,960,380,65,UiTheme.Panel).rectTransform;UiTheme.Label(p,"Loading status","読み込み中",18,4,344,57,30);}
            else{var p=Modal("読み込めませんでした",430,900);UiTheme.Label(p,"Load error",error,44,135,812,130,25);session.Focus(UiTheme.Button(p,"Load return button","タイトルへ",44,310,812,65,session.ReturnTitle,true));}
        }
    }
}
