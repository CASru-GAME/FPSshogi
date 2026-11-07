using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using App.Main.GameMaster;
using App.Main.Player;
using App.Main.ShogiThings;
using App.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Runs the real scenes and input system. The important regressions are lost lobby
// state on Back, a second player's menu input, premature reconnect and HUD drift.
[InitializeOnLoad]
public static class RoomUiRegression
{
    const string Trigger=".utmp/room-ui/run.txt";
    static IEnumerator routine;
    static string dir;
    static double next;
    static Gamepad one,two;
    static Keyboard keyboard;
    static Mouse mouse;
    static readonly List<string> results=new(),errors=new();
    static readonly BindingFlags All=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static EditorWindow gameView;
    static int oldSize=-1;
    static readonly Dictionary<string, float?> savedSettings = new();
    static bool hpNumbers;
    static bool expectedLoadFailure;
    static GameSessionUI Session=>GameSessionUI.Instance;
    static RoomUiRegression()
    {
        EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged+=state=>{
            if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("RoomUiChecks",false)){
                SessionState.SetBool("RoomUiChecks",false);dir="VisualReview/RoomUI/"+SessionState.GetString("RoomUiChecksDir","After")+"/";
                Directory.CreateDirectory(dir);results.Clear();errors.Clear();Application.logMessageReceived+=Log;routine=Run();next=EditorApplication.timeSinceStartup+1;
            }
            if(state==PlayModeStateChange.ExitingPlayMode&&routine!=null)Finish();
        };
    }
    static void Tick()
    {
        if(EditorApplication.isCompiling)return;
        if(File.Exists(Trigger)&&!EditorApplication.isPlayingOrWillChangePlaymode&&routine==null){
            SessionState.SetString("RoomUiChecksDir",File.ReadAllText(Trigger).Trim());File.Delete(Trigger);SessionState.SetBool("RoomUiChecks",true);
            EditorSceneManager.OpenScene("Assets/App/Scenes/Title.unity");EditorApplication.isPlaying=true;
        }
        if(routine==null||EditorApplication.timeSinceStartup<next)return;
        try{if(!routine.MoveNext()){Finish();return;}next=EditorApplication.timeSinceStartup+(routine.Current is double delay?delay:.06);}
        catch(Exception e){errors.Add(e.ToString());Check("Room UI suite completes",false);Finish();}
    }
    static void Log(string message,string trace,LogType type){if(expectedLoadFailure&&message.Contains("__RoomUI_MissingScene__"))return;if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message+"\n"+trace);}
    static void Check(string name,bool pass){results.Add((pass?"PASS ":"FAIL ")+name);File.WriteAllLines(dir+"results.txt",results);}
    static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindFirstObjectByType<T>();
    static bool Visible(string name)=>GameObject.Find(name)!=null;
    static Slider FovSlider(int slot) => GameObject.Find("Field of view slider " + slot)?.GetComponent<Slider>();
    static string FovLabel(int slot) => GameObject.Find("Field of view slider " + slot + " value")?.GetComponent<Text>().text;
    static void CheckGuideForm(PieceType type, bool promoted)
    {
        Canvas.ForceUpdateCanvases();
        var description = GameObject.Find("Skill description")?.GetComponent<Text>();
        string name = PiecePresentation.Name(type, promoted);
        Check("Guide shows the weapon and skill effect for " + name,
            GameObject.Find("Guide piece name").GetComponent<Text>().text == name &&
            GameObject.Find("Weapon name").GetComponent<Text>().text == PiecePresentation.Weapon(type) &&
            description != null && !string.IsNullOrWhiteSpace(description.text) &&
            description.text == PiecePresentation.SkillDescription(type) &&
            description.preferredHeight <= description.rectTransform.rect.height + 1 && !Visible("Matchup tip"));
    }
    static Rect ScreenBounds(RectTransform rect)
    {
        var corners=new Vector3[4];rect.GetWorldCorners(corners);
        return new Rect(corners[0].x,corners[0].y,corners[2].x-corners[0].x,corners[2].y-corners[0].y);
    }
    static void Shot(string name){ScreenCapture.CaptureScreenshot(dir+name+".png");Layout(name);}
    static void Layout(string name)
    {
        Canvas.ForceUpdateCanvases();var texts=Session.GetComponentsInChildren<Text>().Where(t=>t.isActiveAndEnabled);
        var clipped=texts.Where(t=>!t.resizeTextForBestFit&&t.preferredHeight>t.rectTransform.rect.height+1).Select(t=>$"{t.name}: needs {t.preferredHeight}, has {t.rectTransform.rect.height}").ToArray();
        File.WriteAllLines(dir+name+"-layout.txt",clipped);Check(name+" text fits",clipped.Length==0);
        var corners=new Vector3[4];bool inside=true;
        foreach(var b in Session.GetComponentsInChildren<Selectable>().Where(b=>b.isActiveAndEnabled)){
            b.GetComponent<RectTransform>().GetWorldCorners(corners);if(corners.Any(c=>c.x<-.5||c.y<-.5||c.x>Screen.width+.5||c.y>Screen.height+.5))inside=false;
        }
        Check(name+" controls fit",inside);
    }
    static void Resolution(int width,int height)
    {
        var asm=typeof(Editor).Assembly;var viewType=asm.GetType("UnityEditor.GameView");gameView=EditorWindow.GetWindow(viewType);
        var selected=viewType.GetProperty("selectedSizeIndex",All);if(oldSize<0)oldSize=(int)selected.GetValue(gameView);
        var sizesType=asm.GetType("UnityEditor.GameViewSizes");var single=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var sizes=single.GetProperty("instance",BindingFlags.Public|BindingFlags.Static|BindingFlags.FlattenHierarchy).GetValue(null);
        var groupType=asm.GetType("UnityEditor.GameViewSizeGroupType");var group=sizesType.GetMethod("GetGroup",All).Invoke(sizes,new[]{Enum.Parse(groupType,"Standalone")});
        var sizeType=asm.GetType("UnityEditor.GameViewSize");var kind=asm.GetType("UnityEditor.GameViewSizeType");
        var size=Activator.CreateInstance(sizeType,All,null,new[]{Enum.Parse(kind,"FixedResolution"),(object)width,height,"Room UI "+width+"x"+height},null);
        group.GetType().GetMethod("AddCustomSize",All).Invoke(group,new[]{size});int index=(int)group.GetType().GetMethod("GetTotalCount",All).Invoke(group,null)-1;
        selected.SetValue(gameView,index);gameView.Repaint();
    }
    static IEnumerator Tap(Gamepad pad,GamepadButton button)
    {InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(button));yield return .14d;InputSystem.QueueStateEvent(pad,new GamepadState());yield return .22d;}
    static IEnumerator Run()
    {
        one=InputSystem.AddDevice<Gamepad>("Room First");two=InputSystem.AddDevice<Gamepad>("Room Second");keyboard=InputSystem.AddDevice<Keyboard>("Room Keyboard");mouse=InputSystem.AddDevice<Mouse>("Room Mouse");
        foreach (string key in new[] { "FPSshogi.Sensitivity.0", "FPSshogi.Sensitivity.1", "FPSshogi.GamepadZoomSensitivity.0",
            "FPSshogi.GamepadZoomSensitivity.1", "FPSshogi.FieldOfView.0", "FPSshogi.FieldOfView.1",
            "FPSshogi.Audio.Master", "FPSshogi.Audio.Effects", "FPSshogi.Audio.Interface" })
            savedSettings[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetFloat(key) : null;
        PlayerPrefs.DeleteKey("FPSshogi.FieldOfView.0");PlayerPrefs.DeleteKey("FPSshogi.FieldOfView.1");
        hpNumbers=Session.ShowHpNumbers;Session.ShowHpNumbers=false;
        Resolution(1920,1080);yield return .5d;Shot("01-title");yield return .15d;yield return .2d;
        Check("Title stages all forty real pieces",GameObject.Find("Title board pieces")?.transform.childCount==40);
        var shade=GameObject.Find("Title shade").GetComponent<UiSymbol>();Check("Title shade has rendered geometry",shade.canvasRenderer!=null&&shade.canvasRenderer.GetMesh()?.vertexCount>0);
        var titleStart=GameObject.Find("Start button").GetComponent<RectTransform>();Check("Title action is on left",titleStart.position.x<Screen.width*.3f);
        Session.OpenSettings();yield return .25d;
        var titleOne = FovSlider(0); var titleTwo = FovSlider(1);
        Check("Title offers each seat the original 60-degree duel FOV", FovLabel(0) == "60°" && FovLabel(1) == "60°");
        if (titleOne != null && titleTwo != null)
        {
            Session.Focus(titleOne);yield return .2d;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.RightArrow));yield return .12d;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return .15d;
            var bounds = ScreenBounds(titleTwo.GetComponent<RectTransform>());
            foreach (var x in new[]{bounds.xMin + 1, bounds.xMax - 1})
            {
                var pointer = new MouseState{position = new Vector2(x,bounds.center.y)};
                InputSystem.QueueStateEvent(mouse,pointer);yield return .1d;
                InputSystem.QueueStateEvent(mouse,pointer.WithButton(MouseButton.Left));yield return .12d;
                InputSystem.QueueStateEvent(mouse,pointer.WithButton(MouseButton.Left,false));yield return .12d;
                Check("Mouse selects the end of the FOV track at " + x, FovLabel(1) == (x < bounds.center.x ? "50°" : "140°"));
            }
            Check("Keyboard changes only the first FOV by one degree", FovLabel(0) == "61°" && FovLabel(1) == "140°");
        }
        Session.Back();yield return .2d;
        foreach(var resolution in new[]{new Vector2Int(2560,1080),new Vector2Int(1280,1024)}){
            Resolution(resolution.x,resolution.y);yield return .3d;Shot("01-title-"+resolution.x+"x"+resolution.y);yield return .15d;
        }
        Resolution(1920,1080);yield return .3d;
        Session.ConfirmQuit();yield return .2d;Shot("02-quit");yield return .15d;Session.Back();yield return .3d;
        var ownerTest=Tap(one,GamepadButton.Select);while(ownerTest.MoveNext())yield return ownerTest.Current;
        InputSystem.DisableDevice(one);yield return .3d;ownerTest=Tap(two,GamepadButton.East);while(ownerTest.MoveNext())yield return ownerTest.Current;
        Check("Title help remains usable when its owner disconnects",Visible("Start button"));InputSystem.EnableDevice(one);yield return .2d;
        expectedLoadFailure=true;typeof(GameSessionUI).GetMethod("LoadScene",All).Invoke(Session,new object[]{"__RoomUI_MissingScene__"});yield return .35d;expectedLoadFailure=false;
        Check("Failed load has actionable return",Visible("Load return button")&&EventSystem.current.sendNavigationEvents);Shot("02-load-error");yield return .15d;
        Session.OpenHelp();yield return .2d;Session.Back();yield return .2d;Check("Error survives help and back",Visible("Load return button"));
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Enter));yield return .15d;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return .7d;Check("Keyboard exits failed load",Session.IsTitle&&Visible("Start button"));
        Session.StartMatch();yield return .4d;Shot("03-selection");yield return .15d;
        var tap=Tap(one,GamepadButton.South);while(tap.MoveNext())yield return tap.Current;
        tap=Tap(two,GamepadButton.South);while(tap.MoveNext())yield return tap.Current;
        tap=Tap(one,GamepadButton.South);while(tap.MoveNext())yield return tap.Current;
        tap=Tap(one,GamepadButton.Select);while(tap.MoveNext())yield return tap.Current;
        Check("View opens help from selection",Visible("Basics tab"));Shot("04-rules");yield return .15d;
        Session.SetHelpTab(true);yield return .25d;Shot("05-controls");yield return .15d;
        Session.OpenPieces();yield return .3d;Shot("06-pieces");yield return .15d;
        var portrait=GameObject.Find("Guide portrait").GetComponent<RawImage>().texture as Texture2D;
        Check("Piece guide shows a rendered model",portrait!=null&&portrait.GetPixels32().Count(p=>p.a>20)>500);
        GameObject.Find("Guide promoted").GetComponent<Button>().onClick.Invoke();yield return .25d;Shot("07-promoted");yield return .15d;
        Check("Promotion changes model and movement",Session.GuidePromoted&&GameObject.Find("Guide piece name").GetComponent<Text>().text=="龍王");
        foreach (PieceType type in Enum.GetValues(typeof(PieceType)))
        {
            if (type == PieceType.None) continue;
            GameObject.Find("Guide " + type).GetComponent<Button>().onClick.Invoke();
            CheckGuideForm(type, false);
            if (!PiecePresentation.CanPromote(type)) continue;
            GameObject.Find("Guide promoted").GetComponent<Button>().onClick.Invoke();
            CheckGuideForm(type, true);
        }
        Session.Back();yield return .25d;Check("Guide returns to caller controls tab",Visible("Controls sheet 0"));
        Session.Back();yield return .4d;Check("Help returns to lobby with assignments and preparation",Visible("Player selection panel")&&Session.Devices.PlayerFor(one)==0&&Session.Devices.PlayerFor(two)==1&&Session.Devices.IsPrepared(0)&&!Session.Devices.IsPrepared(1));
        tap=Tap(two,GamepadButton.South);while(tap.MoveNext())yield return tap.Current;yield return 1.5d;
        Check("Both prepared players load a match",SceneManager.GetActiveScene().name=="Main");Shot("08-board");yield return .15d;
        foreach(var resolution in new[]{new Vector2Int(2560,1080),new Vector2Int(1280,1024)}){
            Resolution(resolution.x,resolution.y);yield return .3d;Shot("08-board-"+resolution.x+"x"+resolution.y);yield return .15d;
            var footerBounds=ScreenBounds(GameObject.Find("Board controls").GetComponent<RectTransform>());
            Check("Board footer stays clear of playable squares at "+resolution,!Session.GetComponentsInChildren<Button>().Where(b=>b.name.StartsWith("Square ")).Any(b=>footerBounds.Overlaps(ScreenBounds(b.GetComponent<RectTransform>()))));
            Session.OpenPause();yield return .2d;Shot("09-pause-"+resolution.x+"x"+resolution.y);yield return .15d;Session.Resume();yield return .3d;
        }
        Resolution(1920,1080);yield return .3d;
        tap=Tap(one,GamepadButton.Start);while(tap.MoveNext())yield return tap.Current;
        Check("Opening player owns pause",Visible("Resume button")&&GameObject.Find("Menu owner").GetComponent<Text>().text.Contains("1P"));Shot("09-pause");yield return .15d;
        Check("Pause hides board HUD while preserving the board scene",!Visible("Board HUD")&&Find<ShogiBoard>()!=null);
        tap=Tap(two,GamepadButton.East);while(tap.MoveNext())yield return tap.Current;Check("Other player cannot dismiss pause",Visible("Resume button")&&Time.timeScale==0);
        PlayerPrefs.SetFloat("FPSshogi.Sensitivity.0", 3f);
        PlayerPrefs.DeleteKey("FPSshogi.GamepadZoomSensitivity.0");
        Check("An existing normal sensitivity of 3.0 starts zoom at 1.2", Mathf.Approximately(Session.ZoomSensitivity(0), 1.2f));
        Session.SetSensitivity(0,2);Session.SetSensitivity(1,2);
        Check("Changing normal look preserves the initial zoom value", Mathf.Approximately(Session.ZoomSensitivity(0), 1.2f));
        Session.SetZoomSensitivity(0,.8f);Session.SetZoomSensitivity(1,.8f);
        Session.OpenSettings();yield return .25d;Shot("10-settings");yield return .15d;
        float beforeSlider=Session.Sensitivity(0);tap=Tap(one,GamepadButton.DpadRight);while(tap.MoveNext())yield return tap.Current;
        Check("D-pad adjusts normal look by one tenth",Mathf.Approximately(Session.Sensitivity(0), beforeSlider + .1f));
        tap=Tap(one,GamepadButton.DpadDown);while(tap.MoveNext())yield return tap.Current;
        Check("Controller focuses first-player zoom",EventSystem.current.currentSelectedGameObject?.name=="Zoom sensitivity slider 0");
        tap=Tap(one,GamepadButton.DpadRight);while(tap.MoveNext())yield return tap.Current;
        Check("1P zoom changes by one tenth independently",Mathf.Approximately(Session.ZoomSensitivity(0),.9f) && Mathf.Approximately(Session.ZoomSensitivity(1),.8f) && Mathf.Approximately(Session.Sensitivity(0),2.1f));
        tap=Tap(one,GamepadButton.DpadDown);while(tap.MoveNext())yield return tap.Current;
        Check("Controller navigates to second-player sensitivity",EventSystem.current.currentSelectedGameObject?.name=="Sensitivity slider 1");
        beforeSlider=Session.Sensitivity(1);tap=Tap(one,GamepadButton.DpadRight);while(tap.MoveNext())yield return tap.Current;
        Check("Controller adjusts focused second-player sensitivity",Mathf.Approximately(Session.Sensitivity(1), beforeSlider + .1f));
        tap=Tap(one,GamepadButton.DpadDown);while(tap.MoveNext())yield return tap.Current;
        Check("Controller focuses second-player zoom",EventSystem.current.currentSelectedGameObject?.name=="Zoom sensitivity slider 1");
        tap=Tap(one,GamepadButton.DpadLeft);while(tap.MoveNext())yield return tap.Current;
        var players = Find<PlayerManager>();
        Check("Four independent settings reach the paired players", Mathf.Approximately(players.PlayerOne.GetComponent<Player>().GamepadZoomSensitivity,.9f) &&
            Mathf.Approximately(players.PlayerTwo.GetComponent<Player>().GamepadZoomSensitivity,.7f) && Mathf.Approximately(players.PlayerOne.GetComponent<Player>().LookSensitivity,2.1f) && Mathf.Approximately(players.PlayerTwo.GetComponent<Player>().LookSensitivity,2.1f));
        var firstView = players.PlayerOne.GetComponentInChildren<Camera>();
        var secondView = players.PlayerTwo.GetComponentInChildren<Camera>();
        float boardFov = Camera.main.fieldOfView;
        Check("Both title FOV settings reach the new match", firstView.fieldOfView == 61 && secondView.fieldOfView == 140);
        if (FovSlider(0) != null && FovSlider(1) != null)
        {
            tap=Tap(one,GamepadButton.DpadDown);while(tap.MoveNext())yield return tap.Current;
            tap=Tap(one,GamepadButton.DpadRight);while(tap.MoveNext())yield return tap.Current;
            tap=Tap(one,GamepadButton.DpadDown);while(tap.MoveNext())yield return tap.Current;
            tap=Tap(one,GamepadButton.DpadLeft);while(tap.MoveNext())yield return tap.Current;
            Check("D-pad adjusts both seat FOVs independently and leaves the board camera alone",
                firstView.fieldOfView == 62 && secondView.fieldOfView == 139 && Camera.main.fieldOfView == boardFov);
        }
        Session.Back();yield return .2d;Session.OpenSettings();yield return .2d;
        Check("Both zoom values survive reopening settings", GameObject.Find("Zoom sensitivity slider 0 value").GetComponent<Text>().text=="0.9" && GameObject.Find("Zoom sensitivity slider 1 value").GetComponent<Text>().text=="0.7");
        Check("Pause retains both FOV values when reopened", FovLabel(0) == "62°" && FovLabel(1) == "139°");
        Session.SetSensitivity(0,4);Session.ShowHpNumbers=true;GameObject.Find("Reset settings button").GetComponent<Button>().onClick.Invoke();yield return .2d;
        Check("Nested settings keep gameplay HUD hidden",!Visible("Board HUD")&&!Visible("Duel HUD"));
        Check("Reset returns both duel cameras and labels to 60 degrees", FovLabel(0) == "60°" && FovLabel(1) == "60°" &&
            firstView.fieldOfView == 60 && secondView.fieldOfView == 60);
        Check("Reset restores both normal values to 2.0, zoom to 0.8 and HP preference",Session.Sensitivity(0)==2&&Session.Sensitivity(1)==2&&Mathf.Approximately(Session.ZoomSensitivity(0),.8f)&&Mathf.Approximately(Session.ZoomSensitivity(1),.8f)&&!Session.ShowHpNumbers);
        Session.Back();yield return .25d;Check("Settings returns to pause",Visible("Resume button"));
        Session.ConfirmReturn();yield return .25d;Shot("11-return-confirm");yield return .15d;Session.Back();yield return .25d;Check("Cancelled return restores pause",Visible("Resume button"));
        Session.OpenHelp();yield return .25d;InputSystem.DisableDevice(two);yield return .4d;
        Check("Disconnect in nested menu blocks immediately",Visible("Connection message")&&Time.timeScale==0);Shot("12-disconnected");yield return .15d;
        InputSystem.EnableDevice(two);yield return .4d;Session.Resume();yield return .2d;
        Check("Connection alone cannot resume",Visible("Connection message")&&Time.timeScale==0&&!Session.Devices.IsPrepared(0)&&!Session.Devices.IsPrepared(1));
        Session.ConfirmReturn();yield return .2d;Session.Back();yield return .4d;Check("Cancel exit returns to reconnect",Visible("Connection message"));
        InputSystem.DisableDevice(two);yield return .4d;InputSystem.EnableDevice(two);InputSystem.QueueStateEvent(two,new GamepadState().WithButton(GamepadButton.South));yield return .35d;
        Check("Held button on reconnect does not confirm",!Session.Devices.IsPrepared(1));InputSystem.QueueStateEvent(two,new GamepadState());yield return .35d;
        tap=Tap(one,GamepadButton.South);while(tap.MoveNext())yield return tap.Current;Check("Reconnect waits for second confirmation",Visible("Connection message")&&Session.Devices.IsPrepared(0));Shot("13-reconnected");yield return .15d;
        tap=Tap(two,GamepadButton.South);while(tap.MoveNext())yield return tap.Current;yield return .3d;Check("Both confirmations resume existing board",!GameSessionUI.BlocksGameplay&&Visible("Turn label"));
        var board=Find<ShogiBoard>();board.MovePiece(0,2,0,3,PlayerType.PlayerOne);board.MovePiece(0,6,0,5,PlayerType.PlayerTwo);board.MovePiece(0,3,0,4,PlayerType.PlayerOne);board.MovePiece(0,5,0,4,PlayerType.PlayerTwo);yield return .3d;Shot("14-countdown");yield return .15d;
        Check("Countdown blocks gameplay",GameSessionUI.BlocksGameplay&&Visible("Countdown"));yield return 3.5d;
        Shot("15-duel");yield return .15d;
        var p1=Find<PlayerManager>().PlayerOne.GetComponent<Player>();
        var p2=p1.Opponent;
        Check("Both spears show their brackets at the opening of an adjacent pawn capture",
            Visible("P1 melee reach") && Visible("P2 melee reach"));
        p1.FieldOfView=Player.DefaultFieldOfView;p2.FieldOfView=140;yield return .15d;
        Check("140 degree split view preserves each spear's physical reach",
            firstView.fieldOfView == 60 && secondView.fieldOfView == 140 && Visible("P1 melee reach") && Visible("P2 melee reach"));
        Shot("15-duel-fov-60-140");yield return .15d;
        p1.FieldOfView=Session.FieldOfView(0);p2.FieldOfView=Session.FieldOfView(1);
        var otherBody=p2.GetComponent<Rigidbody>();
        Vector3 start=otherBody.position;
        otherBody.position=start+(start-p1.transform.position).normalized*8f;
        Physics.SyncTransforms();yield return .2d;
        Check("Both reticles return to a plain crosshair beyond spear reach",!Visible("P1 melee reach")&&!Visible("P2 melee reach"));
        Shot("15-duel-out-of-reach");yield return .15d;
        otherBody.position=start;Physics.SyncTransforms();yield return .2d;
        var damageCamera=p1.GetComponentInChildren<Camera>();
        var healthBar=GameObject.Find("P1 health bar").GetComponent<RectTransform>();
        var hpBefore=p1.playerStatus.Hp.Current;
        float widthBefore=healthBar.sizeDelta.x;
        p1.playerStatus.TakeDamage(20,damageCamera.transform.position+damageCamera.transform.right*5);yield return .12d;
        Check("Damage feedback points toward the actual source",GameObject.Find("P1 damage edge 1").GetComponent<Image>().color.a>0&&GameObject.Find("P1 damage edge 0").GetComponent<Image>().color.a==0);
        float expectedWidth=widthBefore*(hpBefore-20)/hpBefore;
        Check("HP gauge follows damage without numeric text",widthBefore>0&&p1.playerStatus.Hp.Current==hpBefore-20&&
            Mathf.Abs(healthBar.sizeDelta.x-expectedWidth)<1&&!Visible("P1 HP"));Shot("16-damage");yield return .15d;
        InputSystem.QueueStateEvent(one,new GamepadState{rightTrigger=1});yield return .14d;InputSystem.QueueStateEvent(one,new GamepadState());yield return .12d;
        Check("Cooldown arc reflects the actual attack",p1.PrimaryCooldown>0&&GameObject.Find("P1 cooldown 0").GetComponent<UiSymbol>().Fill>0);Shot("17-cooldown");yield return .15d;
        var secondCamera = Find<PlayerManager>().PlayerTwo.GetComponentInChildren<Camera>();
        Rect leftView = damageCamera.rect, rightView = secondCamera.rect;
        Check("Active duel shows its center divider", Visible("Screen divider"));
        Session.OpenPause();
        Check("Opening a duel menu immediately hides its center divider", !Visible("Screen divider"));
        yield return .2d;Check("Pause hides FPS HUD and crosshairs",!Visible("Duel HUD")&&!Visible("P1 Crosshair"));float remaining=p1.PrimaryCooldown;yield return .3d;Check("Pause freezes cooldown",Mathf.Approximately(p1.PrimaryCooldown,remaining));
        GameObject.Find("Settings button").GetComponent<Button>().onClick.Invoke();yield return .25d;
        Check("Duel settings have one continuous full-screen room behind the controls", Visible("Zoom sensitivity slider 0") &&
            Camera.main != null && Camera.main.rect == new Rect(0,0,1,1) && !damageCamera.enabled && !secondCamera.enabled && !Visible("Screen divider"));
        Shot("17-duel-settings");yield return .25d;
        Session.Back();yield return .2d;
        Check("Returning from duel settings keeps the pause menu on a single background", Visible("Resume button") && !damageCamera.enabled && !secondCamera.enabled);
        InputSystem.QueueStateEvent(one,new GamepadState{leftStick=Vector2.up});yield return .1d;Session.Resume();yield return .3d;
        Check("Held movement remains blocked after resume",GameSessionUI.BlocksGameplay);InputSystem.QueueStateEvent(one,new GamepadState());yield return .4d;Check("Neutral movement releases gameplay gate",!GameSessionUI.BlocksGameplay);
        Check("Resume restores FPS HUD and both crosshairs",Visible("Duel HUD")&&Visible("P1 Crosshair")&&Visible("P2 Crosshair"));
        Check("Resume restores both original viewports and their divider", damageCamera.enabled && secondCamera.enabled && damageCamera.rect == leftView && secondCamera.rect == rightView && Visible("Screen divider"));
        foreach(var resolution in new[]{new Vector2Int(1280,720),new Vector2Int(2560,1080),new Vector2Int(1280,1024)}){
            Resolution(resolution.x,resolution.y);yield return .4d;Canvas.ForceUpdateCanvases();
            for(int p=0;p<2;p++){var cross=GameObject.Find($"P{p+1} Crosshair").GetComponent<RectTransform>();Check($"Crosshair {p+1} centered at {resolution}",Mathf.Abs(cross.position.x-Screen.width*(p==0?.25f:.75f))<2&&Mathf.Abs(cross.position.y-Screen.height*.5f)<2);}
            Shot("18-duel-"+resolution.x+"x"+resolution.y);yield return .15d;
        }
        Resolution(1920,1080);yield return .3d;p1.playerStatus.Hp.Subtract(100000);yield return .3d;Shot("19-duel-result");yield return .15d;yield return 2.4d;
        SceneManager.LoadScene("Main");yield return .7d;board=Find<ShogiBoard>();board.GetBoard()[0,4]=board.GetBoard()[4,0];board.GetBoard()[4,0]=null;board.GetBoard()[0,5]=board.GetBoard()[4,8];board.GetBoard()[4,8]=null;board.MovePiece(0,4,0,5,PlayerType.PlayerOne);yield return 3.5d;
        Find<PlayerManager>().PlayerTwo.GetComponent<Player>().playerStatus.Hp.Subtract(100000);yield return .4d;Shot("20-result");yield return .15d;Check("King defeat offers rematch",Visible("Rematch button"));
        Session.ReturnTitle();yield return .8d;Check("Return loads usable title",Session.IsTitle&&Visible("Start button"));
        Resolution(1280,720);yield return .3d;Shot("21-title-720");yield return .15d;Session.OpenSettings();yield return .2d;Shot("22-settings-720");yield return .15d;Session.Back();Session.StartMatch();yield return .3d;Shot("23-selection-720");yield return .15d;
        Check("No runtime errors",errors.Count==0);
    }
    static void Finish()
    {
        routine=null;Application.logMessageReceived-=Log;File.WriteAllLines(dir+"errors.txt",errors);
        File.WriteAllText(dir+"complete.txt",$"{DateTime.Now:O}\n{results.Count(x=>x.StartsWith("PASS"))} PASS / {results.Count(x=>x.StartsWith("FAIL"))} FAIL");
        foreach (var pair in savedSettings)
        {
            if (pair.Value.HasValue) PlayerPrefs.SetFloat(pair.Key, pair.Value.Value);
            else PlayerPrefs.DeleteKey(pair.Key);
        }
        if(Session!=null)Session.ShowHpNumbers=hpNumbers;
        PlayerPrefs.Save();
        foreach(var d in new InputDevice[]{one,two,keyboard,mouse})if(d!=null&&d.added)InputSystem.RemoveDevice(d);
        if(gameView!=null&&oldSize>=0)gameView.GetType().GetProperty("selectedSizeIndex",All).SetValue(gameView,oldSize);oldSize=-1;
        Time.timeScale=1;EditorApplication.isPlaying=false;
    }
}
