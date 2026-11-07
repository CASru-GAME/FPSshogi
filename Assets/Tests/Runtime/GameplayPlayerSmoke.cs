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
using App.PlayData;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using State = App.Main.GameMaster.GameStateHolder.GameState;

// Compiled only into the separate verification player, never the delivered game.
public sealed partial class GameplayPlayerSmoke : MonoBehaviour
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
        string suite = dashRun ? "DashContact" : reachRun ? "MeleeReach" : openingRun ? "DuelOpenings" : matchupRun ? "Matchups" : "PlayerSmoke";
        output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../GameplayReview", suite));
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
    static IEnumerator Tap(Player player, GamepadButton button)
    {
        Input(player, new GamepadState().WithButton(button)); yield return new WaitForSecondsRealtime(.14f);
        Input(player, new GamepadState()); yield return new WaitForSecondsRealtime(.22f);
    }
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
    void StartDuel(IPiece defender, IPiece attacker = null)
    {
        var board = Find<ShogiBoard>(); var grid = board.GetBoard(); Array.Clear(grid, 0, grid.Length);
        grid[4,4] = attacker ?? new Hisya(PlayerType.PlayerOne); grid[4,5] = defender;
        Check(board.MovePiece(4,4,4,5,PlayerType.PlayerOne) == ShogiBoard.MoveResult.DuelMove, "Native board move starts the duel");
    }
    IEnumerator Start()
    {
        first = InputSystem.AddDevice<Gamepad>("Native gameplay one"); second = InputSystem.AddDevice<Gamepad>("Native gameplay two");
        yield return new WaitForSecondsRealtime(.4f);
        SceneManager.LoadScene("Title"); yield return new WaitForSecondsRealtime(.6f);
        var join=SelectionTestInput.TwoGamepads(first,second);while(join.MoveNext())yield return new WaitForSecondsRealtime((float)(double)join.Current);
        Check(Find<ShogiBoard>().GetBoard().Cast<IPiece>().Count(piece => piece != null) == 40, "Native initial board contains forty pieces");
        if (dashRun)
        {
            yield return CheckDashContacts();
            Finish();
            yield break;
        }
        if (reachRun)
        {
            yield return CheckMeleeReach();
            Finish();
            yield break;
        }
        if (openingRun)
        {
            yield return CheckDuelOpenings();
            Finish();
            yield break;
        }
        if (matchupRun)
        {
            yield return CheckMatchups();
            Finish();
            yield break;
        }
        StartDuel(new Kyosya(PlayerType.PlayerTwo)); yield return new WaitForSecondsRealtime(3.6f);
        var one = One(); var two = Two(); Shot("duel"); yield return new WaitForSecondsRealtime(.2f);
        Check(!App.UI.GameSessionUI.BlocksGameplay && one.GetComponentInChildren<Camera>().enabled, "Native countdown enables split-screen combat");
        Input(one, new GamepadState { rightTrigger = 1 });
        yield return new WaitForSecondsRealtime(.55f);
        Shot("bow-drawing");
        Check(one.BowDrawAmount > .2f && one.PrimaryCooldown == 0, "Native holding RT draws without shooting");
        Input(one, new GamepadState { rightTrigger = 1 }.WithButton(GamepadButton.Start));
        yield return new WaitForSecondsRealtime(.18f);
        Input(one, new GamepadState());
        yield return new WaitForSecondsRealtime(.24f);
        yield return Tap(one, GamepadButton.DpadDown);
        yield return Tap(one, GamepadButton.South);
        Check(GameObject.Find("Zoom sensitivity slider 0") != null && GameObject.Find("Screen divider") == null &&
            Camera.main != null && !one.GetComponentInChildren<Camera>().enabled && !two.GetComponentInChildren<Camera>().enabled,
            "Native controller opens duel settings over a continuous room view");
        var export = GameObject.Find("Export play data").GetComponent<UnityEngine.UI.Button>();
        Check(export != null && GameObject.Find("Open play data folder") != null, "Native settings expose local play record export");
        export.onClick.Invoke();
        yield return WaitForExport(export);
        var snapshot = ReadMatches().Single();
        Check(snapshot.result == "unfinished" && snapshot.duels.Single().result == "unfinished" &&
            snapshot.duels[0].seats[0].attacks == 0 && one.PrimaryCooldown == 0 && one.BowDrawAmount == 0,
            "Native mid-match export preserves an unfinished duel and pausing a drawn bow fires no arrow");
        double pausedSeconds = snapshot.duels[0].seconds;
        yield return new WaitForSecondsRealtime(.5f);
        export.onClick.Invoke();
        yield return WaitForExport(export);
        Check(Math.Abs(ReadMatches().Single().duels[0].seconds - pausedSeconds) < .01, "Native exported combat clock excludes time in settings");
        Shot("duel-settings"); yield return new WaitForSecondsRealtime(.25f);
        yield return Tap(one, GamepadButton.East);
        yield return Tap(one, GamepadButton.Start);
        Check(!App.UI.GameSessionUI.BlocksGameplay && GameObject.Find("Screen divider") != null &&
            one.GetComponentInChildren<Camera>().enabled && two.GetComponentInChildren<Camera>().enabled,
            "Native resume restores both split views and the divider");

        // Keep the real players, configured actions, input pairing and projectile prefabs.
        var origin = new Vector3(3000, 200, 2000); Place(one, origin, 0); Place(two, origin + Vector3.forward * 8, 180);
        var field = GameObject.CreatePrimitive(PrimitiveType.Cube); field.name = "Native trigger fixture";
        field.transform.position = origin + Vector3.forward * 4; field.transform.localScale = new Vector3(8, 8, .1f); field.GetComponent<Collider>().isTrigger = true;
        Physics.SyncTransforms();
        int beforeOne = one.playerStatus.Hp.Current, beforeTwo = two.playerStatus.Hp.Current;
        var bow = one.WeaponObject.GetComponent<WeaponPresentation>();
        var relaxedTip = bow.bowString.GetPosition(0);
        var relaxedNock = bow.bowString.GetPosition(1);
        Input(one, new GamepadState { rightTrigger = 1 });
        yield return new WaitForSecondsRealtime(.55f);
        var halfNock = bow.bowString.GetPosition(1);
        yield return new WaitForSecondsRealtime(.9f);
        var fullTip = bow.bowString.GetPosition(0);
        var fullNock = bow.bowString.GetPosition(1);
        var tail = bow.nockedArrow.localPosition + Vector3.forward * bow.nockedArrow.GetComponent<MeshFilter>().sharedMesh.bounds.min.z;
        var bestTipDistance = bow.visual.GetComponent<MeshFilter>().sharedMesh.vertices.Min(point => Vector3.Distance(point, fullTip));
        Check(one.BowDrawAmount == 1f && GameObject.Find("P1 bow draw track") != null && two.playerStatus.Hp.Current == beforeTwo &&
            fullNock.z < halfNock.z && halfNock.z < relaxedNock.z && fullTip.z < relaxedTip.z - .1f &&
            bestTipDistance < .015f && Vector3.Distance(tail, fullNock) < .015f,
            "Native full draw holds the arrow on the string, bends the limbs and fills the first-seat gauge");
        Input(one, new GamepadState());
        yield return new WaitForSecondsRealtime(.35f);
        Check(two.playerStatus.Hp.Current == beforeTwo - 84 && one.playerStatus.Hp.Current == beforeOne &&
            GameObject.Find("P1 bow draw track") == null && Vector3.Distance(relaxedTip,bow.bowString.GetPosition(0)) < .01f,
            "Native RT release alone fires one 84-damage arrow through the trigger and relaxes the bow");
        int afterArrow = two.playerStatus.Hp.Current; yield return new WaitForSecondsRealtime(.2f);
        Check(two.playerStatus.Hp.Current == afterArrow, "Native arrow does not apply damage twice");
        Input(two, new GamepadState { rightTrigger = 1 }); yield return new WaitForSecondsRealtime(.15f); Input(two, new GamepadState());
        Check(one.playerStatus.Hp.Current < beforeOne, "Native second gamepad fires the gun through a trigger");
        Destroy(field);
        // An early press is rejected. The next accepted shot points away from the opponent.
        yield return new WaitForSecondsRealtime(.08f);
        Input(two, new GamepadState {rightTrigger=1}); yield return new WaitForSecondsRealtime(.1f); Input(two, new GamepadState());
        yield return new WaitForSecondsRealtime(two.PrimaryCooldown + .12f);
        Place(two, two.transform.position, 90);
        Input(two, new GamepadState {rightTrigger=1}); yield return new WaitForSecondsRealtime(.12f); Input(two, new GamepadState());

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

        var resumedBoard = Find<ShogiBoard>();
        resumedBoard.GetBoard()[0,6] = new Fuhyo(PlayerType.PlayerTwo);
        Check(resumedBoard.MovePiece(0,6,0,5,PlayerType.PlayerTwo) == ShogiBoard.MoveResult.NormalMove &&
            resumedBoard.SetPiece(2,4,PieceType.Kyosya,PlayerType.PlayerOne) == ShogiBoard.MoveResult.NormalMove,
            "Native board accepts a move and a captured-piece drop before leaving the match");
        SceneManager.LoadScene("Main"); yield return new WaitForSecondsRealtime(.8f); StartDuel(new Gin(PlayerType.PlayerTwo)); yield return new WaitForSecondsRealtime(3.6f);
        One().playerStatus.TakeDamage(10000); Two().playerStatus.TakeDamage(10000); yield return new WaitForSecondsRealtime(.2f); Shot("draw");
        var drawBoard = Find<ShogiBoard>();
        Check(drawBoard.GetPieceAt(4,4) != null && drawBoard.GetPieceAt(4,5) != null && drawBoard.GetCapturedPieces(PlayerType.PlayerOne).Count == 0 && drawBoard.GetCapturedPieces(PlayerType.PlayerTwo).Count == 0, "Native simultaneous deaths preserve both pieces without captures");
        yield return new WaitForSecondsRealtime(2.4f);
        Check(Find<GameStateHolder>().CurrentState == State.PlayerTwoTurn && !App.UI.GameSessionUI.BlocksGameplay, "Native draw ends the attacker turn and resumes the board");

        SceneManager.LoadScene("Main"); yield return new WaitForSecondsRealtime(.8f);
        StartDuel(new King(PlayerType.PlayerTwo), new Fuhyo(PlayerType.PlayerOne)); yield return new WaitForSecondsRealtime(3.6f);
        var pawn = One(); var king = Two();
        Place(king, origin, 0); Place(pawn, origin + Vector3.forward * 12f, 180);
        int startingHp = pawn.playerStatus.Hp.Current;
        yield return Tap(king, GamepadButton.East);
        Check(UnityEngine.Object.FindObjectsByType<KingTurret>(FindObjectsSortMode.None).Length == 3, "Native king deploys its three support turrets");
        yield return new WaitForSecondsRealtime(3.8f);
        int supportDamage = startingHp - pawn.playerStatus.Hp.Current;
        Check(supportDamage > 0 && supportDamage <= 72 && pawn.playerStatus.Hp.Current > 0,
            $"Native unguarded pawn survives the whole turret deployment ({supportDamage} damage)");

        Place(pawn, origin, 0);
        yield return null; yield return new WaitForEndOfFrame();
        var spear = pawn.WeaponObject.GetComponent<BoxCollider>();
        var contact = spear.transform.TransformPoint(spear.center);
        Place(king, new Vector3(contact.x, origin.y, contact.z), 180);
        var kingHp = king.playerStatus.Hp;
        Check(kingHp.Current == 160, "Native king starts the spear exchange at full health");
        Input(king, new GamepadState {leftTrigger=1}); yield return new WaitForSecondsRealtime(.10f); Input(king, new GamepadState());
        Input(pawn, new GamepadState {rightTrigger=1}); yield return new WaitForSecondsRealtime(.45f); Input(pawn, new GamepadState());
        Check(kingHp.Current == 160, "Native guarded spear contact removes no HP");
        yield return new WaitForSecondsRealtime(.75f);
        for (int hit = 1; hit <= 5; hit++)
        {
            Input(pawn, new GamepadState { rightTrigger = 1 });
            yield return new WaitForSecondsRealtime(.45f);
            Input(pawn, new GamepadState());
            Check(kingHp.Current == 160 - 32 * hit, $"Native spear thrust {hit} leaves the king at {kingHp.Current} HP");
            yield return new WaitForSecondsRealtime(.65f);
        }
        Shot("king-victory"); yield return new WaitForSecondsRealtime(.2f);
        Check(Find<GameStateHolder>().CurrentState == State.PlayerOneWin, "Native king defeat ends the match with the correct winner");
        SceneManager.LoadScene("Title"); yield return new WaitForSecondsRealtime(.6f); Shot("title"); yield return new WaitForSecondsRealtime(.2f);
        yield return CheckRecords(supportDamage);
        Finish();
    }

    static MatchRecord[] ReadMatches() => Directory.GetFiles(PlayDataFiles.RecordsDirectory, "*.json")
        .Select(file => JsonUtility.FromJson<MatchRecord>(File.ReadAllText(file))).ToArray();

    static IEnumerator WaitForExport(UnityEngine.UI.Button button)
    {
        float deadline = Time.realtimeSinceStartup + 10f;
        while (!button.interactable && Time.realtimeSinceStartup < deadline) yield return null;
    }

    IEnumerator CheckRecords(int supportDamage)
    {
        var matches = ReadMatches();
        Check(matches.Length == 3 && matches.Select(m => m.id).Distinct().Count() == 3 &&
            matches.All(m => m.device1 == "gamepad" && m.device2 == "gamepad"), "Native disk records separate three matches and both gamepad seats");
        var ranged = matches.Single(m => m.duels.Single().seats[1].piece == "香車");
        var duel = ranged.duels[0];
        Check(ranged.result == "interrupted" && ranged.moves == 3 && ranged.drops == 1 && duel.move == 1 && duel.attackingSeat == 1 &&
            duel.result == "1P", "Native interrupted match retains its completed duel, turns and drop");
        Check(duel.seats[0].attacks == 1 && duel.seats[0].hits == 1 && duel.seats[0].primaryDamage == 84 &&
            duel.seats[1].attacks == 2 && duel.seats[1].hits == 1 && duel.seats[1].primaryDamage == 36 &&
            duel.seats[1].hitDistanceSum > 0, "Native history includes a miss and excludes the rejected cooldown press");
        Check(matches.Single(m => m.duels[0].result == "draw").result == "interrupted", "Native draw does not become a win in saved history");
        var winner = matches.Single(m => m.result == "1P");
        var pawn = winner.duels[0].seats[0]; var king = winner.duels[0].seats[1];
        Check(pawn.hits == 6 && pawn.attacks == 6 && pawn.primaryDamage == 160 && king.hitsBlocked == 1 &&
            king.damageTaken == 160 && king.hpLeft == 0, "Native history distinguishes contact, guard and effective damage");
        Check(king.skillUses == 1 && king.attacks == 0 && king.turretShots >= king.turretHits && king.turretHits > 0 &&
            king.turretDamage == supportDamage && pawn.damageTaken == supportDamage && pawn.hpLeft == pawn.hpMax - supportDamage,
            "Native turret activity stays separate from personal accuracy and matches HP lost");

        // Join a new match through the lobby with a different device pair. Start
        // a second duel from the other side, then leave while it is still active.
        var keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
        if (Mouse.current == null) InputSystem.AddDevice<Mouse>();
        App.UI.GameSessionUI.Instance.StartMatch();
        yield return new WaitForSecondsRealtime(.4f);
        foreach (var device in new InputDevice[] {keyboard, second, keyboard, second})
        {
            var tap = SelectionTestInput.Tap(device);
            while (tap.MoveNext()) yield return new WaitForSecondsRealtime((float)(double)tap.Current);
        }
        yield return new WaitForSecondsRealtime(1.5f);
        var dragon = new Hisya(PlayerType.PlayerOne);
        dragon.Promote();
        StartDuel(new King(PlayerType.PlayerTwo), dragon);
        yield return new WaitForSecondsRealtime(3.6f);
        var archer = One(); var defender = Two();
        var range = new Vector3(3000, 200, 2000);
        Place(archer, range, 0); Place(defender, range + Vector3.forward * 8, 180);
        // Compare two draw lengths against the same stationary target. In the
        // weightless fixture the first physical arrow otherwise pushes it away.
        defender.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
        var mouse = archer.GetComponent<PlayerInput>().devices.OfType<Mouse>().Single();
        int before = defender.playerStatus.Hp.Current;
        InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
        InputSystem.QueueStateEvent(mouse, new MouseState());
        yield return new WaitForSecondsRealtime(.45f);
        Check(defender.playerStatus.Hp.Current == before - 64, "Native mouse tap releases a 64-damage dragon arrow");
        before = defender.playerStatus.Hp.Current;
        InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
        InputSystem.QueueStateEvent(mouse, new MouseState());
        yield return new WaitForSecondsRealtime(.1f);
        float recovery = archer.PrimaryCooldown;
        InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
        yield return new WaitForSecondsRealtime(recovery + .66f);
        InputSystem.QueueStateEvent(mouse, new MouseState());
        yield return new WaitForSecondsRealtime(.35f);
        int partialDamage = before - defender.playerStatus.Hp.Current;
        Check(partialDamage >= 75 && partialDamage <= 86, $"Native holding through recovery releases one partially drawn arrow ({partialDamage} damage)");
        One().playerStatus.TakeDamage(10000);
        yield return new WaitForSecondsRealtime(2.7f);
        var board = Find<ShogiBoard>(); var grid = board.GetBoard();
        Array.Clear(grid, 0, grid.Length);
        grid[4,4] = new Keima(PlayerType.PlayerOne); grid[4,4].Promote();
        grid[4,5] = new Hisya(PlayerType.PlayerTwo); grid[4,5].Promote();
        Check(board.MovePiece(4,5,4,4,PlayerType.PlayerTwo) == ShogiBoard.MoveResult.DuelMove,
            "Native second seat starts a duel between promoted pieces");
        yield return new WaitForSecondsRealtime(3.6f);
        archer = Two(); defender = One();
        Place(archer, range, 0); Place(defender, range + Vector3.forward * 8, 180);
        Input(archer, new GamepadState { rightTrigger = 1 });
        yield return new WaitForSecondsRealtime(1.45f);
        Check(GameObject.Find("P2 bow draw track") != null && GameObject.Find("P1 bow draw track") == null,
            "Native second-seat charge gauge is independent");
        Input(archer, new GamepadState());
        yield return new WaitForSecondsRealtime(.35f);
        App.UI.GameSessionUI.Instance.ReturnTitle();
        yield return new WaitForSecondsRealtime(.8f);
        var mixed = ReadMatches().Single(m => m.device1 == "keyboard_mouse");
        var last = mixed.duels.Last();
        Check(mixed.device2 == "gamepad" && mixed.result == "interrupted" && mixed.moves == 2 &&
            mixed.duels.Count == 2 && mixed.duels[0].result == "2P" && last.result == "interrupted" && last.number == 2 &&
            last.move == 2 && last.attackingSeat == 2 && last.seats.All(s => s.promoted) &&
            last.seats[0].hpLeft == last.seats[0].hpMax - 96 && last.seats[1].attacks == 1 && last.seats[1].primaryDamage == 96 &&
            last.seats[0].piece == "桂馬" && last.seats[1].piece == "飛車",
            "Native mixed-input history preserves both duels, seat order, promotions and a mid-duel exit");

        // A damaged source record must not hide valid history or break the menu.
        File.WriteAllText(Path.Combine(PlayDataFiles.RecordsDirectory,"damaged.json"), "{");
        App.UI.GameSessionUI.Instance.OpenSettings();
        var button = GameObject.Find("Export play data").GetComponent<UnityEngine.UI.Button>();
        button.onClick.Invoke();
        yield return WaitForExport(button);
        string message = GameObject.Find("Play data status").GetComponent<UnityEngine.UI.Text>().text;
        Check(message.Contains("4局・5決闘") && message.Contains("1件"), "Native settings export all valid records and report a damaged file");
        File.WriteAllText(Path.Combine(output,"play-data-root.txt"), PlayDataFiles.Root);
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
