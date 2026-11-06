using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using App.Main.GameMaster;
using App.Main.Player;
using App.Main.ShogiThings;
using App.Main.ShogiPointer;
using App.Main.ViewManager;

[InitializeOnLoad]
public static class UiRegressionRunner
{
    const string Dir = ".utmp/ui-fix/";
    static IEnumerator routine;
    static double nextStep;
    static readonly List<string> results = new List<string>();
    static readonly List<string> runtimeErrors = new List<string>();
    static Gamepad pad1, pad2;
    static Keyboard keyboard;
    static UiRegressionRunner()
    {
        EditorApplication.update += Update;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("UiRegressionPending", false))
            {
                SessionState.SetBool("UiRegressionPending", false);
                results.Clear(); runtimeErrors.Clear();
                Application.logMessageReceived += OnLog;
                routine = Run(); nextStep = EditorApplication.timeSinceStartup + 1;
            }
        };
    }
    static void OnLog(string condition,string trace,LogType type)
    { if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert) runtimeErrors.Add(condition+"\n"+trace); }
    [MenuItem("FPSshogi/Run UI regression")]
    public static void Begin()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory(Dir);
        SessionState.SetBool("UiRegressionPending", true);
        EditorSceneManager.OpenScene("Assets/App/Scenes/Title.unity");
        EditorApplication.isPlaying = true;
    }
    static void Update()
    {
        if (EditorApplication.isCompiling) return;
        string path = Dir + "run-tests.txt";
        if (File.Exists(path) && !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode && routine == null)
        {
            string id = File.ReadAllText(path);
            File.Delete(path);
            if (id != SessionState.GetString("UiRegressionRun", ""))
            { SessionState.SetString("UiRegressionRun", id); Begin(); }
        }
        if (routine == null || EditorApplication.timeSinceStartup < nextStep) return;
        try
        {
            if (!routine.MoveNext()) { Finish(); return; }
            nextStep = EditorApplication.timeSinceStartup + (routine.Current is double delay ? delay : .05);
        }
        catch (Exception ex) { results.Add("ERROR " + ex); Finish(); }
    }
    static void Check(string name, bool condition)
    {
        results.Add((condition ? "PASS " : "FAIL ") + name);
        if(!condition)
        {
            var session=Session();
            string details=session==null?"no session":$"page={Field(session,"page")} gate={Field(session,"releaseGate")} remaining={Field(session,"transitionRemaining")} time={Time.unscaledTime} releaseAfter={Field(session,"releaseAfter")}";
            results.Add($"  scene={SceneManager.GetActiveScene().name} timescale={Time.timeScale} selected={EventSystem.current?.currentSelectedGameObject?.name} {details}");
        }
        File.WriteAllLines(Dir + "regression-live.txt", results);
    }
    static T Find<T>() where T : UnityEngine.Object => UnityEngine.Object.FindFirstObjectByType<T>();
    static object Field(object o, string name) => o.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(o);
    static MonoBehaviour Session() => UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).FirstOrDefault(m => m.GetType().Name == "GameSessionUI");
    static void InvokeSession(string method) => Session()?.GetType().GetMethod(method)?.Invoke(Session(), null);
    static bool Visible(string name) => GameObject.Find(name) != null;
    static void Capture(string name) => ScreenCapture.CaptureScreenshot(Dir + name + ".png");
    static void CheckLayout(string page)
    {
        Canvas.ForceUpdateCanvases();
        var texts=UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Where(t=>t.isActiveAndEnabled && t.transform.IsChildOf(Session().transform)).ToArray();
        var clipped=texts.Where(t=>t.preferredHeight>t.rectTransform.rect.height+1).Select(t=>$"{t.name}: needs {t.preferredHeight:0.0}, has {t.rectTransform.rect.height:0.0}").ToArray();
        File.WriteAllLines(Dir+page+"-text-layout.txt",clipped);
        Check(page+" text fits its containers",clipped.Length==0);
        var corners=new Vector3[4];
        bool inside=true;
        foreach(var selectable in UnityEngine.Object.FindObjectsByType<Selectable>(FindObjectsSortMode.None).Where(s=>s.isActiveAndEnabled))
        {
            selectable.GetComponent<RectTransform>().GetWorldCorners(corners);
            if(corners.Any(c=>c.x<-.5f||c.y<-.5f||c.x>Screen.width+.5f||c.y>Screen.height+.5f)) inside=false;
        }
        Check(page+" controls remain inside screen",inside);
    }
    static IEnumerator Run()
    {
        pad1 = InputSystem.AddDevice<Gamepad>("UI Test One");
        pad2 = InputSystem.AddDevice<Gamepad>("UI Test Two");
        yield return .4;
        Check("Connected gamepads wait for explicit player selection",!App.UI.GameSessionUI.Instance.Devices.Connected(0) && !App.UI.GameSessionUI.Instance.Devices.Connected(1));
        SceneManager.LoadScene("Title");
        yield return .5;
        Check("Title starts with an actionable selection", EventSystem.current?.currentSelectedGameObject?.GetComponent<Button>()?.interactable == true);
        keyboard=InputSystem.AddDevice<Keyboard>("UI Test Keyboard");
        InvokeSession("OpenHelp"); yield return .3;
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape)); yield return .15;
        InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return .3;
        Check("Keyboard Escape returns from help",Visible("Start button"));
        InvokeSession("OpenHelp"); yield return .3;
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Enter)); yield return .15;
        InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return .3;
        Check("Keyboard Enter activates focused Back",Visible("Start button"));
        InputSystem.RemoveDevice(keyboard); keyboard=null;
        InvokeSession("Back"); yield return .3;
        Capture("test-title");
        yield return .3;
        CheckLayout("title");
        InvokeSession("OpenHelp"); yield return .3;
        CheckLayout("help"); Capture("test-help"); yield return .3;
        GameObject.Find("Abilities tab").GetComponent<Button>().onClick.Invoke(); yield return .3;
        CheckLayout("abilities"); Capture("test-abilities"); yield return .3;
        InputSystem.QueueStateEvent(pad1,new GamepadState().WithButton(GamepadButton.East)); yield return .15;
        InputSystem.QueueStateEvent(pad1,new GamepadState()); yield return .3;
        Check("Gamepad cancel returns from help",Visible("Start button"));
        InvokeSession("Back"); InvokeSession("OpenSettings"); yield return .3;
        CheckLayout("settings"); Capture("test-settings"); yield return .3;
        InvokeSession("Back"); yield return .3;
        InputSystem.QueueStateEvent(pad1, new GamepadState().WithButton(GamepadButton.South));
        yield return .2;
        InputSystem.QueueStateEvent(pad1, new GamepadState());
        yield return .5;
        Check("Gamepad submit opens player selection", SceneManager.GetActiveScene().name == "Title" && Visible("Player selection panel"));
        CheckLayout("selection"); Capture("test-selection-empty");
        var join=SelectionTestInput.TwoGamepads(pad1,pad2,false);while(join.MoveNext())yield return join.Current;
        Check("Both players confirm before the match starts", SceneManager.GetActiveScene().name == "Main");
        yield return .6;
        var players = UnityEngine.Object.FindObjectsByType<PlayerInput>(FindObjectsSortMode.None);
        Check("Both players have distinct devices", players.Length == 2 && players.All(p => p.devices.Count > 0) && !players[0].devices.Intersect(players[1].devices).Any());
        Check("Board shows whose turn it is", Visible("Turn label"));
        var pointer = Find<ShogiPointer>();
        Check("Title submit does not select a board piece",pointer.SelectedPosition.x<0);
        var view=Find<ViewPiece>();
        File.WriteAllLines(Dir+"board-geometry.txt", new[]{new Vector2Int(0,0),new Vector2Int(8,0),new Vector2Int(0,8),new Vector2Int(8,8)}.Select(p=>$"{p}: world={view.boardCellPositions[p.x,p.y]} screen={Camera.main.WorldToScreenPoint(view.boardCellPositions[p.x,p.y])}"));
        File.WriteAllLines(Dir+"geometry-renderers.txt",UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>!r.name.Contains("Clone")).Select(r=>$"{r.name} mesh={r.GetComponent<MeshFilter>()?.sharedMesh?.name} bounds={r.bounds}"));
        pointer.Select(); pointer.SelectPositionUp();
        yield return .2;
        Check("Selected origin stays visible after cursor movement", Visible("Selected tile"));
        Capture("test-selection"); yield return .3;
        CheckLayout("board");

        var board = Find<ShogiBoard>();
        Check("Legal target validation rejects occupied and outside squares",board.CanMove(0,0,0,1,PlayerType.PlayerOne) && !board.CanMove(0,0,1,0,PlayerType.PlayerOne) && !board.CanMove(-1,0,0,1,PlayerType.PlayerOne));
        var held = (Dictionary<PlayerType,List<PieceType>>)Field(board,"capturedPieces");
        held[PlayerType.PlayerTwo] = new List<PieceType>{PieceType.Gin};
        Find<GameStateHolder>().ChangeIntoPlayerTwoTurn();
        yield return .2;
        Check("Silver can be dropped on a file containing own pawn", board.SetPiece(4,4,PieceType.Gin,PlayerType.PlayerTwo) == ShogiBoard.MoveResult.NormalMove);
        if (board.GetCapturedPieces(PlayerType.PlayerTwo).Count > 0)
        {
            board.GetBoard()[4,6] = null;
            board.SetPiece(4,4,PieceType.Gin,PlayerType.PlayerTwo);
        }
        yield return .2;
        var shown = (Dictionary<PieceType,GameObject>)Field(Find<ViewCapturedPieces>(),"playerTwoCapturedPieceObjects");
        Check("Last consumed P2 piece disappears from tray", shown.Count == 0);
        held[PlayerType.PlayerOne] = new List<PieceType>{PieceType.Fuhyo,PieceType.Fuhyo,PieceType.Fuhyo};
        Find<GameStateHolder>().ChangeIntoPlayerTwoTurn(); Find<GameStateHolder>().ChangeIntoPlayerOneTurn();
        yield return .2;
        Check("Captured count is visible", UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t => t.isActiveAndEnabled && t.text.Contains("歩") && t.text.Contains("3")));
        Capture("test-board");
        yield return .3;
        Check("Pawn drop still rejects nifu",!board.CanDrop(4,3,PieceType.Fuhyo,PlayerType.PlayerOne));
        board.GetBoard()[4,2]=null;
        GameObject.Find("P1 captured Fuhyo").GetComponent<Button>().onClick.Invoke(); yield return .2;
        Check("Captured button enters visible placement mode",pointer.IsPlacingCaptured && GameObject.Find("Turn label").GetComponent<Text>().text.Contains("持ち駒を打つ") && board.CanDrop(4,3,PieceType.Fuhyo,PlayerType.PlayerOne));
        Capture("test-captured-placement"); yield return .3;
        pointer.SetCursor(4,3); pointer.Select(); yield return .3;
        Check("Placing a captured piece consumes exactly one and changes turn",board.GetCapturedPieces(PlayerType.PlayerOne)[PieceType.Fuhyo]==2 && board.GetPieceAt(4,3)?.Player==PlayerType.PlayerOne && Find<GameStateHolder>().CurrentState==GameStateHolder.GameState.PlayerTwoTurn && !pointer.IsPlacingCaptured);

        SceneManager.LoadScene("Main"); yield return .6;
        board = Find<ShogiBoard>();
        board.MovePiece(0,2,0,3,PlayerType.PlayerOne); board.MovePiece(0,6,0,5,PlayerType.PlayerTwo);
        board.MovePiece(0,3,0,4,PlayerType.PlayerOne); board.MovePiece(0,5,0,4,PlayerType.PlayerTwo);
        yield return .3;
        Check("Duel begins with an input-blocking countdown", Time.timeScale == 0f && Visible("Transition title"));
        yield return 3.5;
        Check("Duel has two HP displays and crosshairs", Visible("P1 HP") && Visible("P2 HP") && Visible("P1 Crosshair") && Visible("P2 Crosshair"));
        CheckLayout("duel");
        var p1 = Find<PlayerManager>().PlayerOne.GetComponent<Player>();
        p1.playerStatus.TakeDamage(20); yield return .2;
        Check("HP display reflects damage", GameObject.Find("P1 HP")?.GetComponent<Text>()?.text.Contains("80") == true);
        Capture("test-duel");
        yield return .3;
        InputSystem.QueueStateEvent(pad1,new GamepadState{rightTrigger=1}); yield return .15;
        InputSystem.QueueStateEvent(pad1,new GamepadState()); yield return .15;
        Check("Attack cooldown follows real gamepad action",p1.PrimaryCooldown>0 && GameObject.Find("P1 cooldowns").GetComponent<Text>().text.Contains("秒"));
        var weapon = p1.WeaponObject;
        float cooldownAtPause=p1.PrimaryCooldown;
        InvokeSession("OpenPause"); yield return .2;
        Check("Pause stops simulation", Time.timeScale == 0f);
        Check("Pause freezes action cooldown",Mathf.Abs(p1.PrimaryCooldown-cooldownAtPause)<.04f);
        CheckLayout("pause"); Capture("test-pause"); yield return .3;
        InvokeSession("Resume"); yield return .2;
        Check("Resume preserves duel weapon and HP", Time.timeScale == 1f && weapon != null && p1.WeaponObject == weapon && p1.playerStatus.Hp.Current == 80);
        InputSystem.RemoveDevice(pad2); pad2 = null; yield return .5;
        Check("Disconnect blocks gameplay", Time.timeScale == 0f && Visible("Connection message"));
        CheckLayout("connection"); Capture("test-connection"); yield return .3;
        pad2 = InputSystem.AddDevice<Gamepad>("UI Test Two Reconnected"); yield return .5;
        CheckLayout("reconnected"); Capture("test-reconnected"); yield return .2;
        InvokeSession("Resume"); yield return .2;
        Check("Reconnect resumes existing duel", Time.timeScale == 1f && p1.WeaponObject == weapon);

        SceneManager.LoadScene("Main"); yield return .6;
        board = Find<ShogiBoard>();
        board.GetBoard()[0,4] = board.GetBoard()[4,0]; board.GetBoard()[4,0] = null;
        board.GetBoard()[0,5] = board.GetBoard()[4,8]; board.GetBoard()[4,8] = null;
        board.MovePiece(0,4,0,5,PlayerType.PlayerOne); yield return 3.5;
        Find<PlayerManager>().PlayerTwo.GetComponent<Player>().playerStatus.Hp.Subtract(100000);
        yield return .4;
        Check("Match result offers a rematch", Visible("Rematch button"));
        CheckLayout("result");
        Capture("test-result");
        yield return 5.5;
        Check("Result waits for player choice", SceneManager.GetActiveScene().name == "Main" && Visible("Rematch button"));
        InputSystem.RemoveDevice(pad2); pad2=null; yield return .2;
        GameObject.Find("Rematch button")?.GetComponent<Button>()?.onClick.Invoke(); yield return .6;
        Check("Rematch selection waits for missing controller",Visible("Player selection panel") && Time.timeScale==0);
        pad2=InputSystem.AddDevice<Gamepad>("UI Test Rematch Reconnected"); yield return .4;
        join=SelectionTestInput.TwoGamepads(pad1,pad2,false);while(join.MoveNext())yield return join.Current;
        Check("Rematch after reconnect starts a fresh match",Find<GameStateHolder>()?.CurrentState==GameStateHolder.GameState.PlayerOneTurn && Time.timeScale==1);
        Check("Rematch returns to fresh board", Find<GameStateHolder>()?.CurrentState == GameStateHolder.GameState.PlayerOneTurn && Time.timeScale == 1f);
        Check("Only one UI root after rematch", UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Count(m => m.GetType().Name == "GameSessionUI") == 1);
        InvokeSession("OpenPause"); InvokeSession("ConfirmReturn"); yield return .2;
        GameObject.Find("Confirm return button").GetComponent<Button>().onClick.Invoke(); yield return .6;
        Check("Confirmed return reaches usable title",SceneManager.GetActiveScene().name=="Title" && Time.timeScale==1 && EventSystem.current?.currentSelectedGameObject?.name=="Start button");
    }
    static void Finish()
    {
        routine = null;
        Application.logMessageReceived -= OnLog;
        File.WriteAllLines(Dir+"runtime-errors.txt",runtimeErrors);
        Check("No runtime errors or exceptions",runtimeErrors.Count==0);
        if (pad1 != null && pad1.added) InputSystem.RemoveDevice(pad1);
        if (pad2 != null && pad2.added) InputSystem.RemoveDevice(pad2);
        pad1 = pad2 = null;
        if(keyboard!=null && keyboard.added) InputSystem.RemoveDevice(keyboard);
        keyboard=null;
        results.Add($"TOTAL {results.Count(s=>s.StartsWith("PASS"))} PASS / {results.Count(s=>s.StartsWith("FAIL")||s.StartsWith("ERROR"))} FAIL");
        File.WriteAllLines(Dir + "regression-results.txt", results);
        Time.timeScale = 1; EditorApplication.isPlaying = false;
    }
}
