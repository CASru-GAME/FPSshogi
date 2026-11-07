#if UNITY_EDITOR || FPS_AUDIO_SMOKE
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using App.Audio;
using App.Main.GameMaster;
using App.Main.Player;
using App.Main.ShogiThings;
using App.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class AudioRegressionScenario
{
    const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
    static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindFirstObjectByType<T>();
    static object Get(object o,string name)=>o.GetType().GetField(name,Private).GetValue(o);
    static void Set(object o,string name,object value)=>o.GetType().GetField(name,Private).SetValue(o,value);
    static IPiece Piece(PieceType type,PlayerType player)=>type switch {
        PieceType.Fuhyo=>new Fuhyo(player),PieceType.Kyosya=>new Kyosya(player),PieceType.Keima=>new Keima(player),
        PieceType.Gin=>new Gin(player),PieceType.Kin=>new Kin(player),PieceType.Kakugyo=>new Kakugyo(player),
        PieceType.Hisya=>new Hisya(player),_=>new King(player)};
    static void FinishByShot(Player one,Player two)
    {
        one.GetComponent<Rigidbody>().constraints=two.GetComponent<Rigidbody>().constraints=RigidbodyConstraints.FreezeAll;
        one.transform.position=new Vector3(0,600,0);two.transform.position=new Vector3(0,600,7);Physics.SyncTransforms();
        one.GetComponentInChildren<Camera>().transform.LookAt(two.GetComponent<Collider>().bounds.center);
        two.playerStatus.DefensePoint.Reset();two.playerStatus.Hp.Subtract(two.playerStatus.Hp.Current-1);
        new LongGunPrimary().PrimaryAction(one,one.playerStatus);
    }
    static IEnumerator MouseClick(Mouse mouse,string name)
    {
        var rect=GameObject.Find(name).GetComponent<RectTransform>();var corners=new Vector3[4];rect.GetWorldCorners(corners);
        Vector2 center=(corners[0]+corners[2])*.5f;
        InputSystem.QueueStateEvent(mouse,new MouseState{position=center,buttons=1});yield return .12d;
        InputSystem.QueueStateEvent(mouse,new MouseState{position=center});yield return .2d;
    }
    public static IEnumerator Run(Action<bool,string> check,string output,bool capture)
    {
        var played=new List<GameAudio.Playback>();var trace=new List<string>();var coverage=new Dictionary<SoundCue,int>();
        void Observe(GameAudio.Playback p){coverage.TryGetValue(p.cue,out int n);coverage[p.cue]=n+1;played.Add(p);trace.Add($"{Time.unscaledTime:F3}\t{p.cue}\t{p.clip}\tplayer={p.player}\tpan={p.pan:F2}");}
        int Count(SoundCue cue)=>played.Count(p=>p.cue==cue);
        var saved=Enum.GetValues(typeof(AudioBus)).Cast<AudioBus>().ToDictionary(b=>b,b=>GameAudio.GetVolume(b));
        var hadKeys=saved.Keys.ToDictionary(b=>b,b=>PlayerPrefs.HasKey("FPSshogi.Audio."+b));
        Gamepad first=null,second=null;Mouse mouse=null;AudioOutputProbe probe=null;
        void Record(string name)
        {
            if(!capture)return;
            var result=probe.Save(Path.Combine(output,name+".wav"));
            File.AppendAllText(Path.Combine(output,"levels.tsv"),$"{name}\t{result.peak:F6}\t{result.rms:F6}\t{result.count}\n");
            check(result.count>0&&result.peak>.0001f,"Native mixed output audible: "+name);
            check(result.peak<.99f,"Native mixed output has headroom: "+name);
        }
        try
        {
            GameAudio.ResetVolumes();GameAudio.Played+=Observe;
            check(GameAudio.Instance!=null,"Persistent audio service starts in Title");
            foreach(SoundCue cue in Enum.GetValues(typeof(SoundCue)))check(GameAudio.HasCue(cue),"Authored clip loaded: "+cue);
            float assetPeak=0;
            foreach(var clip in Resources.LoadAll<AudioClip>("Audio/Clips"))
            {
                var data=new float[clip.samples*clip.channels];clip.GetData(data,0);
                foreach(float v in data)assetPeak=Mathf.Max(assetPeak,Mathf.Abs(v));
            }
            check(assetPeak>.1f&&assetPeak<=.71f,"Unity import preserves authored PCM headroom: "+assetPeak.ToString("F4"));
            check(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(l=>l.enabled)==1,"One enabled shared listener");
            if(capture)probe=GameAudio.Instance.GetComponentInChildren<AudioListener>().gameObject.AddComponent<AudioOutputProbe>();
            mouse=InputSystem.AddDevice<Mouse>("Audio test mouse");first=InputSystem.AddDevice<Gamepad>("Audio test 1P");
            var rect=GameObject.Find("Settings button").GetComponent<RectTransform>();var corners=new Vector3[4];rect.GetWorldCorners(corners);
            Vector2 center=(corners[0]+corners[2])*.5f;
            probe?.Begin();InputSystem.QueueStateEvent(mouse,new MouseState{position=center});yield return .3d;played.Clear();
            InputSystem.QueueStateEvent(mouse,new MouseState{position=center,buttons=1});yield return .12d;
            InputSystem.QueueStateEvent(mouse,new MouseState{position=center});yield return .3d;
            check(Count(SoundCue.UiOpen)==1&&Count(SoundCue.UiConfirm)==0&&Count(SoundCue.UiNavigate)==0,"Real mouse click after hover emits only the semantic open sound");
            check(UnityEngine.Object.FindObjectsByType<Slider>(FindObjectsSortMode.None).Length==5,"Settings exposes two sensitivities and three volume sliders");
            if(capture){ScreenCapture.CaptureScreenshot(Path.Combine(output,"settings.png"));yield return .2d;}
            played.Clear();GameObject.Find("Back button").GetComponent<Button>().onClick.Invoke();yield return .3d;
            check(Count(SoundCue.UiBack)==1&&Count(SoundCue.UiConfirm)==0,"Back button has no extra confirmation sound");
            played.Clear();InputSystem.QueueStateEvent(first,new GamepadState().WithButton(GamepadButton.DpadUp));yield return .13d;
            InputSystem.QueueStateEvent(first,new GamepadState());yield return .2d;
            check(Count(SoundCue.UiNavigate)==1,"Gamepad menu navigation produces one focus sound");
            var submit=SelectionTestInput.Tap(first);while(submit.MoveNext())yield return submit.Current;
            check(Count(SoundCue.UiOpen)==1&&Count(SoundCue.UiConfirm)==0,"Gamepad submit opens menu once");
            GameSessionUI.Instance.Back();yield return .3d;
            Record("01-menu");
            played.Clear();
            for(int i=0;i<4;i++){GameAudio.Play(SoundCue.BoardCursor);yield return .09d;}
            var variants=played.Where(p=>p.cue==SoundCue.BoardCursor).ToArray();
            check(variants.Length==4&&Enumerable.Range(1,3).All(i=>variants[i].clip!=variants[i-1].clip),"Repeated actions avoid immediate sample repetition");
            played.Clear();for(int i=0;i<50;i++)GameAudio.Play(SoundCue.UiInvalid);
            check(Count(SoundCue.UiInvalid)==1,"Invalid feedback is rate limited");

            second=InputSystem.AddDevice<Gamepad>("Audio test 2P");
            GameSessionUI.Instance.StartMatch();yield return .35d;
            var lobbyTap=SelectionTestInput.Tap(first);while(lobbyTap.MoveNext())yield return lobbyTap.Current;
            played.Clear();var leave=SelectionTestInput.Tap(first,true);while(leave.MoveNext())yield return leave.Current;
            check(Count(SoundCue.PlayerLeave)==1,"Assigned player leaving the lobby has one departure sound");
            lobbyTap=SelectionTestInput.Tap(first);while(lobbyTap.MoveNext())yield return lobbyTap.Current;
            played.Clear();InputSystem.RemoveDevice(first);yield return .3d;
            check(played.Any(p=>p.cue==SoundCue.Disconnect&&p.player==0),"Unplugging a single lobby participant emits its seat disconnect");
            GameSessionUI.Instance.CancelSelection();first=InputSystem.AddDevice<Gamepad>("Audio replacement 1P");yield return .3d;
            played.Clear();var join=SelectionTestInput.TwoGamepads(first,second);
            while(join.MoveNext())yield return join.Current;
            float deadline=Time.unscaledTime+8;
            while((SceneManager.GetActiveScene().name!="Main"||GameSessionUI.BlocksGameplay)&&Time.unscaledTime<deadline)yield return .1d;
            check(!GameSessionUI.BlocksGameplay,"New match finished loading and released the board input gate");
            check(Count(SoundCue.PlayerJoin)==2&&Count(SoundCue.PlayerReady)==2,"Actual device joining and readiness have distinct cues");
            check(played.Where(p=>p.cue==SoundCue.PlayerReady).Select(p=>p.player).Distinct().Count()==2,"Both ready cues use their own seat and repeat gate");
            check(GameAudio.Instance!=null&&SceneManager.GetActiveScene().name=="Main","Audio and selection survive scene load");
            var pointer=Find<App.Main.ShogiPointer.ShogiPointer>();var board=Find<ShogiBoard>();var state=Find<GameStateHolder>();
            played.Clear();pointer.SetCursor(0,2,false);pointer.Select();pointer.Deselect();yield return .2d;
            check(Count(SoundCue.BoardCancel)==1,"Deselecting a chosen piece emits board cancellation");
            played.Clear();var pick=MouseClick(mouse,"Square 0,2");while(pick.MoveNext())yield return pick.Current;
            var place=MouseClick(mouse,"Square 0,3");while(place.MoveNext())yield return place.Current;
            check(Count(SoundCue.BoardSelect)==1&&Count(SoundCue.BoardMove)==1,"Valid board move plays select and placement");
            check(Count(SoundCue.BoardCursor)==0,"Atomic board clicks do not double-play independent cursor movement");
            played.Clear();state.ChangeState(StateOne);pointer.SetCursor(4,4);pointer.Select();
            check(Count(SoundCue.UiInvalid)==1,"Illegal board action emits rejection without HUD polling");
            state.ChangeState(StateOne);board.GetBoard()[4,5]=new Fuhyo(PlayerType.PlayerOne);board.GetBoard()[4,6]=null;
            played.Clear();board.MovePiece(4,5,4,6,PlayerType.PlayerOne);yield return .3d;
            check(Count(SoundCue.Promote)==1,"Promotion emits a single cue");
            state.ChangeState(StateOne);board.GetBoard()[4,7]=null;played.Clear();board.MovePiece(4,6,4,7,PlayerType.PlayerOne);yield return .3d;
            check(Count(SoundCue.Promote)==0,"Promoted piece does not replay promotion on later moves");
            var captured=(Dictionary<PlayerType,List<PieceType>>)Get(board,"capturedPieces");captured[PlayerType.PlayerOne].Add(PieceType.Gin);
            state.ChangeState(StateOne);board.GetBoard()[4,4]=null;played.Clear();board.SetPiece(4,4,PieceType.Gin,PlayerType.PlayerOne);
            check(Count(SoundCue.BoardDrop)==1,"Legal captured-piece drop has its own placement sound");
            played.Clear();InputSystem.RemoveDevice(first);yield return .3d;InputSystem.RemoveDevice(second);yield return .35d;
            check(played.Where(p=>p.cue==SoundCue.Disconnect).Select(p=>p.player).Distinct().Count()==2,"Further loss on the reconnect screen still identifies each missing seat");
            first=InputSystem.AddDevice<Gamepad>("Audio reconnect 1P");second=InputSystem.AddDevice<Gamepad>("Audio reconnect 2P");yield return .65d;
            var session=GameSessionUI.Instance;session.ConfirmReconnect(session.Devices.DevicesFor(0).FirstOrDefault());session.ConfirmReconnect(session.Devices.DevicesFor(1).FirstOrDefault());yield return .4d;
            check(!GameSessionUI.BlocksGameplay&&session.Devices.AllPrepared,"Both reconnected players resume the match");

            var types=new[]{PieceType.Fuhyo,PieceType.Kyosya,PieceType.Keima,PieceType.Gin,PieceType.Kin,PieceType.Kakugyo,PieceType.Hisya,PieceType.King};
            var attacks=new[]{SoundCue.SpearSwing,SoundCue.ShortGun,SoundCue.SwordSwing,SoundCue.NaginataSwing,SoundCue.NaginataSwing,SoundCue.LongGun,SoundCue.BowRelease,SoundCue.KnifeSwing};
            var skills=new[]{SoundCue.Leap,SoundCue.Throw,SoundCue.Dash,SoundCue.GinShield,SoundCue.KinShield,SoundCue.Ascend,SoundCue.FieldCast,SoundCue.Command};
            for(int index=0;index<types.Length;index++)
            {
                if(output.Contains("ReviewEdges")&&index!=1&&index!=5&&index!=7)continue;
                if(output.Contains("SurfaceProbe")&&index!=1)continue;
                SceneManager.LoadScene("Main");yield return .65d;
                board=Find<ShogiBoard>();board.GetBoard()[4,4]=Piece(types[index],PlayerType.PlayerOne);board.GetBoard()[4,5]=Piece(types[index],PlayerType.PlayerTwo);
                Set(board,"savedFromX",4);Set(board,"savedFromY",4);Set(board,"savedToX",4);Set(board,"savedToY",5);Set(board,"currentPlayer",PlayerType.PlayerOne);
                played.Clear();Find<GameStateHolder>().ChangeStateIntoDuel();yield return 3.55d;
                check(Count(SoundCue.Countdown)==3&&Count(SoundCue.DuelStart)==1,types[index]+" countdown and start match displayed timing");
                var manager=Find<PlayerManager>();var one=manager.PlayerOne.GetComponent<Player>();var two=manager.PlayerTwo.GetComponent<Player>();
                one.GetComponent<Rigidbody>().constraints=two.GetComponent<Rigidbody>().constraints=RigidbodyConstraints.FreezeAll;
                GameAudio.Register(one);GameAudio.Register(two);
                played.Clear();probe?.Begin();
                var primary=(IPrimaryAction)Get(one,"currentPrimaryAction");primary.PrimaryAction(one,one.playerStatus);primary.PrimaryAction(one,one.playerStatus);
                check(Count(attacks[index])==1,types[index]+" accepted attack sounds once; cooldown press is silent");
                yield return .75d;
                if(types[index]==PieceType.Hisya)check(Count(SoundCue.BowNock)==1,"Arrow re-nocking sounds once despite fixed and late pose updates");
                var skill=(ISkill)Get(one,"currentSkill");skill.UseSkill(one,one.playerStatus);skill.UseSkill(one,one.playerStatus);
                check(Count(skills[index])==1,types[index]+" accepted skill sounds once; cooldown press is silent");
                yield return types[index]==PieceType.Kyosya?2.3d:.8d;
                if(types[index]==PieceType.Kyosya)check(Count(SoundCue.FlashBurst)==1,"Flash grenade sounds once at detonation");
                if(types[index]==PieceType.Keima)check(Count(SoundCue.DashStrike)==1,"Dash slash sounds at the landing attack");
                if(types[index]==PieceType.King)check(Count(SoundCue.TurretShot)>0,"Deployed turrets emit shots");
                var secondary=(ISecondaryAction)Get(one,"currentSecondaryAction");played.Clear();
                secondary.SecondaryAction(one,one.playerStatus);secondary.SecondaryAction(one,one.playerStatus);
                check(one.SecondaryName=="ズーム"?Count(SoundCue.AimIn)==1&&Count(SoundCue.AimOut)==1:Count(SoundCue.GuardRaise)==1,types[index]+" secondary action uses accepted guard / aim cues");
                yield return .35d;Record("weapon-"+types[index]);
                if(index==0)
                {
                    var floor=UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).First(c=>c.name=="床");
                    var floorRay=new Ray(floor.bounds.center+new Vector3(40,floor.bounds.extents.y+5,-45),Vector3.down);
                    if(!floor.Raycast(floorRay,out var floorHit,1000))throw new InvalidOperationException("Missing walkable tatami in Field2");
                    var body=one.GetComponent<Collider>();var rb=one.GetComponent<Rigidbody>();
                    float feet=one.transform.position.y-body.bounds.min.y;
                    one.transform.position=floorHit.point+Vector3.up*(feet+.025f);Physics.SyncTransforms();one.GetComponent<PlayerFoley>().ResetTracking();
                    played.Clear();Set(one,"moveInput",Vector2.right);yield return .65d;
                    check(Count(SoundCue.FootstepSoft)+Count(SoundCue.FootstepWood)==0,"Holding movement against a fixed body makes no footsteps");
                    var walkStart=one.transform.position;rb.constraints=RigidbodyConstraints.FreezeRotation;yield return 1.8d;Set(one,"moveInput",Vector2.zero);
                    check(Vector3.Distance(one.transform.position,walkStart)>1.65f&&Count(SoundCue.FootstepSoft)>0,"Actual grounded travel emits footsteps");
                    var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Audio climb check";
                    one.transform.position=new Vector3(0,500,0);one.transform.rotation=Quaternion.identity;Set(one,"yaw",0f);
                    wall.transform.position=one.transform.position+Vector3.forward*1.3f;wall.transform.localScale=new Vector3(5,12,.4f);
                    Physics.SyncTransforms();one.GetComponent<PlayerFoley>().ResetTracking();Set(one,"climbInput",true);played.Clear();yield return .85d;
                    check(one.transform.position.y>501&&Count(SoundCue.Climb)>0,"Climb foley follows actual upward travel beside a wall");
                    Set(one,"climbInput",false);UnityEngine.Object.Destroy(wall);
                    var platform=GameObject.CreatePrimitive(PrimitiveType.Cube);platform.name="Audio landing check";
                    platform.transform.position=new Vector3(one.transform.position.x,495,one.transform.position.z);platform.transform.localScale=new Vector3(16,1,16);
                    Physics.SyncTransforms();played.Clear();float landDeadline=Time.unscaledTime+4;
                    while(Count(SoundCue.Land)==0&&Time.unscaledTime<landDeadline)yield return .05d;
                    yield return .3d;float gap=body.bounds.min.y-platform.GetComponent<Collider>().bounds.max.y;
                    File.AppendAllText(Path.Combine(output,"surfaces.txt"),$"Landing after climb: floor gap {gap:F3}, contacts heard {Count(SoundCue.Land)}\n");
                    check(Mathf.Abs(gap)<.15f&&Count(SoundCue.Land)==1,"Falling from a real climb sounds once on contact with the platform");
                    UnityEngine.Object.Destroy(platform);
                    one.playerStatus.TakeDamage(100000);two.playerStatus.TakeDamage(100000);yield return .25d;
                    check(Count(SoundCue.DuelDraw)==1&&Count(SoundCue.MatchWin)==0,"Simultaneous real deaths emit the draw result once");
                }
                if(index==1)
                {
                    var foley=one.GetComponent<PlayerFoley>();var rb=one.GetComponent<Rigidbody>();var body=one.GetComponent<Collider>();
                    foreach(var ground in new[]{("床",SoundCue.FootstepSoft),("円柱.002",SoundCue.FootstepWood)})
                    {
                        var surface=UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).First(c=>c.name==ground.Item1);
                        var ray=new Ray(surface.bounds.center+Vector3.up*(surface.bounds.extents.y+5),Vector3.down);
                        if(ground.Item1=="床")ray.origin+=new Vector3(40,0,-45);
                        else ray.origin=Find<App.Main.ViewManager.ViewPiece>().boardCellPositions[2,4]+Vector3.up*50;
                        if(!surface.Raycast(ray,out var hit,1000)){check(false,"Room surface reachable: "+ground.Item1);continue;}
                        float feet=one.transform.position.y-body.bounds.min.y;
                        one.transform.position=hit.point+Vector3.up*(feet+.025f);Physics.SyncTransforms();foley.ResetTracking();
                        rb.constraints=RigidbodyConstraints.FreezeRotation;yield return .16d;played.Clear();
                        var start=one.transform.position;
                        Set(one,"moveInput",Vector2.right);yield return 1.2d;Set(one,"moveInput",Vector2.zero);
                        string under=string.Join(",",Physics.RaycastAll(body.bounds.center,Vector3.down,8,~0,QueryTriggerInteraction.Ignore).Select(h=>h.collider.name+"@"+h.distance.ToString("F2")));
                        File.AppendAllText(Path.Combine(output,"surfaces.txt"),$"{ground.Item1}: ray {hit.point:F2}, start {start:F2}, end {one.transform.position:F2}, contacts {under}, soft {Count(SoundCue.FootstepSoft)}, wood {Count(SoundCue.FootstepWood)}\n");
                        check(Count(ground.Item2)>0,"Walking on actual room collider "+ground.Item1+" emits "+ground.Item2);
                    }
                    played.Clear();FinishByShot(one,two);yield return .2d;
                    check(Count(SoundCue.DuelWin)==1&&Count(SoundCue.MatchWin)==0,"Ordinary capture plays its duel result once");
                }
                if(index==3||index==4)
                {
                    played.Clear();yield return index==3?2.3d:4.4d;
                    check(Count(SoundCue.BuffEnd)==1,types[index]+" defense expiry emits its release cue once");
                }
                if(index==6)
                {
                    var field=Find<HisyaFieldArea>();two.transform.position=field.transform.position+Vector3.right*25;Physics.SyncTransforms();yield return .18d;
                    played.Clear();two.transform.position=field.transform.position+Vector3.up;Physics.SyncTransforms();yield return .22d;
                    two.transform.position=field.transform.position+Vector3.right*25;Physics.SyncTransforms();yield return .22d;
                    check(played.Any(e=>e.cue==SoundCue.FieldEnter&&e.player==1)&&played.Any(e=>e.cue==SoundCue.FieldExit&&e.player==1),"Actual field entry and exit sound on the affected seat");
                }
                if(index==5)
                {
                    played.Clear();GameAudio.StopCombat();yield return .1d;
                    GameAudio.Play(SoundCue.LongGun,one);GameAudio.Play(SoundCue.LongGun,two);
                    check(played.Any(p=>p.cue==SoundCue.LongGun&&p.pan<0)&&played.Any(p=>p.cue==SoundCue.LongGun&&p.pan>0),"Both players are mixed with opposite modest pan");
                    yield return .1d;
                    var source=GameAudio.Instance.GetComponents<AudioSource>().First(s=>s.isPlaying&&s.clip.name.StartsWith("LongGun"));
                    GameSessionUI.Instance.OpenPause();int pausedAt=source.timeSamples;yield return .2d;
                    check(GameAudio.CombatPaused&&!source.isPlaying&&Math.Abs(source.timeSamples-pausedAt)<8,"Pause freezes an in-flight combat sound");
                    check(GameAudio.Play(SoundCue.UiConfirm),"Interface sound remains playable while paused");
                    check(!GameAudio.Play(SoundCue.LongGun,one),"Pause rejects new gameplay sound");
                    GameSessionUI.Instance.Resume();yield return .21d;
                    check(!GameAudio.CombatPaused,"Resume releases audio pause with gameplay gate");
                    GameAudio.SetVolume(AudioBus.Effects,0);
                    check(GameAudio.Instance.ActiveGameplayVoices==0&&!GameAudio.Play(SoundCue.LongGun,one),"Effects mute immediately stops and rejects combat audio");
                    check(GameAudio.Play(SoundCue.UiNavigate),"Effects mute preserves interface sounds");
                    GameAudio.SetVolume(AudioBus.Interface,0);
                    check(!GameAudio.Play(SoundCue.UiConfirm),"Interface mute rejects interface sound");
                    GameAudio.SetVolume(AudioBus.Effects,.85f);GameAudio.SetVolume(AudioBus.Interface,.65f);
                    yield return .2d;GameAudio.Play(SoundCue.LongGun,one);GameAudio.Play(SoundCue.UiConfirm);yield return .08d;
                    check(GameAudio.Instance.GetComponents<AudioSource>().Any(s=>s.isPlaying&&s.volume>0),"Master mute starts with live audible sources");
                    GameAudio.SetVolume(AudioBus.Master,0);yield return .15d;probe?.Begin();
                    check(!GameAudio.Play(SoundCue.ShortGun,one)&&!GameAudio.Play(SoundCue.UiBack),"Master zero rejects new effects and UI while child buses are up");yield return .3d;
                    if(capture){var muted=probe.Save(Path.Combine(output,"mute.wav"));check(muted.count>0&&muted.peak<.00001f,"Native master mute outputs silence");}
                    check(PlayerPrefs.GetFloat("FPSshogi.Audio.Master",-1)==0,"Volume choice is stored in preferences");
                    GameAudio.ResetVolumes();
                    var status=new PlayerStatus(100,10,5,one);played.Clear();status.TakeDamage(10);status.DefensePoint.Add(999);status.TakeDamage(10);
                    check(Count(SoundCue.Hit)==1&&Count(SoundCue.Block)==1,"Actual damage and blocked damage have distinct cues");
                    status.DefensePoint.Reset();status.TakeDamage(200);check(Count(SoundCue.Down)==1,"Lethal hit has a distinct down cue");
                    probe?.Begin();
                    foreach(AudioBus bus in Enum.GetValues(typeof(AudioBus)))GameAudio.SetVolume(bus,1);
                    for(int j=0;j<6;j++){GameAudio.Play(SoundCue.ShortGun,one);GameAudio.Play(SoundCue.LongGun,two);GameAudio.Play(SoundCue.TurretShot,one);GameAudio.Play(SoundCue.Block,two);yield return .08d;}
                    yield return .7d;Record("stress-two-players");
                    check(GameAudio.Instance.GetComponents<AudioSource>().Length==32,"Simultaneous combat keeps a bounded voice pool");GameAudio.ResetVolumes();
                    GameAudio.Play(SoundCue.GinShield,one);GameAudio.Play(SoundCue.KinShield,two);yield return .08d;
                    check(GameAudio.Instance.ActiveGameplayVoices>=2,"Reset test has two live owners");
                    one.ResetDuelPresentation();
                    var sources=GameAudio.Instance.GetComponents<AudioSource>();
                    check(!sources.Any(s=>s.isPlaying&&s.clip.name.StartsWith("GinShield"))&&sources.Any(s=>s.isPlaying&&s.clip.name.StartsWith("KinShield")),"Owner reset stops only the departing owner's live effects");
                }
                if(index==7)
                {
                    two.transform.position=new Vector3(500,600,500);Physics.SyncTransforms();played.Clear();yield return 5.7d;
                    check(Count(SoundCue.TurretEnd)>0,"Turret removal sounds at natural skill expiry");
                    // Exercise actual combat death and the synchronous king-capture result chain.
                    // A real ray from the first camera to the other capsule eliminates cover.
                    played.Clear();probe?.Begin();FinishByShot(one,two);yield return .12d;
                    var sources=GameAudio.Instance.GetComponents<AudioSource>();
                    check(Count(SoundCue.Down)==1&&Count(SoundCue.MatchWin)==1&&Count(SoundCue.DuelWin)==0,"King capture emits only the final result and down cue");
                    check(sources.Any(s=>s.isPlaying&&s.clip.name.StartsWith("Down"))&&sources.Any(s=>s.isPlaying&&s.clip.name.StartsWith("LongGun")),"Final shot and fall remain audible after real duel teardown");
                    yield return .7d;Record("king-capture");
                }
            }
            SceneManager.LoadScene("Title");yield return .7d;
            check(GameAudio.Instance.ActiveGameplayVoices==0,"Scene changes leave no stale combat sounds");
            check(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(l=>l.enabled)==1,"Listener stays unique after repeated scene changes");
            var all=Enum.GetValues(typeof(SoundCue)).Cast<SoundCue>();
            var missing=all.Where(c=>!coverage.ContainsKey(c)).ToArray();
            if(!output.Contains("ReviewEdges")&&!output.Contains("SurfaceProbe"))check(missing.Length==0,missing.Length==0?"All 58 cues reached":"Missing cues: "+string.Join(",",missing));
            File.WriteAllLines(Path.Combine(output,"coverage.tsv"),all.Select(c=>c+"\t"+(coverage.TryGetValue(c,out int n)?n:0)));
        }
        finally
        {
            GameAudio.Played-=Observe;
            foreach(var pair in saved){GameAudio.SetVolume(pair.Key,pair.Value);if(!hadKeys[pair.Key])PlayerPrefs.DeleteKey("FPSshogi.Audio."+pair.Key);}PlayerPrefs.Save();
            if(first!=null&&first.added)InputSystem.RemoveDevice(first);if(second!=null&&second.added)InputSystem.RemoveDevice(second);
            if(mouse!=null&&mouse.added)InputSystem.RemoveDevice(mouse);
            File.WriteAllLines(Path.Combine(output,"events.tsv"),trace);
            if(probe!=null)UnityEngine.Object.Destroy(probe);
        }
    }
    const GameStateHolder.GameState StateOne=GameStateHolder.GameState.PlayerOneTurn;
}
#endif
