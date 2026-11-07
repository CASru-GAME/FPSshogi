using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace App.UI
{
    public sealed class PlayerSelectionPanel
    {
        readonly GameSessionUI session;
        readonly Text[] device=new Text[2],state=new Text[2],controls=new Text[2];
        readonly UiSymbol[] icons=new UiSymbol[2],checks=new UiSymbol[2];
        readonly Text status,keyboardCaption;
        readonly Button keyboardButton;
        public PlayerSelectionPanel(RectTransform parent,GameSessionUI session)
        {
            this.session=session;var panel=UiTheme.Rect(parent,"Player selection panel",0,0,1920,1080);
            UiTheme.Label(panel,"Selection instructions","使う機器で参加すると、1P・2Pが決まります。",90,160,1740,60,30,UiTheme.Ink);
            for(int p=0;p<2;p++){
                var card=UiTheme.Rect(panel,$"P{p+1} selection card",260+p*760,280,640,440);
                UiTheme.Box(card,"Player stripe",170,0,300,2,UiTheme.PlayerColor(p));
                UiTheme.Label(card,"Player heading",$"{p+1}P",24,20,592,76,48,UiTheme.Ink,TextAnchor.MiddleCenter);
                icons[p]=UiTheme.Symbol(card,"Device icon",UiSymbol.Kind.Gamepad,280,107,80,UiTheme.PlayerColor(p));
                device[p]=UiTheme.Label(card,$"P{p+1} selection device","",30,202,580,85,28,UiTheme.Ink,TextAnchor.MiddleCenter);
                device[p].resizeTextForBestFit=true;device[p].resizeTextMinSize=23;device[p].resizeTextMaxSize=28;
                state[p]=UiTheme.Label(card,$"P{p+1} selection state","",30,287,580,48,31,UiTheme.PlayerColor(p),TextAnchor.MiddleCenter);
                checks[p]=UiTheme.Symbol(card,"Ready check",UiSymbol.Kind.Check,148,291,38,UiTheme.PlayerColor(p));
                controls[p]=UiTheme.Label(card,$"P{p+1} selection controls","",30,351,580,76,24,UiTheme.Muted,TextAnchor.MiddleCenter);
            }
            status=UiTheme.Label(panel,"Selection status","",90,773,1740,65,30,UiTheme.Ink,TextAnchor.MiddleCenter);
            keyboardButton=UiTheme.Button(panel,"Keyboard selection button","キーマウで参加",90,920,700,70,session.KeyboardSelectionAction);keyboardCaption=keyboardButton.GetComponentInChildren<Text>();
            UiTheme.Button(panel,"Selection help button","遊び方",1490,920,340,70,session.OpenHelp);
            UiTheme.Box(panel,"Selection footer rule",90,1001,1740,1,UiTheme.Rule);
            UiTheme.Label(panel,"Selection footer","A / Enter：参加・準備完了　　B / Esc：準備取消 → 参加解除　　VIEW / F1：遊び方",90,1010,1740,42,23,UiTheme.Muted,TextAnchor.MiddleCenter);Tick();
        }
        public void Tick()
        {
            var d=session.Devices;
            for(int p=0;p<2;p++){
                bool connected=d.Connected(p),ready=d.IsPrepared(p);device[p].text=connected?d.Description(p):"";
                state[p].text=ready?"準備完了":connected?"参加済み":"参加待ち";
                icons[p].SetShape(connected&&!d.IsGamepad(p)?UiSymbol.Kind.Keyboard:UiSymbol.Kind.Gamepad);checks[p].gameObject.SetActive(ready);
                controls[p].text=connected?(ready?(d.IsGamepad(p)?"B":"Esc")+"：準備を戻す":(d.IsGamepad(p)?"A":"Enter")+"：準備完了\n"+(d.IsGamepad(p)?"B":"Esc")+"：参加を取り消す"):"A / Enter で参加";
            }
            status.text=d.AllPrepared?"両者準備完了":"両者の準備完了後に開始";
            int slot=d.PlayerFor(Keyboard.current);keyboardButton.interactable=Keyboard.current!=null&&Keyboard.current.enabled&&Mouse.current!=null&&Mouse.current.enabled&&(slot>=0||!d.Ready);
            keyboardCaption.text=slot<0?"キーマウで参加":d.IsPrepared(slot)?$"{slot+1}P キーマウ：準備を戻す":$"{slot+1}P キーマウ：準備完了";
        }
    }
}
