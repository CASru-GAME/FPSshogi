using UnityEngine;
using UnityEngine.UI;
using App.Main.Player;

namespace App.UI
{
    public sealed class DuelHud
    {
        readonly RectTransform root;
        readonly Player[] players;
        readonly Text[] hp=new Text[2], title=new Text[2], abilities=new Text[2], controls=new Text[2];
        readonly Image[] health=new Image[2], hurt=new Image[2];
        readonly int[] previousHp={-1,-1};
        readonly float[] damageUntil=new float[2];
        readonly LocalMatchDevices devices;
        public DuelHud(RectTransform parent,PlayerManager manager,LocalMatchDevices devices)
        {
            this.devices=devices;
            players=new[]{manager.PlayerOne.GetComponent<Player>(),manager.PlayerTwo.GetComponent<Player>()};
            root=UiTheme.Rect(parent,"Duel HUD",0,0,1920,1080);
            for(int p=0;p<2;p++)
            {
                int offset=p*960;
                hurt[p]=UiTheme.Box(root,$"P{p+1} damage",offset,0,960,1080,Color.clear);
                UiTheme.Box(root,$"P{p+1} status backing",offset+28,26,904,138,UiTheme.Ink);
                title[p]=UiTheme.Label(root,$"P{p+1} identity","",offset+50,30,860,48,29,UiTheme.Gold);
                hp[p]=UiTheme.Label(root,$"P{p+1} HP","",offset+50,78,860,46,27);
                UiTheme.Box(root,"HP track",offset+50,130,860,13,new Color(.3f,.25f,.2f));
                health[p]=UiTheme.Box(root,$"P{p+1} health bar",offset+50,130,860,13,UiTheme.Blue);
                var cross=UiTheme.Rect(root,$"P{p+1} Crosshair",offset+464,524,32,32);
                UiTheme.Box(cross,"Shadow horizontal",0,13,32,6,UiTheme.Ink);
                UiTheme.Box(cross,"Shadow vertical",13,0,6,32,UiTheme.Ink);
                UiTheme.Box(cross,"Horizontal",2,15,28,2,Color.white);
                UiTheme.Box(cross,"Vertical",15,2,2,28,Color.white);
                UiTheme.Box(root,$"P{p+1} ability backing",offset+28,895,904,158,UiTheme.Ink);
                abilities[p]=UiTheme.Label(root,$"P{p+1} cooldowns","",offset+48,904,864,93,26);
                controls[p]=UiTheme.Label(root,$"P{p+1} controls","",offset+48,1000,864,40,21,UiTheme.Muted);
            }
            UiTheme.Box(root,"Screen divider",958,0,4,1080,UiTheme.Gold);
        }
        static string Ready(float remaining) => remaining>.05f?$"あと {remaining:0.0} 秒":"使用可能";
        public void Tick(bool visible)
        {
            root.gameObject.SetActive(visible);
            if(!visible) { previousHp[0]=previousHp[1]=-1; return; }
            for(int i=0;i<2;i++)
            {
                var p=players[i]; var status=p.playerStatus;
                if(status==null) continue;
                if(previousHp[i]>=0&&status.Hp.Current<previousHp[i]) damageUntil[i]=Time.unscaledTime+.45f;
                previousHp[i]=status.Hp.Current;
                float flash=Mathf.Clamp01((damageUntil[i]-Time.unscaledTime)/.45f);
                hurt[i].color=new Color(.85f,.12f,.08f,flash*.32f);
                title[i].text=$"{i+1}P　{PiecePresentation.Name(p.PieceType)}　｜　{PiecePresentation.Weapon(p.PieceType)}";
                hp[i].text=$"HP  {status.Hp.Current} / {status.Hp.Max}"+(flash>0?"　被弾！":"");
                health[i].rectTransform.sizeDelta=new Vector2(860f*status.Hp.Current/Mathf.Max(1,status.Hp.Max),13);
                health[i].color=status.Hp.Current<=status.Hp.Max*.25f?new Color(1,.36f,.2f):UiTheme.Blue;
                string primary=devices.IsGamepad(i)?"RT":"左クリック", secondary=devices.IsGamepad(i)?"LT":"右クリック", skill=devices.IsGamepad(i)?"B":"E";
                abilities[i].text=$"{primary} 攻撃：{Ready(p.PrimaryCooldown)}　 {secondary} {p.SecondaryName}：{Ready(p.SecondaryCooldown)}\n{skill} {PiecePresentation.Skill(p.PieceType)}：{Ready(p.SkillCooldown)}";
                controls[i].text=devices.IsGamepad(i)?"左スティック 移動 / 右 視点 / A 登る / START メニュー":"WASD 移動 / マウス 視点 / Space 登る / Esc メニュー";
            }
        }
    }
}
