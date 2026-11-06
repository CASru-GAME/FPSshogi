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
            if(shade)UiTheme.ScreenDim(root,.88f);
        }
        RectTransform Modal(string title,float height=540,float width=700)
        {
            Clear();var panel=UiTheme.Rect(root,"Menu content",(1920-width)*.5f,(1080-height)*.5f,width,height);
            UiTheme.Label(panel,"Menu title",title,44,28,width-88,72,42);
            UiTheme.Box(panel,"Title rule",44,109,width-88,1,UiTheme.Rule);return panel;
        }
        Button Header(string title,string backName="Back button",UnityEngine.Events.UnityAction back=null)
        {
            UiTheme.Label(root,"Menu title",title,80,49,920,70,42);
            UiTheme.Box(root,"Page rule",80,132,1760,1,UiTheme.Rule);
            var button=UiTheme.Button(root,backName,"戻る",1650,51,210,65,back??session.Back);session.Focus(button);return button;
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
            Clear(false);UiTheme.EdgeShade(root,"Title shade",0,0,1320,1080,UiSymbol.Kind.LeftShade,.94f);
            UiTheme.Heading(root,"Game title","FPS将棋",85,86,650,140,96);
            string[] names={"Start button","Help button","Settings button","Exit button"},labels={"対局","遊び方","設定","終了"};
            UnityEngine.Events.UnityAction[] actions={session.StartMatch,session.OpenHelp,session.OpenSettings,session.ConfirmQuit};var buttons=new Button[4];
            for(int i=0;i<4;i++){
                var button=UiTheme.Button(root,names[i],labels[i],84,285+i*100,350,84,actions[i]);
                var text=button.GetComponentInChildren<Text>();text.alignment=TextAnchor.MiddleLeft;text.fontSize=39;text.rectTransform.anchoredPosition=new Vector2(25,-4);buttons[i]=button;
            }
            UiTheme.VerticalNavigation(buttons);UiTheme.Key(root,"Title confirm","A / Enter",84,970,135,42);UiTheme.Label(root,"Menu controls","決定",235,970,300,42,26);session.Focus(buttons[0]);
        }
        public void Pause()
        {
            Clear(false);UiTheme.ScreenDim(root,.28f);UiTheme.EdgeShade(root,"Pause shade",0,0,1320,1080,UiSymbol.Kind.LeftShade,.92f);
            UiTheme.Label(root,"Menu title","一時停止",85,100,650,90,48);
            var a=UiTheme.Button(root,"Resume button","再開",84,285,510,80,session.Resume);
            var b=UiTheme.Button(root,"Settings button","設定",84,385,510,80,session.OpenSettings);
            var c=UiTheme.Button(root,"Help button","操作・遊び方",84,485,510,80,session.OpenHelp);
            var d=UiTheme.Button(root,"Return title button","タイトルへ戻る",84,585,510,80,session.ConfirmReturn);
            foreach(var button in new[]{a,b,c,d}){var text=button.GetComponentInChildren<Text>();text.alignment=TextAnchor.MiddleLeft;text.fontSize=34;}
            UiTheme.Label(root,"Menu owner",session.MenuOwnerLabel,100,965,650,50,24,UiTheme.Muted);
            UiTheme.VerticalNavigation(a,b,c,d);session.Focus(a);
        }
        public void Help(bool controls=false)
        {
            Clear();Header("遊び方");
            UiTheme.Button(root,"Basics tab","ルール",60,150,210,65,()=>session.SetHelpTab(false),!controls);
            UiTheme.Button(root,"Controls tab","操作",290,150,210,65,()=>session.SetHelpTab(true),controls);
            UiTheme.Button(root,"Abilities tab","駒の説明",520,150,270,65,session.OpenPieces);
            if(!controls){
                var photo=UiTheme.Rect(root,"Board reference",60,270,900,506).gameObject.AddComponent<RawImage>();photo.texture=Resources.Load<Texture2D>("UI/BoardReference");photo.raycastTarget=false;
                var panel=UiTheme.Rect(root,"Rules",1010,270,850,660);
                string[] titles={"1. 駒を動かす","2. 駒が重なるとFPSで対戦","3. 王を倒すと勝利"};
                string[] details={"自分の駒を選び、移動先を決めます。","倒した駒は持ち駒になり、空きマスへ打てます。","盤面とFPSを繰り返して対局します。"};
                for(int i=0;i<3;i++){UiTheme.Label(panel,"Rule "+i,titles[i],32,22+i*135,786,56,33);UiTheme.Label(panel,"Rule detail "+i,details[i],32,81+i*135,786,58,26,UiTheme.Muted);}
                UiTheme.Label(panel,"Rule exceptions","王手・詰み判定はなく、成りは自動です。\n相打ちは両方の駒を残し、攻めた側の手番が終わります。\n二歩・行き場のない駒は置けません。",32,446,786,178,25,UiTheme.Muted);
            }else for(int p=0;p<2;p++){
                bool assigned=session.Devices.Connected(p);bool pad=assigned?session.Devices.IsGamepad(p):session.Devices.HasParticipants||p==0;
                var sheet=UiTheme.Rect(root,"Controls sheet "+p,190+p*800,265,740,685);
                if(p==1)UiTheme.Box(sheet,"Controls separator",-30,24,1,620,UiTheme.Rule);
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
            Clear();var back=Header("設定");
            var panel=UiTheme.Rect(root,"Settings content",350,260,1220,650);
            var sliders=new Slider[2];
            for(int p=0;p<2;p++){
                int index=p;float y=p*150;
                UiTheme.Label(panel,"Sensitivity label "+p,$"{p+1}P 視点感度",0,y,370,80,30);
                var value=UiTheme.Label(panel,"Sensitivity value "+p,$"{session.Sensitivity(p):0.0}",1100,y,120,80,30,UiTheme.Ink,TextAnchor.MiddleRight);
                var row=UiTheme.Box(panel,"Sensitivity slider "+p,420,y,620,80,Color.clear,true).rectTransform;
                UiTheme.Box(row,"Track",0,38,620,4,UiTheme.Track);
                var slider=row.gameObject.AddComponent<Slider>();var handleArea=UiTheme.Rect(row,"Handle area",0,22,620,36);
                var handle=UiTheme.Box(handleArea,"Handle",0,0,14,0,Color.white,true).rectTransform;handle.pivot=new Vector2(.5f,.5f);
                slider.handleRect=handle;slider.targetGraphic=handle.GetComponent<Image>();slider.minValue=.5f;slider.maxValue=5;slider.value=session.Sensitivity(p);
                var colors=slider.colors;colors.normalColor=UiTheme.Muted;colors.highlightedColor=colors.selectedColor=UiTheme.Gold;slider.colors=colors;
                slider.onValueChanged.AddListener(v=>{session.SetSensitivity(index,v);value.text=$"{v:0.0}";});sliders[p]=slider;
                UiTheme.Box(panel,"Settings row rule "+p,0,y+105,1220,1,UiTheme.Rule);
            }
            UiTheme.Label(panel,"HP setting label","HPの数値表示",0,300,650,80,30);
            var hp=UiTheme.Button(panel,"HP number setting","",920,300,300,80,null);
            void SetLabel()=>hp.GetComponentInChildren<Text>().text=session.ShowHpNumbers?"オン":"オフ";SetLabel();
            hp.onClick.AddListener(()=>{session.ShowHpNumbers=!session.ShowHpNumbers;SetLabel();});
            UiTheme.Box(panel,"HP setting rule",0,405,1220,1,UiTheme.Rule);
            var reset=UiTheme.Button(panel,"Reset settings button","初期値に戻す",850,540,370,65,()=>{session.ResetSettings();Settings();});
            UiTheme.VerticalNavigation(sliders[0],sliders[1],hp,reset,back);session.Focus(sliders[0]);
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
            if(error==null){UiTheme.Shade(root,"Loading shade",1150,850,770,230,UiSymbol.Kind.BottomShade,.8f);UiTheme.Label(root,"Loading status","読み込み中",1498,964,344,57,30);}
            else{var p=Modal("読み込めませんでした",430,900);UiTheme.Label(p,"Load error",error,44,135,812,130,25);session.Focus(UiTheme.Button(p,"Load return button","タイトルへ",44,310,812,65,session.ReturnTitle,true));}
        }
    }
}
