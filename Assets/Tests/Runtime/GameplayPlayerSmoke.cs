#if FPS_GAMEPLAY_SMOKE
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using App.Main.GameMaster;
using App.Main.Player;
using App.Main.ShogiThings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using State = App.Main.GameMaster.GameStateHolder.GameState;

// Compiled only into the separate verification player, never the delivered game.
public sealed class GameplayPlayerSmoke : MonoBehaviour
{
    readonly List<string> results = new(), errors = new();
    string output;
    Gamepad first, second;
    bool finished;
    float began;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Launch() => DontDestroyOnLoad(new GameObject("GameplayPlayerSmoke").AddComponent<GameplayPlayerSmoke>());

    void Awake()
    {
        output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../GameplayReview/PlayerSmoke"));
        Directory.CreateDirectory(output);
        if (File.Exists(Path.Combine(output, "complete.txt"))) File.Delete(Path.Combine(output, "complete.txt"));
        began = Time.realtimeSinceStartup;
        Application.logMessageReceived += Log;
    }
    void Log(string message, string trace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message + "\n" + trace);
    }
    void Update()
    {
        if (!finished && Time.realtimeSinceStartup - began > 90f) { Check(false, "Native checks finish before timeout"); Finish(); }
    }
    static T Find<T>() where T : UnityEngine.Object => UnityEngine.Object.FindFirstObjectByType<T>();
    static void Set(object value, string field, object data) => value.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(value, data);
    static Player One() => Find<PlayerManager>().PlayerOne.GetComponent<Player>();
    static Player Two() => Find<PlayerManager>().PlayerTwo.GetComponent<Player>();
    static Gamepad Pad(Player player) => player.GetComponent<PlayerInput>().devices.OfType<Gamepad>().First();
    static void Input(Player player, GamepadState state) => InputSystem.QueueStateEvent(Pad(player), state);
    void Check(bool pass, string name) { results.Add((pass ? "PASS " : "FAIL ") + name); File.WriteAllLines(Path.Combine(output, "results.txt"), results); }
    void Shot(string name) => ScreenCapture.CaptureScreenshot(Path.Combine(output, name + ".png"));
    static void Place(Player player, Vector3 position, float yaw)
    {
        player.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
        Set(player, "yaw", yaw); Set(player, "pitch", 0f); player.ResetInput();
        player.GetComponent<Rigidbody>().useGravity = false;
        player.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        Physics.SyncTransforms();
    }
    void StartDuel(IPiece defender)
    {
        var board = Find<ShogiBoard>(); var grid = board.GetBoard(); Array.Clear(grid, 0, grid.Length);
        grid[4,4] = new Hisya(PlayerType.PlayerOne); grid[4,5] = defender;
        Check(board.MovePiece(4,4,4,5,PlayerType.PlayerOne) == ShogiBoard.MoveResult.DuelMove, "Native board move starts the duel");
    }
    IEnumerator Start()
    {
        first = InputSystem.AddDevice<Gamepad>("Native gameplay one"); second = InputSystem.AddDevice<Gamepad>("Native gameplay two");
        yield return new WaitForSecondsRealtime(.4f);
        SceneManager.LoadScene("Title"); yield return new WaitForSecondsRealtime(.6f);
        App.UI.GameSessionUI.Instance.StartMatch(); yield return new WaitForSecondsRealtime(.8f);
        Check(Find<ShogiBoard>().GetBoard().Cast<IPiece>().Count(piece => piece != null) == 40, "Native initial board contains forty pieces");
        StartDuel(new Kyosya(PlayerType.PlayerTwo)); yield return new WaitForSecondsRealtime(3.6f);
        var one = One(); var two = Two(); Shot("duel"); yield return new WaitForSecondsRealtime(.2f);
        Check(!App.UI.GameSessionUI.BlocksGameplay && one.GetComponentInChildren<Camera>().enabled, "Native countdown enables split-screen combat");

        // Keep the real players, configured actions, input pairing and projectile prefabs.
        var origin = new Vector3(3000, 200, 2000); Place(one, origin, 0); Place(two, origin + Vector3.forward * 8, 180);
        var field = GameObject.CreatePrimitive(PrimitiveType.Cube); field.name = "Native trigger fixture";
        field.transform.position = origin + Vector3.forward * 4; field.transform.localScale = new Vector3(8, 8, .1f); field.GetComponent<Collider>().isTrigger = true;
        Physics.SyncTransforms();
        int beforeOne = one.playerStatus.Hp.Current, beforeTwo = two.playerStatus.Hp.Current;
        Input(one, new GamepadState { rightTrigger = 1 }); yield return new WaitForSecondsRealtime(.45f); Input(one, new GamepadState());
        Check(two.playerStatus.Hp.Current < beforeTwo && one.playerStatus.Hp.Current == beforeOne, "Native gamepad fires the real bow through a trigger and hits only the opponent");
        int afterArrow = two.playerStatus.Hp.Current; yield return new WaitForSecondsRealtime(.2f);
        Check(two.playerStatus.Hp.Current == afterArrow, "Native arrow does not apply damage twice");
        Input(two, new GamepadState { rightTrigger = 1 }); yield return new WaitForSecondsRealtime(.15f); Input(two, new GamepadState());
        Check(one.playerStatus.Hp.Current < beforeOne, "Native second gamepad fires the gun through a trigger");
        Destroy(field);

        var initialPosition = one.transform.position;
        Input(one, new GamepadState { leftStick = Vector2.up }); yield return new WaitForSecondsRealtime(.2f); Input(one, new GamepadState());
        Check(Vector3.Distance(initialPosition, one.transform.position) > .1f, "Native left stick moves the player");
        var initialYaw = one.transform.eulerAngles.y;
        Input(one, new GamepadState { rightStick = new Vector2(.5f, 0) }); yield return new WaitForSecondsRealtime(.2f); Input(one, new GamepadState());
        yield return new WaitForSecondsRealtime(.1f); float stoppedYaw = one.transform.eulerAngles.y;
        Check(Mathf.Abs(Mathf.DeltaAngle(initialYaw, stoppedYaw)) > 2, "Native right stick turns the camera");
        yield return new WaitForSecondsRealtime(.1f);
        Check(Mathf.Abs(Mathf.DeltaAngle(stoppedYaw, one.transform.eulerAngles.y)) < .01f, "Native neutral stick stops camera rotation");

        Place(one, origin, 0);
        var step = GameObject.CreatePrimitive(PrimitiveType.Cube); step.name = "Native climb fixture";
        step.transform.position = origin + new Vector3(0, -1.25f, 1.3f); step.transform.localScale = new Vector3(3, 1.5f, .4f); Physics.SyncTransforms();
        Input(one, new GamepadState().WithButton(GamepadButton.South)); yield return new WaitForSecondsRealtime(.2f); Input(one, new GamepadState());
        Check(one.transform.position.y > origin.y + .05f, "Native climb button raises the real player beside solid cover");
        Destroy(step);

        one.playerStatus.EffectList.AddEffect(new SlowEffect());
        App.UI.GameSessionUI.Instance.OpenPause(); two.playerStatus.TakeDamage(10000); yield return new WaitForSecondsRealtime(.2f);
        Check(Find<GameStateHolder>().CurrentState == State.Duel, "Native pause defers a pending death");
        App.UI.GameSessionUI.Instance.Resume(); yield return new WaitForSecondsRealtime(2.7f);
        Check(Find<GameStateHolder>().CurrentState == State.PlayerTwoTurn && Find<ShogiBoard>().GetCapturedPieces(PlayerType.PlayerOne).Count == 1, "Native resume resolves the capture and changes turn");
        Check(one.playerStatus.MoveSpeed.Current == one.playerStatus.MoveSpeed.Default && !one.GetComponentInChildren<Camera>().enabled, "Native duel end removes slow and restores board presentation");

        SceneManager.LoadScene("Main"); yield return new WaitForSecondsRealtime(.8f); StartDuel(new Gin(PlayerType.PlayerTwo)); yield return new WaitForSecondsRealtime(3.6f);
        One().playerStatus.TakeDamage(10000); Two().playerStatus.TakeDamage(10000); yield return new WaitForSecondsRealtime(.2f); Shot("draw");
        var drawBoard = Find<ShogiBoard>();
        Check(drawBoard.GetPieceAt(4,4) != null && drawBoard.GetPieceAt(4,5) != null && drawBoard.GetCapturedPieces(PlayerType.PlayerOne).Count == 0 && drawBoard.GetCapturedPieces(PlayerType.PlayerTwo).Count == 0, "Native simultaneous deaths preserve both pieces without captures");
        yield return new WaitForSecondsRealtime(2.4f);
        Check(Find<GameStateHolder>().CurrentState == State.PlayerTwoTurn && !App.UI.GameSessionUI.BlocksGameplay, "Native draw ends the attacker turn and resumes the board");

        SceneManager.LoadScene("Main"); yield return new WaitForSecondsRealtime(.8f); StartDuel(new King(PlayerType.PlayerTwo)); yield return new WaitForSecondsRealtime(3.6f);
        Two().playerStatus.TakeDamage(10000); yield return new WaitForSecondsRealtime(.3f); Shot("king-victory"); yield return new WaitForSecondsRealtime(.2f);
        Check(Find<GameStateHolder>().CurrentState == State.PlayerOneWin, "Native king defeat ends the match with the correct winner");
        SceneManager.LoadScene("Title"); yield return new WaitForSecondsRealtime(.6f); Shot("title"); yield return new WaitForSecondsRealtime(.2f);
        Finish();
    }
    void Finish()
    {
        if (finished) return; finished = true;
        Check(errors.Count == 0, "No native runtime errors");
        File.WriteAllLines(Path.Combine(output, "errors.txt"), errors);
        File.WriteAllText(Path.Combine(output, "complete.txt"), DateTime.Now.ToString("O"));
        Application.logMessageReceived -= Log;
        if (first != null && first.added) InputSystem.RemoveDevice(first);
        if (second != null && second.added) InputSystem.RemoveDevice(second);
        Application.Quit(results.Any(row => row.StartsWith("FAIL")) ? 1 : 0);
    }
}
#endif
