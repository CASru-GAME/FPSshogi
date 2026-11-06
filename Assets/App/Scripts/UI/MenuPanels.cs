using UnityEngine;
using UnityEngine.UI;

namespace App.UI
{
    public sealed class MenuPanels
    {
        readonly RectTransform root;
        readonly GameSessionUI session;
        RectTransform panel;
        Text connection;
        Button readyButton;
        public bool Visible => root.gameObject.activeSelf;
        public MenuPanels(RectTransform parent,GameSessionUI session)
        { this.session=session; root=UiTheme.Rect(parent,"Menus",0,0,1920,1080); }
        void Clear(bool shade=true)
        {
            foreach(Transform child in root) { child.gameObject.SetActive(false); Object.Destroy(child.gameObject); }
            root.gameObject.SetActive(true); connection=null; readyButton=null;
            if(shade) UiTheme.Box(root,"Menu shade",0,0,1920,1080,new Color(0,0,0,.68f),true);
        }
        RectTransform Standard(string title)
        {
            Clear(); panel=UiTheme.Box(root,"Menu panel",420,125,1080,830,UiTheme.Ink,true).rectTransform;
            UiTheme.Label(panel,"Menu title",title,44,28,992,72,46,UiTheme.Gold);
            UiTheme.Box(panel,"Heading rule",44,108,992,2,UiTheme.Gold); return panel;
        }
        void Focus(Button button) => session.Focus(button);
        public void Hide() { root.gameObject.SetActive(false); connection=null; readyButton=null; }
        public void Title()
        {
            Clear(false);
            UiTheme.Box(root,"Title shade",0,0,1920,1080,new Color(.08f,.05f,.03f,.38f));
            panel=UiTheme.Box(root,"Title panel",600,120,720,840,UiTheme.Ink,true).rectTransform;
            UiTheme.Label(panel,"Game title","FPS 将棋",40,16,640,140,82,UiTheme.Gold,TextAnchor.MiddleCenter);
            UiTheme.Label(panel,"Tagline","駒を取る、その瞬間が決闘。",40,158,640,50,30,UiTheme.Paper,TextAnchor.MiddleCenter);
            UiTheme.Label(panel,"Rules preview","将棋で仕掛け、FPSで勝ち取る。\n相手の王を倒せば勝利。ローカル2人対戦。",55,225,610,85,26,UiTheme.Muted,TextAnchor.MiddleCenter);
            connection=UiTheme.Label(panel,"Connection message","",55,327,610,76,24,UiTheme.Paper,TextAnchor.MiddleCenter);
            var start=UiTheme.Button(panel,"Start button","対局開始",60,428,600,84,session.StartMatch,true);
            UiTheme.Button(panel,"Help button","遊び方",60,536,290,70,session.OpenHelp);
            UiTheme.Button(panel,"Settings button","設定",370,536,290,70,session.OpenSettings);
            UiTheme.Button(panel,"Exit button","終了",60,630,600,64,session.Quit);
            UiTheme.Label(panel,"Menu controls","↑↓ / 十字キーで選択　Enter / Aで決定\nマウスでも操作できます",40,735,640,70,22,UiTheme.Muted,TextAnchor.MiddleCenter);
            Focus(start); UpdateDevices();
        }
        public void Pause()
        {
            Standard("一時停止");
            UiTheme.Label(panel,"Pause hint","対局の状態を保持しています。",44,126,992,48,27,UiTheme.Muted);
            var resume=UiTheme.Button(panel,"Resume button","対局に戻る",180,225,720,78,session.Resume,true);
            UiTheme.Button(panel,"Help button","遊び方",180,332,720,72,session.OpenHelp);
            UiTheme.Button(panel,"Settings button","設定",180,433,720,72,session.OpenSettings);
            UiTheme.Button(panel,"Return title button","タイトルへ戻る",180,534,720,72,session.ConfirmReturn);
            UiTheme.Label(panel,"Pause controls","Esc / START：再開",44,713,992,52,25,UiTheme.Muted,TextAnchor.MiddleCenter); Focus(resume);
        }
        public void Help(bool abilities=false)
        {
            Standard("遊び方");
            UiTheme.Button(panel,"Basics tab","基本操作・ルール",44,130,475,58,()=>Help(false),!abilities);
            UiTheme.Button(panel,"Abilities tab","駒と能力",545,130,491,58,()=>Help(true),abilities);
            if(!abilities)
            {
                UiTheme.Label(panel,"Rule instructions","1. 自分の駒を選び、光っているマスへ動かします。\n2. 相手の駒に進むと、左右に分かれたFPSの決闘が始まります。\n3. 決闘で倒した相手の駒は持ち駒に。空きマスへ打てます。\n4. 相手の王を決闘で倒すと勝利。成れる駒は自動で成ります。",44,215,992,180,26);
                UiTheme.Label(panel,"Keyboard instructions","キーボード / マウス\n将棋：WASD 移動 / F 決定 / C 取消 / E 持ち駒\nFPS：WASD 移動 / マウス 視点\n左クリック 攻撃 / 右クリック 防御・ズーム\nE スキル / Space 登る / Esc メニュー",44,410,992,184,25);
                UiTheme.Label(panel,"Gamepad instructions","ゲームパッド（Xbox表記）\n将棋：十字キー・左スティック 移動 / A 決定 / B 取消 / X 持ち駒\nFPS：左スティック 移動 / 右スティック 視点 / RT 攻撃\nLT 防御・ズーム / B スキル / A 登る / START メニュー",44,599,992,145,24,UiTheme.Muted);
            }
            else
            {
                string[] rows={"歩　｜　近接武器　｜　前へ跳び込む", "香車　｜　短銃　｜　閃光弾で視界を奪う", "桂馬　｜　近接武器　｜　突進して周囲を攻撃", "銀　｜　近接武器　｜　一時的に強力な防御", "金　｜　近接武器　｜　一定時間、防御を強化", "角　｜　長銃　｜　上昇して滞空", "飛車　｜　弓　｜　相手を遅くする範囲を作る", "王　｜　近接武器　｜　砲台を展開して援護"};
                for(int i=0;i<rows.Length;i++) UiTheme.Label(panel,"Ability row "+i,rows[i],56,215+i*55,968,48,27);
                UiTheme.Label(panel,"Cooldown explanation","攻撃・防御・スキルは、再使用までの時間が画面下に表示されます。",44,678,992,55,24,UiTheme.Muted);
            }
            Focus(UiTheme.Button(panel,"Back button","戻る",390,748,300,58,session.Back));
        }
        public void Settings()
        {
            Standard("設定");
            UiTheme.Label(panel,"Sensitivity explanation","左右の視点の速さを、プレイヤーごとに調整できます。",44,145,992,70,27,UiTheme.Muted);
            for(int p=0;p<2;p++)
            {
                int index=p; float y=255+p*180;
                var value=UiTheme.Label(panel,"Sensitivity value "+p,"",70,y,940,48,30);
                var track=UiTheme.Box(panel,"Sensitivity slider "+p,85,y+80,900,18,new Color(.35f,.29f,.2f),true).rectTransform;
                var slider=track.gameObject.AddComponent<Slider>();
                var handle=UiTheme.Box(track,"Handle",0,-11,32,40,UiTheme.Gold,true).rectTransform;
                slider.handleRect=handle; slider.targetGraphic=handle.GetComponent<Image>(); slider.direction=Slider.Direction.LeftToRight;
                slider.minValue=.5f; slider.maxValue=5; slider.value=session.Sensitivity(p);
                value.text=$"{p+1}P 視点感度　{slider.value:0.0}";
                slider.onValueChanged.AddListener(v=>{session.SetSensitivity(index,v);value.text=$"{index+1}P 視点感度　{v:0.0}";});
            }
            UiTheme.Label(panel,"Settings controls","スライダーを選択し、左右キー / スティックで調整。設定は保存されます。",44,651,992,60,25,UiTheme.Muted);
            Focus(UiTheme.Button(panel,"Back button","戻る",390,748,300,58,session.Back));
        }
        public void Connection()
        {
            Standard("プレイヤーの接続");
            UiTheme.Label(panel,"Connection explanation","ゲームパッド2台、または\nキーボード・マウス + ゲームパッド1台で遊べます。",44,155,992,120,30);
            connection=UiTheme.Label(panel,"Connection message","",44,310,992,150,34,UiTheme.Gold,TextAnchor.MiddleCenter);
            UiTheme.Label(panel,"Reconnect hint",session.ConnectionStartsMatch?"2人分の入力機器を接続すると、新しい対局を始められます。":"対局は一時停止中です。接続を確認してから再開できます。",44,505,992,90,26,UiTheme.Muted,TextAnchor.MiddleCenter);
            readyButton=UiTheme.Button(panel,"Connection ready button",session.ConnectionStartsMatch?"対局開始":"対局に戻る",180,620,720,74,session.ConnectionStartsMatch?(UnityEngine.Events.UnityAction)session.StartMatch:session.Resume,true);
            var back=UiTheme.Button(panel,"Connection back button","タイトルへ",390,736,300,58,session.IsTitle?(UnityEngine.Events.UnityAction)session.ReturnTitle:session.ConfirmReturn);
            UpdateDevices(); Focus(readyButton.interactable?readyButton:back);
        }
        public void UpdateDevices()
        {
            if(connection!=null) connection.text=$"1P　{session.Devices.Description(0)}\n2P　{session.Devices.Description(1)}";
            if(readyButton!=null) readyButton.interactable=session.Devices.Ready;
        }
        public void Result(int winner)
        {
            Standard($"{winner}P の勝利");
            UiTheme.Label(panel,"Win reason","相手の王を決闘で倒しました。",44,218,992,110,36,UiTheme.Paper,TextAnchor.MiddleCenter);
            UiTheme.Label(panel,"Result instructions","次の対局を始めるか、タイトルへ戻れます。",44,350,992,70,27,UiTheme.Muted,TextAnchor.MiddleCenter);
            var rematch=UiTheme.Button(panel,"Rematch button","もう一局",180,495,720,85,session.StartMatch,true);
            UiTheme.Button(panel,"Result title button","タイトルへ",180,615,720,74,session.ReturnTitle); Focus(rematch);
        }
        public void ConfirmReturn()
        {
            Standard("対局を終了しますか？");
            UiTheme.Label(panel,"Return warning","この対局を終了して、タイトルへ戻ります。",44,250,992,110,32,UiTheme.Paper,TextAnchor.MiddleCenter);
            var cancel=UiTheme.Button(panel,"Cancel return button","対局に戻る",180,465,720,82,session.Resume,true);
            UiTheme.Button(panel,"Confirm return button","終了してタイトルへ",180,585,720,74,session.ReturnTitle); Focus(cancel);
        }
    }
}
