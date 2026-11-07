using UnityEngine;
using UnityEngine.UI;
using App.Main.Player;

namespace App.UI
{
    public sealed class DuelHud
    {
        readonly RectTransform root;
        readonly Player[] players;
        readonly LocalMatchDevices devices;
        readonly Text[] hp=new Text[2],title=new Text[2],climb=new Text[2],menu=new Text[2];
        readonly Image[] health=new Image[2];
        readonly Image[] drawTrack = new Image[2], drawBar = new Image[2];
        readonly GameObject[] reachMarks = new GameObject[2], equipped = new GameObject[2];
        readonly PlayerWeapon[] weapons = new PlayerWeapon[2];
        readonly UiSymbol[,] icons=new UiSymbol[2,3],rings=new UiSymbol[2,3];
        readonly Text[,] keys=new Text[2,3];
        readonly Image[,] hurt=new Image[2,4];
        readonly int[] previousHp={-1,-1};
        readonly float[] damageUntil=new float[2];
        readonly PlayerStatus[] lastStatus=new PlayerStatus[2];
        readonly int[] lastImpact=new int[2],damageEdge={-1,-1};
        readonly bool[] blockedImpact=new bool[2];
        static void Anchor(RectTransform rect,Vector2 anchor,Vector2 pivot,Vector2 position){rect.anchorMin=rect.anchorMax=anchor;rect.pivot=pivot;rect.anchoredPosition=position;}
        public DuelHud(RectTransform parent,PlayerManager manager,LocalMatchDevices devices)
        {
            this.devices=devices;players=new[]{manager.PlayerOne.GetComponent<Player>(),manager.PlayerTwo.GetComponent<Player>()};
            root=UiTheme.Rect(parent,"Duel HUD",0,0,0,0);UiTheme.Stretch(root);
            root.SetAsFirstSibling(); // Draw the divider and combat overlays beneath full-screen menus.
            for(int p=0;p<2;p++){
                var half=UiTheme.Rect(root,$"P{p+1} viewport HUD",0,0,0,0);half.anchorMin=new Vector2(p*.5f,0);half.anchorMax=new Vector2((p+1)*.5f,1);half.offsetMin=half.offsetMax=Vector2.zero;
                var topShade=UiTheme.Shade(half,"HUD top shade",0,0,0,180,UiSymbol.Kind.TopShade,.8f,.25f);
                topShade.anchorMin=Vector2.up;topShade.anchorMax=Vector2.one;topShade.offsetMin=new Vector2(0,-180);topShade.offsetMax=Vector2.zero;
                var bottomShade=UiTheme.Shade(half,"HUD bottom shade",0,0,0,250,UiSymbol.Kind.BottomShade,.9f,.25f);
                bottomShade.anchorMin=Vector2.zero;bottomShade.anchorMax=Vector2.right;bottomShade.offsetMin=Vector2.zero;bottomShade.offsetMax=new Vector2(0,250);
                for(int e=0;e<4;e++){
                    hurt[p,e]=UiTheme.Box(half,$"P{p+1} damage edge {e}",0,0,0,0,Color.clear);var r=hurt[p,e].rectTransform;
                    r.anchorMin=e==0||e==2?Vector2.zero:e==1?Vector2.right:Vector2.up;
                    r.anchorMax=e==0?Vector2.up:e==1?Vector2.one:e==2?Vector2.right:Vector2.one;
                    r.pivot=e==0?new Vector2(0,.5f):e==1?new Vector2(1,.5f):e==2?new Vector2(.5f,0):new Vector2(.5f,1);
                    r.sizeDelta=e<2?new Vector2(8,0):new Vector2(0,8);r.anchoredPosition=Vector2.zero;
                }
                var identity=UiTheme.Rect(half,$"P{p+1} identity backing",30,28,320,54);
                title[p]=UiTheme.Label(identity,$"P{p+1} identity","",0,4,320,46,28,UiTheme.PlayerColor(p));
                var menuPanel=UiTheme.Rect(half,"Menu hint backing",0,0,175,44);Anchor(menuPanel,Vector2.one,Vector2.one,new Vector2(-30,-30));
                UiTheme.Symbol(menuPanel,"Pause symbol",UiSymbol.Kind.Pause,9,8,28);menu[p]=UiTheme.Label(menuPanel,$"P{p+1} menu key","",45,2,121,40,22);
                var cross=UiTheme.Rect(half,$"P{p+1} Crosshair",0,0,28,28);Anchor(cross,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero);
                UiTheme.Box(cross,"Shadow horizontal",0,11,28,6,UiTheme.Shadow);UiTheme.Box(cross,"Shadow vertical",11,0,6,28,UiTheme.Shadow);
                UiTheme.Box(cross,"Horizontal",2,13,24,2,Color.white);UiTheme.Box(cross,"Vertical",13,2,2,24,Color.white);
                reachMarks[p] = UiTheme.Symbol(cross, $"P{p+1} melee reach", UiSymbol.Kind.MeleeReach, -10, -10, 48, Color.white).gameObject;
                var outline = reachMarks[p].AddComponent<Outline>();
                outline.effectColor = UiTheme.Shadow;
                outline.effectDistance = new Vector2(1.25f, -1.25f);
                reachMarks[p].SetActive(false);
                drawTrack[p] = UiTheme.Box(cross, $"P{p+1} bow draw track", -16, 40, 60, 6, UiTheme.Shadow);
                drawBar[p] = UiTheme.Box(drawTrack[p].transform, "Charge", 1, 1, 0, 4, UiTheme.Ink);
                drawTrack[p].gameObject.SetActive(false);
                var status=UiTheme.Rect(half,$"P{p+1} status backing",0,0,370,82);Anchor(status,Vector2.zero,Vector2.zero,new Vector2(30,30));
                UiTheme.Symbol(status,"Heart",UiSymbol.Kind.Heart,18,22,40,UiTheme.PlayerColor(p));
                UiTheme.Box(status,"HP track",78,34,265,14,UiTheme.Track);health[p]=UiTheme.Box(status,$"P{p+1} health bar",78,34,265,14,UiTheme.PlayerColor(p));
                hp[p]=UiTheme.Label(status,$"P{p+1} HP","",78,50,265,28,18,UiTheme.Muted,TextAnchor.MiddleRight);
                var actions=UiTheme.Rect(half,$"P{p+1} actions",0,0,322,122);Anchor(actions,Vector2.right,Vector2.right,new Vector2(-30,30));
                for(int a=0;a<3;a++){
                    icons[p,a]=UiTheme.Symbol(actions,$"P{p+1} action {a}",UiSymbol.Kind.Sword,30+a*100,20,48);
                    rings[p,a]=UiTheme.Symbol(actions,$"P{p+1} cooldown {a}",UiSymbol.Kind.Ring,21+a*100,11,66,UiTheme.PlayerColor(p));rings[p,a].Fill=0;
                    keys[p,a]=UiTheme.Key(actions,$"P{p+1} action key {a}","",17+a*100,83,76,29);
                }
                var climbPanel=UiTheme.Rect(half,$"P{p+1} climb backing",0,0,210,45);Anchor(climbPanel,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,182));
                climb[p]=UiTheme.Label(climbPanel,$"P{p+1} climb","",10,0,190,45,23,UiTheme.Ink,TextAnchor.MiddleCenter);
            }
            var divider=UiTheme.Box(root,"Screen divider",0,0,2,0,UiTheme.Shadow).rectTransform;
            divider.anchorMin=new Vector2(.5f,0);divider.anchorMax=new Vector2(.5f,1);divider.pivot=new Vector2(.5f,.5f);divider.anchoredPosition=Vector2.zero;
        }
        public void Tick(bool visible)
        {
            root.gameObject.SetActive(visible);if(!visible){previousHp[0]=previousHp[1]=-1;return;}
            for(int i=0;i<2;i++){
                var p=players[i];var status=p.playerStatus;if(status==null)continue;
                if(lastStatus[i]!=status){lastStatus[i]=status;lastImpact[i]=0;damageUntil[i]=0;damageEdge[i]=-1;blockedImpact[i]=false;}
                if(previousHp[i]>=0&&status.Hp.Current<previousHp[i]){damageUntil[i]=Time.unscaledTime+.4f;damageEdge[i]=-1;blockedImpact[i]=false;}previousHp[i]=status.Hp.Current;
                if(status.ImpactVersion!=lastImpact[i]){
                    lastImpact[i]=status.ImpactVersion;damageUntil[i]=Time.unscaledTime+.4f;blockedImpact[i]=status.ImpactBlocked;damageEdge[i]=-1;
                    if(status.ImpactSource.HasValue){var camera=p.GetComponentInChildren<Camera>();float yaw=camera!=null?camera.transform.eulerAngles.y:p.transform.eulerAngles.y;
                        var direction=Quaternion.Inverse(Quaternion.Euler(0,yaw,0))*(status.ImpactSource.Value-p.transform.position);
                        damageEdge[i]=Mathf.Abs(direction.x)>Mathf.Abs(direction.z)?direction.x<0?0:1:direction.z<0?2:3;
                    }
                }
                float flash=Mathf.Clamp01((damageUntil[i]-Time.unscaledTime)/.4f);
                for(int e=0;e<4;e++){var tint=blockedImpact[i]?UiTheme.Gold:new Color(.8f,.16f,.1f);tint.a=(damageEdge[i]<0||damageEdge[i]==e)?flash*.8f:0;hurt[i,e].color=tint;}
                title[i].text=$"{i+1}P　{PiecePresentation.Name(p.PieceType)}";
                hp[i].gameObject.SetActive(GameSessionUI.Instance.ShowHpNumbers);hp[i].text=$"{status.Hp.Current} / {status.Hp.Max}";
                health[i].rectTransform.sizeDelta=new Vector2(265f*Mathf.Clamp01((float)status.Hp.Current/Mathf.Max(1,status.Hp.Max)),14);
                health[i].color=status.Hp.Current<=status.Hp.Max*.25f?new Color(.7f,.18f,.1f):UiTheme.PlayerColor(i);
                bool pad=devices.IsGamepad(i);menu[i].text=pad?"START":"Esc";
                float draw = p.BowDrawAmount;
                drawTrack[i].gameObject.SetActive(draw > 0f);
                if (draw > 0f)
                {
                    drawBar[i].rectTransform.sizeDelta = new Vector2(58f * draw, 4f);
                    drawBar[i].color = draw >= 1f ? UiTheme.Gold : UiTheme.Ink;
                }
                keys[i,0].text=pad?"RT":"左";keys[i,1].text=pad?"LT":"右";keys[i,2].text=pad?"B":"E";
                icons[i,0].SetShape(PiecePresentation.WeaponSymbol(p.PieceType));icons[i,1].SetShape(p.SecondaryName=="ズーム"?UiSymbol.Kind.Zoom:UiSymbol.Kind.Shield);icons[i,2].SetShape(PiecePresentation.SkillSymbol(p.PieceType));
                Cooldown(i,0,p.PrimaryCooldown,p.PrimaryCooldownDuration);Cooldown(i,1,p.SecondaryCooldown,p.SecondaryCooldownDuration);Cooldown(i,2,p.SkillCooldown,p.SkillCooldownDuration);
                climb[i].transform.parent.gameObject.SetActive(!GameSessionUI.BlocksGameplay&&p.ClimbAvailable);climb[i].text=(pad?"A":"Space")+"　登る";
            }
        }
        public void RefreshReach()
        {
            if (!root.gameObject.activeInHierarchy) return;
            for (int i = 0; i < players.Length; i++)
            {
                var weapon = players[i].WeaponObject;
                if (equipped[i] != weapon)
                {
                    equipped[i] = weapon;
                    weapons[i] = weapon != null ? weapon.GetComponent<PlayerWeapon>() : null;
                }
                reachMarks[i].SetActive(weapons[i] != null && weapons[i].CanReachOpponent);
            }
        }
        void Cooldown(int p,int action,float remaining,float duration){rings[p,action].Fill=duration>0?remaining/duration:0;icons[p,action].color=remaining>.05f?UiTheme.Muted:UiTheme.Ink;}
    }
}
