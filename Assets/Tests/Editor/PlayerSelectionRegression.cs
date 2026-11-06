using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using App.Main.GameMaster;
using App.Main.Player;
using App.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class PlayerSelectionRegression
{
    const string Trigger = ".utmp/player-selection/run.txt";
    static IEnumerator routine;
    static string dir;
    static double next;
    static Gamepad first, second, spare;
    static Keyboard keyboard;
    static Mouse mouse;
    static readonly List<string> results = new(), errors = new();
    static PlayerSelectionRegression()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("SelectionChecks", false))
            {
                SessionState.SetBool("SelectionChecks", false);
                dir = "SelectionReview/" + SessionState.GetString("SelectionChecksDir", "After") + "/";
                Directory.CreateDirectory(dir); results.Clear(); errors.Clear();
                if (File.Exists(dir + "complete.txt")) File.Delete(dir + "complete.txt");
                Application.logMessageReceived += Log; routine = Run(); next = EditorApplication.timeSinceStartup + 1;
            }
            if (state == PlayModeStateChange.ExitingPlayMode && routine != null) Finish(false);
        };
    }
    static void Tick()
    {
        if (EditorApplication.isCompiling) return;
        if (File.Exists(Trigger) && !EditorApplication.isPlayingOrWillChangePlaymode && routine == null)
        {
            SessionState.SetString("SelectionChecksDir", File.ReadAllText(Trigger).Trim()); File.Delete(Trigger);
            SessionState.SetBool("SelectionChecks", true); EditorSceneManager.OpenScene("Assets/App/Scenes/Title.unity"); EditorApplication.isPlaying = true;
        }
        if (routine == null || EditorApplication.timeSinceStartup < next) return;
        try { if (!routine.MoveNext()) { Finish(true); return; } next = EditorApplication.timeSinceStartup + (routine.Current is double delay ? delay : .05); }
        catch (Exception e) { errors.Add(e.ToString()); Check("Selection suite completes without exception", false); Finish(true); }
    }
    static void Log(string message, string trace, LogType type) { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message + "\n" + trace); }
    static void Check(string name, bool pass) { results.Add((pass ? "PASS " : "FAIL ") + name); File.WriteAllLines(dir + "results.txt", results); }
    static void Shot(string name) => ScreenCapture.CaptureScreenshot(dir + name + ".png");
    static bool Lobby => GameObject.Find("Player selection panel") != null;
    static bool Prepared(int slot) => GameObject.Find($"P{slot+1} selection state")?.GetComponent<Text>().text == "準備完了";
    static GameSessionUI Session => GameSessionUI.Instance;
    static T Find<T>() where T : UnityEngine.Object => UnityEngine.Object.FindFirstObjectByType<T>();
    static void Pad(Gamepad device, GamepadButton? button = null) => InputSystem.QueueStateEvent(device, button.HasValue ? new GamepadState().WithButton(button.Value) : new GamepadState());
    static void Key(Key? key = null) => InputSystem.QueueStateEvent(keyboard, key.HasValue ? new KeyboardState(key.Value) : new KeyboardState());
    static IEnumerator Tap(Gamepad device, GamepadButton button = GamepadButton.South)
    { Pad(device, button); yield return .12d; Pad(device); yield return .16d; }
    static IEnumerator TapKey(Key key = UnityEngine.InputSystem.Key.Enter)
    {
        // The editor's real mouse can become current after a computer-use click.
        // Give the fixture mouse an actual event before keyboard participation.
        InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(20 + Time.frameCount % 100, 20) });
        yield return .05d;
        File.AppendAllText(dir + "device-identities.txt", $"Before {key}: fixture mouse={mouse.deviceId}, current mouse={Mouse.current?.deviceId}\n");
        Key(key); yield return .12d; Key(); yield return .16d;
    }
    static IEnumerator Click(string name)
    {
        var rect=GameObject.Find(name).GetComponent<RectTransform>();var corners=new Vector3[4];rect.GetWorldCorners(corners);
        Vector2 center=(corners[0]+corners[2])*.5f;
        InputSystem.QueueStateEvent(mouse,new MouseState{position=center,buttons=1});yield return .12d;
        InputSystem.QueueStateEvent(mouse,new MouseState{position=center});yield return .2d;
    }
    static IEnumerator NewLobby()
    { SceneManager.LoadScene("Title"); yield return .6d; Session.StartMatch(); yield return .4d; }
    static void Layout(string name)
    {
        Canvas.ForceUpdateCanvases();
        var clipped = UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Where(t => t.isActiveAndEnabled && t.transform.IsChildOf(Session.transform) && t.preferredHeight > t.rectTransform.rect.height + 1).Select(t => $"{t.name}: needs {t.preferredHeight}, has {t.rectTransform.rect.height}").ToArray();
        File.WriteAllLines(dir + name + "-layout.txt", clipped); Check(name + " text fits", clipped.Length == 0);
        var corners = new Vector3[4]; bool inside = true;
        foreach (var button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Where(b => b.isActiveAndEnabled))
        { button.GetComponent<RectTransform>().GetWorldCorners(corners); if (corners.Any(c => c.x < 0 || c.y < 0 || c.x > Screen.width + 1 || c.y > Screen.height + 1)) inside = false; }
        Check(name + " buttons fit screen", inside);
    }
    static IEnumerator Run()
    {
        File.WriteAllText(dir + "device-identities.txt", $"Screen: {Screen.width}x{Screen.height}\n");
        first = InputSystem.AddDevice<Gamepad>("Selection First"); second = InputSystem.AddDevice<Gamepad>("Selection Second"); spare = InputSystem.AddDevice<Gamepad>("Selection Spare");
        keyboard = InputSystem.AddDevice<Keyboard>("Selection Keyboard"); mouse = InputSystem.AddDevice<Mouse>("Selection Mouse");
        var flow = NewLobby(); while (flow.MoveNext()) yield return flow.Current;
        Check("Start opens player selection instead of loading a match", SceneManager.GetActiveScene().name == "Title" && Lobby);
        Check("Connected devices are not assigned until they participate", !Session.Devices.Connected(0) && !Session.Devices.Connected(1));
        Shot("empty"); if (!Lobby) yield break;
        Layout("empty");

        // Join in the reverse of connection order. Holding that press cannot also prepare.
        Pad(second, GamepadButton.South); yield return .7d;
        Check("First press joins only 1P", Session.Devices.Connected(0) && !Session.Devices.Connected(1));
        Check("Held join does not prepare the player", !Prepared(0)); Pad(second); yield return .2d;
        flow = Tap(second); while (flow.MoveNext()) yield return flow.Current;
        Check("Second press prepares only its player", Prepared(0) && !Prepared(1)); yield return 1.2d;
        Check("One prepared player cannot start", Lobby && SceneManager.GetActiveScene().name == "Title");
        flow = Tap(first); while (flow.MoveNext()) yield return flow.Current;
        Check("Second device joins 2P unprepared", Session.Devices.Connected(1) && !Prepared(1));
        flow = Tap(spare); while (flow.MoveNext()) yield return flow.Current;
        Check("Third controller cannot prepare an occupied slot", Prepared(0) && !Prepared(1) && Lobby);
        flow = Tap(spare, GamepadButton.East); while (flow.MoveNext()) yield return flow.Current;
        Check("Unjoined controller cannot cancel another player", Prepared(0) && Session.Devices.Connected(1));
        flow = Tap(second, GamepadButton.East); while (flow.MoveNext()) yield return flow.Current;
        Check("Cancel first clears readiness and keeps assignment", !Prepared(0) && Session.Devices.Connected(0));
        flow = Tap(second, GamepadButton.East); while (flow.MoveNext()) yield return flow.Current;
        Check("Second cancel leaves only its own slot", !Session.Devices.Connected(0) && Session.Devices.Connected(1));
        flow = Tap(spare); while (flow.MoveNext()) yield return flow.Current;
        flow = Tap(first); while (flow.MoveNext()) yield return flow.Current;
        Pad(spare, GamepadButton.South); yield return .15d; Pad(spare); yield return .08d;
        Check("Both cards show preparation before launch", Prepared(0) && Prepared(1)); Shot("both-ready"); Layout("both-ready");
        yield return .08d;
        InputSystem.DisableDevice(first); yield return 1.2d;
        Check("Disconnection during ready delay aborts launch", Lobby && Prepared(0) && !Prepared(1) && !Session.Devices.Connected(1)); Shot("disconnected"); Layout("disconnected");
        InputSystem.EnableDevice(first); yield return .3d;
        Check("Reconnected selection requires fresh preparation", Session.Devices.Connected(1) && !Prepared(1));
        flow = Tap(first); while (flow.MoveNext()) yield return flow.Current;
        flow = Tap(spare, GamepadButton.East); while (flow.MoveNext()) yield return flow.Current; yield return 1.2d;
        Check("Cancelling during ready delay aborts launch", Lobby && !Prepared(0) && Prepared(1));
        Pad(spare, GamepadButton.South); yield return 1.5d;
        Check("Both prepared controllers start Main", SceneManager.GetActiveScene().name == "Main" && !Lobby);
        var manager = Find<PlayerManager>();
        Check("1P uses the controller that claimed 1P", manager.PlayerOne.GetComponent<PlayerInput>().devices.Contains(spare));
        Check("2P keeps the controller that claimed 2P", manager.PlayerTwo.GetComponent<PlayerInput>().devices.Contains(first));
        Check("Held ready button is blocked from board controls", GameSessionUI.BlocksGameplay);
        Check("Held ready button does not select a board piece", Find<App.Main.ShogiPointer.ShogiPointer>().SelectedPosition.x < 0);
        Pad(spare); yield return .4d; Check("Releasing ready enables gameplay", !GameSessionUI.BlocksGameplay);

        var survivor = manager.PlayerTwo;
        InputSystem.RemoveDevice(spare); spare = null; yield return .4d;
        Check("Disconnect pauses an existing match", Time.timeScale == 0 && GameObject.Find("Connection message") != null);
        // The unused original second controller is available as replacement.
        Session.Resume(); yield return .4d;
        Check("Reconnect preserves the surviving player's assignment", survivor != null && manager.PlayerTwo == survivor && manager.PlayerTwo.GetComponent<PlayerInput>().devices.Contains(first));
        var oldState = Find<GameStateHolder>(); Session.StartMatch(); yield return .4d;
        Check("Rematch opens selection and resets readiness", Lobby && !Prepared(0) && !Prepared(1));
        flow = Tap(second); while (flow.MoveNext()) yield return flow.Current; yield return 1.2d;
        Check("Rematch cannot start with only one confirmation", Lobby && oldState != null);
        flow = Tap(first); while (flow.MoveNext()) yield return flow.Current; yield return 1.5d;
        Check("Both rematch confirmations create a fresh match", !Lobby && oldState == null && Find<GameStateHolder>().CurrentState == GameStateHolder.GameState.PlayerOneTurn);

        foreach (bool keyboardFirst in new[] { true, false })
        {
            flow = NewLobby(); while (flow.MoveNext()) yield return flow.Current;
            flow = keyboardFirst ? TapKey() : Tap(second); while (flow.MoveNext()) yield return flow.Current;
            flow = keyboardFirst ? Tap(second) : TapKey(); while (flow.MoveNext()) yield return flow.Current;
            Check("Mixed participants join without being prepared (keyboard first=" + keyboardFirst + ")", Session.Devices.Ready && !Prepared(0) && !Prepared(1));
            Check("Mixed mode honors choice despite extra connected pad", Session.Devices.IsGamepad(0) != keyboardFirst && Session.Devices.IsGamepad(1) == keyboardFirst);
            Shot(keyboardFirst ? "keyboard-first" : "controller-first"); Layout("mixed-" + keyboardFirst);
            flow = TapKey(); while (flow.MoveNext()) yield return flow.Current; yield return 1.2d;
            Check("Keyboard prepares only its own slot", Lobby && Prepared(keyboardFirst ? 0 : 1) && !Prepared(keyboardFirst ? 1 : 0));
            flow = Tap(second); while (flow.MoveNext()) yield return flow.Current; yield return 1.5d;
            manager = Find<PlayerManager>();
            var keyboardPlayer = (keyboardFirst ? manager.PlayerOne : manager.PlayerTwo).GetComponent<PlayerInput>();
            var padPlayer = (keyboardFirst ? manager.PlayerTwo : manager.PlayerOne).GetComponent<PlayerInput>();
            File.AppendAllText(dir + "device-identities.txt", $"keyboardFirst={keyboardFirst}: paired={string.Join(",", keyboardPlayer.devices.Select(d => d.layout + "#" + d.deviceId))}; expected={keyboard.deviceId},{mouse.deviceId}\n");
            Check("Mixed mode starts with keyboard AND mouse paired", !Lobby && keyboardPlayer.devices.Contains(keyboard) && keyboardPlayer.devices.Contains(mouse));
            Check("Mixed mode pairs only the participating controller", padPlayer.devices.Count == 1 && padPlayer.devices.Contains(second));
        }
        flow = NewLobby(); while (flow.MoveNext()) yield return flow.Current;
        flow = TapKey(); while (flow.MoveNext()) yield return flow.Current;
        flow = TapKey(); while (flow.MoveNext()) yield return flow.Current;
        Check("Same keyboard cannot fill the second slot", Session.Devices.Connected(0) && !Session.Devices.Connected(1));
        flow = TapKey(UnityEngine.InputSystem.Key.Escape); while (flow.MoveNext()) yield return flow.Current;
        Check("Keyboard cancel clears its preparation", Lobby && !Prepared(0) && Session.Devices.Connected(0));
        flow = TapKey(UnityEngine.InputSystem.Key.Escape); while (flow.MoveNext()) yield return flow.Current;
        Check("Keyboard can leave its slot", Lobby && !Session.Devices.Connected(0));
        flow = TapKey(UnityEngine.InputSystem.Key.Escape); while (flow.MoveNext()) yield return flow.Current;
        Check("Empty selection can return to title", !Lobby && GameObject.Find("Start button") != null);
        flow = NewLobby(); while (flow.MoveNext()) yield return flow.Current;
        flow = Click("Keyboard selection button"); while (flow.MoveNext()) yield return flow.Current;
        Check("Mouse button joins keyboard/mouse without preparing", Session.Devices.Connected(0) && !Session.Devices.IsGamepad(0) && !Prepared(0) && !Session.Devices.Connected(1));
        flow = Tap(first); while (flow.MoveNext()) yield return flow.Current;
        Check("Controller submit cannot activate the focused keyboard button", !Prepared(0) && Session.Devices.IsGamepad(1) && !Prepared(1));
        flow = Click("Keyboard selection button"); while (flow.MoveNext()) yield return flow.Current;
        Check("Mouse button prepares only keyboard/mouse", Prepared(0) && !Prepared(1));
        flow = Click("Keyboard selection button"); while (flow.MoveNext()) yield return flow.Current;
        Check("Mouse button cancels keyboard/mouse readiness", !Prepared(0) && Session.Devices.Connected(0));
        flow = Click("Selection back button"); while (flow.MoveNext()) yield return flow.Current;
        Check("Mouse can return from selection to title", !Lobby && GameObject.Find("Start button") != null);
    }
    static void Finish(bool stop)
    {
        routine = null; Application.logMessageReceived -= Log;
        Check("No runtime errors or exceptions", errors.Count == 0); File.WriteAllLines(dir + "errors.txt", errors);
        File.WriteAllText(dir + "complete.txt", DateTime.Now.ToString("O"));
        foreach (var device in new InputDevice[] { first, second, spare, keyboard, mouse }) if (device != null && device.added) InputSystem.RemoveDevice(device);
        Time.timeScale = 1; if (stop) EditorApplication.isPlaying = false;
    }
}
