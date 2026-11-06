#if FPS_SELECTION_SMOKE
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using App.Main.GameMaster;
using App.Main.Player;
using App.Main.ShogiThings;
using App.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

public sealed class PlayerSelectionSmoke : MonoBehaviour
{
    readonly List<string> results=new(),errors=new();
    string output;
    Gamepad first,second,extra;
    Keyboard keyboard;
    Mouse mouse;
    bool finished;
    float began;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Launch()=>DontDestroyOnLoad(new GameObject("PlayerSelectionSmoke").AddComponent<PlayerSelectionSmoke>());
    void Awake()
    {
        output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../SelectionReview/PlayerSmoke"));Directory.CreateDirectory(output);
        if(File.Exists(Path.Combine(output,"complete.txt")))File.Delete(Path.Combine(output,"complete.txt"));
        began=Time.realtimeSinceStartup;Application.logMessageReceived+=Log;
    }
    void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message+"\n"+trace);}
    void Update(){if(!finished&&Time.realtimeSinceStartup-began>90){Check(false,"Native selection completes before timeout");Finish();}}
    static T Find<T>()where T:UnityEngine.Object=>UnityEngine.Object.FindFirstObjectByType<T>();
    static GameSessionUI Session=>GameSessionUI.Instance;
    static bool Lobby=>GameObject.Find("Player selection panel")!=null;
    void Check(bool pass,string name){results.Add((pass?"PASS ":"FAIL ")+name);File.WriteAllLines(Path.Combine(output,"results.txt"),results);}
    void Shot(string name)=>ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));
    IEnumerator Tap(InputDevice device)
    {
        if(device is Keyboard)
        {
            InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(20+Time.frameCount%100,20)});
            yield return new WaitForSecondsRealtime(.05f);
            File.AppendAllText(Path.Combine(output,"device-identities.txt"),$"Before keyboard: fixture mouse={mouse.deviceId}, current mouse={Mouse.current?.deviceId}\n");
        }
        var tap=SelectionTestInput.Tap(device);while(tap.MoveNext())yield return new WaitForSecondsRealtime((float)(double)tap.Current);
    }
    static IEnumerator OpenLobby()
    {SceneManager.LoadScene("Title");yield return new WaitForSecondsRealtime(.6f);Session.StartMatch();yield return new WaitForSecondsRealtime(.4f);}
    IEnumerator CheckLook(InputDevice device,int slot,string label)
    {
        var manager=Find<PlayerManager>();var owner=(slot==0?manager.PlayerOne:manager.PlayerTwo).GetComponent<Player>();var other=(slot==0?manager.PlayerTwo:manager.PlayerOne).GetComponent<Player>();
        float ownerYaw=owner.transform.eulerAngles.y,otherYaw=other.transform.eulerAngles.y;
        if(device is Gamepad pad)InputSystem.QueueStateEvent(pad,new GamepadState{rightStick=new Vector2(.5f,0)});
        else InputSystem.QueueStateEvent(mouse,new MouseState{delta=new Vector2(24,0)});
        yield return new WaitForSecondsRealtime(.2f);
        if(device is Gamepad released)InputSystem.QueueStateEvent(released,new GamepadState());
        else InputSystem.QueueStateEvent(mouse,new MouseState());
        Check(Mathf.Abs(Mathf.DeltaAngle(ownerYaw,owner.transform.eulerAngles.y))>.1f&&Mathf.Abs(Mathf.DeltaAngle(otherYaw,other.transform.eulerAngles.y))<.01f,label+" moves only its selected player's camera");
        yield return new WaitForSecondsRealtime(.15f);
    }
    void BeginDuel()
    {
        var board=Find<ShogiBoard>();var grid=board.GetBoard();Array.Clear(grid,0,grid.Length);
        grid[4,4]=new Hisya(PlayerType.PlayerOne);grid[4,5]=new Kyosya(PlayerType.PlayerTwo);
        Check(board.MovePiece(4,4,4,5,PlayerType.PlayerOne)==ShogiBoard.MoveResult.DuelMove,"Selected players can enter a real duel");
    }
    IEnumerator Start()
    {
        File.WriteAllText(Path.Combine(output,"device-identities.txt"),$"Screen: {Screen.width}x{Screen.height}\n");
        first=InputSystem.AddDevice<Gamepad>("Native First");second=InputSystem.AddDevice<Gamepad>("Native Second");extra=InputSystem.AddDevice<Gamepad>("Native Unjoined");
        keyboard=InputSystem.AddDevice<Keyboard>("Native Keyboard");mouse=InputSystem.AddDevice<Mouse>("Native Mouse");
        yield return OpenLobby();
        Check(Lobby&&!Session.Devices.Ready,"Native title opens an unassigned selection screen");
        yield return Tap(second);yield return Tap(first);Shot("two-pads");yield return new WaitForSecondsRealtime(.2f);
        Check(Session.Devices.Ready&&!Session.Devices.AllPrepared,"Native two controllers join without auto preparation");
        yield return Tap(second);yield return new WaitForSecondsRealtime(1.1f);
        Check(Lobby&&Session.Devices.IsPrepared(0)&&!Session.Devices.IsPrepared(1),"Native first player cannot approve the second");
        yield return Tap(extra);
        Check(Lobby&&!Session.Devices.IsPrepared(1),"Native third controller cannot approve another slot");
        yield return Tap(first);Shot("two-ready");yield return new WaitForSecondsRealtime(1.5f);
        var manager=Find<PlayerManager>();
        Check(!Lobby&&manager.PlayerOne.GetComponent<PlayerInput>().devices.Contains(second)&&manager.PlayerTwo.GetComponent<PlayerInput>().devices.Contains(first),"Native joining order determines 1P and 2P");
        BeginDuel();yield return new WaitForSecondsRealtime(3.6f);
        yield return CheckLook(second,0,"Native 1P controller");yield return CheckLook(first,1,"Native 2P controller");Shot("two-pad-duel");yield return new WaitForSecondsRealtime(.2f);
        foreach(bool keyboardFirst in new[]{true,false})
        {
            yield return OpenLobby();
            yield return Tap(keyboardFirst?(InputDevice)keyboard:second);yield return Tap(keyboardFirst?(InputDevice)second:keyboard);
            Shot(keyboardFirst?"keyboard-first":"controller-first");yield return new WaitForSecondsRealtime(.2f);
            Check(Session.Devices.IsGamepad(0)!=keyboardFirst&&Session.Devices.IsGamepad(1)==keyboardFirst,"Native mixed selection honors the chosen devices: keyboardFirst="+keyboardFirst);
            yield return Tap(keyboard);yield return new WaitForSecondsRealtime(1.1f);
            Check(Lobby&&!Session.Devices.AllPrepared,"Native mixed match waits for both confirmations");
            yield return Tap(second);yield return new WaitForSecondsRealtime(1.5f);
            manager=Find<PlayerManager>();var kbPlayer=(keyboardFirst?manager.PlayerOne:manager.PlayerTwo).GetComponent<PlayerInput>();var padPlayer=(keyboardFirst?manager.PlayerTwo:manager.PlayerOne).GetComponent<PlayerInput>();
            File.AppendAllText(Path.Combine(output,"device-identities.txt"),$"keyboardFirst={keyboardFirst}: paired={string.Join(",",kbPlayer.devices.Select(d=>d.layout+"#"+d.deviceId))}; expected={keyboard.deviceId},{mouse.deviceId}\n");
            Check(kbPlayer.devices.Contains(keyboard)&&kbPlayer.devices.Contains(mouse)&&padPlayer.devices.Count==1&&padPlayer.devices.Contains(second),"Native mixed match keeps keyboard/mouse and controller separate");
            BeginDuel();yield return new WaitForSecondsRealtime(3.6f);
            yield return CheckLook(keyboard,keyboardFirst?0:1,"Native mixed mouse");yield return CheckLook(second,keyboardFirst?1:0,"Native mixed controller");
        }
        Session.StartMatch();yield return new WaitForSecondsRealtime(.4f);
        Check(Lobby&&Session.Devices.Ready&&!Session.Devices.AllPrepared,"Native rematch retains devices but requires fresh preparation");
        yield return Tap(keyboard);yield return Tap(second);yield return new WaitForSecondsRealtime(1.5f);
        Check(!Lobby&&Find<GameStateHolder>().CurrentState==GameStateHolder.GameState.PlayerOneTurn,"Native rematch starts only after both players prepare");
        SceneManager.LoadScene("Title");yield return new WaitForSecondsRealtime(.6f);Finish();
    }
    void Finish()
    {
        if(finished)return;finished=true;Check(errors.Count==0,"No native runtime errors");
        File.WriteAllLines(Path.Combine(output,"errors.txt"),errors);File.WriteAllText(Path.Combine(output,"complete.txt"),DateTime.Now.ToString("O"));
        Application.logMessageReceived-=Log;foreach(var device in new InputDevice[]{first,second,extra,keyboard,mouse})if(device!=null&&device.added)InputSystem.RemoveDevice(device);
        Application.Quit(results.Any(row=>row.StartsWith("FAIL"))?1:0);
    }
}
#endif
