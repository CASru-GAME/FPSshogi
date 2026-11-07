using System.Collections.Generic;
using App.Audio;
using App.PlayData;
using App.Main.Player;
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
        string lastExport;
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
                string[] titles={"1. 駒を動かす","2. 相手の駒を取りに行く","3. 決闘に勝つ"};
                string[] details={"自分の駒を動かすか、持ち駒を空きマスに打ちます。","互いの駒がいたマスから、向かい合って決闘します。","倒した駒は持ち駒になります。王を倒せば対局の勝ちです。"};
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
            Clear();
            var back = Header("設定");
            var panel = UiTheme.Rect(root, "Settings content", 350, 180, 1220, 760);
            var controls = new List<Selectable>();
            float y = 0;

            void AddSlider(string name, string label, float initial, float min, float max, bool sensitivity, System.Action<float> apply)
            {
                string Format(float v) => sensitivity ? $"{v:0.0}" : $"{v * 100:0}%";
                UiTheme.Label(panel, name + " label", label, 0, y, 420, 70, 28);
                var value = UiTheme.Label(panel, name + " value", Format(initial), 1100, y, 120, 70, 28, UiTheme.Ink, TextAnchor.MiddleRight);
                var row = UiTheme.Box(panel, name, 450, y, 590, 70, Color.clear, true).rectTransform;
                UiTheme.Box(row, "Track", 0, 33, 590, 4, UiTheme.Track);
                var area = UiTheme.Rect(row, "Handle area", 0, 17, 590, 36);
                var handle = UiTheme.Box(area, "Handle", 0, 0, 14, 0, Color.white, true);
                handle.rectTransform.pivot = new Vector2(.5f, .5f);
                var slider = row.gameObject.AddComponent<Slider>();
                slider.handleRect = handle.rectTransform;
                slider.targetGraphic = handle;
                // A whole-number slider moves one unit per D-pad press. Use tenths for sensitivity.
                float scale = sensitivity ? 10f : 1f;
                slider.wholeNumbers = sensitivity;
                slider.minValue = min * scale;
                slider.maxValue = max * scale;
                slider.SetValueWithoutNotify(initial * scale);
                row.gameObject.AddComponent<UiFocusSound>();
                var colors = slider.colors;
                colors.normalColor = UiTheme.Muted;
                colors.highlightedColor = colors.selectedColor = UiTheme.Gold;
                slider.colors = colors;
                slider.onValueChanged.AddListener(raw => {
                    float selected = raw / scale;
                    apply(selected);
                    value.text = Format(selected);
                    if (sensitivity) GameAudio.Play(SoundCue.UiNavigate);
                });
                controls.Add(slider);
                UiTheme.Box(panel, name + " rule", 0, y + 72, 1220, 1, UiTheme.Rule);
                y += 80;
            }

            for (int i = 0; i < 2; i++)
            {
                int slot = i;
                AddSlider("Sensitivity slider " + slot, $"{slot+1}P 視点感度", session.Sensitivity(slot),
                    Player.MinLookSensitivity, Player.MaxLookSensitivity, true, v => session.SetSensitivity(slot, v));
                AddSlider("Zoom sensitivity slider " + slot, $"{slot+1}P ズーム感度（パッド）", session.ZoomSensitivity(slot),
                    Player.MinZoomSensitivity, Player.MaxLookSensitivity, true, v => session.SetZoomSensitivity(slot, v));
            }
            y += 14;
            void AddVolume(string label, AudioBus bus) => AddSlider("Audio slider " + bus, label, GameAudio.GetVolume(bus), 0, 1, false,
                v => { GameAudio.SetVolume(bus, v); GameAudio.PreviewVolume(bus); });
            AddVolume("全体音量", AudioBus.Master);
            AddVolume("効果音", AudioBus.Effects);
            AddVolume("操作音", AudioBus.Interface);

            UiTheme.Label(panel,"HP setting label","HPの数値表示",0,y,650,70,28);
            var hp=UiTheme.Button(panel,"HP number setting","",920,y,300,70,null);
            void SetLabel()=>hp.GetComponentInChildren<Text>().text=session.ShowHpNumbers?"オン":"オフ";SetLabel();
            hp.onClick.AddListener(()=>{session.ShowHpNumbers=!session.ShowHpNumbers;SetLabel();});
            UiTheme.Box(panel,"HP setting rule",0,y + 72,1220,1,UiTheme.Rule);
            var reset=UiTheme.Button(panel,"Reset settings button","初期値に戻す",850,y + 100,370,65,()=>{session.ResetSettings();Settings();});
            var feedback = UiTheme.Label(panel, "Play data status", "", 0, y + 171, 1220, 45, 23, UiTheme.Muted);
            var export = UiTheme.Button(panel, "Export play data", "プレイ記録をCSV出力", 0, y + 100, 428, 65, null);
            export.onClick.AddListener(() => ExportRecords(export, feedback));
            var folder = UiTheme.Button(panel, "Open play data folder", "出力先を開く", 448, y + 100, 316, 65, () => OpenRecords(feedback));
            controls.Add(hp);
            controls.Add(export);
            controls.Add(folder);
            controls.Add(reset);
            controls.Add(back);
            UiTheme.VerticalNavigation(controls.ToArray());
            session.Focus(controls[0]);
        }
        async void ExportRecords(Button button, Text feedback)
        {
            button.interactable = false;
            feedback.text = "書き出し中…";
            try
            {
                var result = await PlayDataFiles.ExportAsync();
                if (result.directory != null) lastExport = result.directory;
                if (feedback != null)
                {
                    feedback.text = result.matches == 0 ? "書き出せる対局はありません。" :
                        $"{result.matches}局・{result.duels}決闘を書き出しました。";
                    if (result.skipped > 0) feedback.text += $"　読み込めない記録 {result.skipped}件";
                }
            }
            catch (System.Exception ex)
            {
                if (feedback != null) feedback.text = "書き出せませんでした。保存先の空き容量と書き込み権限を確認してください。";
                Debug.LogWarning("プレイ記録のCSV出力に失敗しました。 " + ex.Message);
            }
            finally { if (button != null) button.interactable = true; }
        }

        void OpenRecords(Text feedback)
        {
            try
            {
                string path = System.IO.Directory.Exists(lastExport) ? lastExport : PlayDataFiles.ExportsDirectory;
                System.IO.Directory.CreateDirectory(path);
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = path, UseShellExecute = true })?.Dispose();
#else
                Application.OpenURL(new System.Uri(path + System.IO.Path.DirectorySeparatorChar).AbsoluteUri);
#endif
            }
            catch (System.Exception ex)
            {
                feedback.text = "出力先を開けませんでした。";
                Debug.LogWarning("プレイ記録の出力先を開けませんでした。 " + ex.Message);
            }
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
