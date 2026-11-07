#if FPS_GAMEPLAY_SMOKE
using System;
using System.Collections;
using System.Linq;
using App.Main.GameMaster;
using App.Main.Player;
using App.Main.ShogiThings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

public sealed partial class GameplayPlayerSmoke
{
    readonly bool matchupRun = Environment.GetCommandLineArgs().Contains("-matchup-checks");
    static readonly Vector3 ArenaOrigin = new Vector3(3000, 200, 2000);
    static readonly GamepadState Shoot = new GamepadState { rightTrigger = 1 };
    static readonly GamepadState Forward = new GamepadState { leftStick = Vector2.up };

    IEnumerator Matchup(IPiece firstPiece, IPiece secondPiece)
    {
        // A held stick from the previous duel correctly keeps the next board's
        // input gate closed. Release both devices just as the players would.
        InputSystem.QueueStateEvent(first,new GamepadState());
        InputSystem.QueueStateEvent(second,new GamepadState());
        yield return null;
        SceneManager.LoadScene("Main");
        yield return new WaitForSecondsRealtime(.7f);
        var board = Find<ShogiBoard>();
        var grid = board.GetBoard();
        Array.Clear(grid, 0, grid.Length);
        var delta = firstPiece.Movement.First(m => Mathf.Abs(m[0]) <= 2 && Mathf.Abs(m[1]) <= 2);
        int x = 4 + delta[0], y = 4 + delta[1];
        grid[4,4] = firstPiece; grid[x,y] = secondPiece;
        Check(board.MovePiece(4,4,x,y,PlayerType.PlayerOne) == ShogiBoard.MoveResult.DuelMove,
            $"Board starts {firstPiece.Type} vs {secondPiece.Type}");
        yield return new WaitForSecondsRealtime(3.65f);
    }

    // A clear stretch of floor gives repeatable distances without removing gravity,
    // body collision, the equipped weapons or either player's actual input pairing.
    static void Arena(Player rear, Player front, float gap, bool sameHeading = false)
    {
        Stand(rear, ArenaOrigin, 0f);
        Stand(front, ArenaOrigin + Vector3.forward * gap, sameHeading ? 0f : 180f);
        float feet = rear.GetComponent<Collider>().bounds.min.y;
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Matchup test floor";
        floor.transform.position = new Vector3(ArenaOrigin.x,feet - .52f,ArenaOrigin.z + 30f);
        floor.transform.localScale = new Vector3(100f,1f,160f);
        Physics.SyncTransforms();
    }

    static void Stand(Player fighter, Vector3 position, float yaw)
    {
        Place(fighter, position, yaw);
        fighter.GetComponent<Rigidbody>().useGravity = true;
    }

    static void AimAt(Player shooter, Player target)
    {
        var view = shooter.GetComponentInChildren<Camera>().transform;
        Vector3 delta = target.GetComponent<Collider>().bounds.center - view.position;
        var aim = Quaternion.LookRotation(delta).eulerAngles;
        float pitch = Mathf.DeltaAngle(0f, aim.x);
        Set(shooter,"yaw",aim.y); Set(shooter,"pitch",pitch);
        shooter.transform.rotation = Quaternion.Euler(0f,aim.y,0f);
        view.localRotation = Quaternion.Euler(pitch,0f,0f);
        Physics.SyncTransforms();
    }

    static float GroundSpeed(Player fighter)
    {
        var velocity = fighter.GetComponent<Rigidbody>().linearVelocity;
        return new Vector2(velocity.x,velocity.z).magnitude;
    }

    static IEnumerator FireOnce(Player shooter)
    {
        Input(shooter,Shoot);
        yield return new WaitForSecondsRealtime(.08f);
        Input(shooter,new GamepadState());
        yield return null;
    }

    IEnumerator CheckMatchups()
    {
        yield return SpearVsFlight();
        yield return ShortGunVsFlight(false);
        yield return ShortGunVsFlight(true);
        yield return KnightChasesBow();
        yield return GuardMatchups();
    }

    IEnumerator SpearVsFlight()
    {
        yield return Matchup(new Fuhyo(PlayerType.PlayerOne),new Kakugyo(PlayerType.PlayerTwo));
        var pawn = One(); var bishop = Two();
        Arena(pawn,bishop,4f);
        yield return new WaitForSecondsRealtime(.2f);
        var spear = pawn.WeaponObject.GetComponent<BoxCollider>();
        var thrust = spear.transform.TransformPoint(spear.center);
        var spot = new Vector3(thrust.x,pawn.transform.position.y,thrust.z);
        Stand(bishop,spot,180f);
        int full = bishop.playerStatus.Hp.Current;
        yield return FireOnce(pawn);
        yield return new WaitForSecondsRealtime(.98f);
        int grounded = bishop.playerStatus.Hp.Current;
        Check(full - grounded == 32,"Grounded bishop is within normal spear reach");
        Stand(bishop,spot,180f);
        float surface = bishop.transform.position.y;
        Input(bishop,new GamepadState().WithButton(GamepadButton.East));
        yield return new WaitForSecondsRealtime(.4f);
        Input(bishop,new GamepadState { leftStick = Vector2.right });
        yield return new WaitForSecondsRealtime(.18f);
        float height = bishop.transform.position.y - surface;
        float airborneSpeed = GroundSpeed(bishop);
        Input(bishop,new GamepadState());
        yield return FireOnce(pawn);
        yield return new WaitForSecondsRealtime(.4f);
        Check(height > 6.5f && height < 9f && Mathf.Abs(airborneSpeed - 1.75f) < .12f &&
            bishop.playerStatus.Hp.Current == grounded,
            $"Airborne bishop escapes the same thrust at {height:F1} m, lateral speed {airborneSpeed:F2} m/s");
        float deadline = Time.realtimeSinceStartup + 5f;
        while (bishop.transform.position.y > surface + .15f && Time.realtimeSinceStartup < deadline) yield return null;
        yield return FireOnce(pawn);
        yield return new WaitForSecondsRealtime(.4f);
        Check(bishop.playerStatus.Hp.Current == grounded - 32,"Pawn connects again after the bishop lands");
        Input(bishop,Forward);
        yield return new WaitForSecondsRealtime(.2f);
        Check(Mathf.Abs(GroundSpeed(bishop) - 5f) < .12f,"Bishop regains ground speed after flight");
    }

    IEnumerator ShortGunVsFlight(bool promoted)
    {
        var bishopPiece = new Kakugyo(promoted ? PlayerType.PlayerTwo : PlayerType.PlayerOne);
        var lancePiece = new Kyosya(promoted ? PlayerType.PlayerOne : PlayerType.PlayerTwo);
        if (promoted) { bishopPiece.Promote(); lancePiece.Promote(); }
        IPiece attacker = promoted ? (IPiece)lancePiece : bishopPiece;
        IPiece defender = promoted ? (IPiece)bishopPiece : lancePiece;
        yield return Matchup(attacker,defender);
        var bishop = promoted ? Two() : One(); var lance = promoted ? One() : Two();
        var flyer = bishop.playerStatus; var gunner = lance.playerStatus;
        Arena(lance,bishop,62.5f);
        yield return new WaitForSecondsRealtime(.18f);
        AimAt(lance,bishop);
        yield return FireOnce(lance);
        AimAt(bishop,lance);
        yield return FireOnce(bishop);
        Check(flyer.Hp.Current == flyer.Hp.Max && gunner.Hp.Current < gunner.Hp.Max,
            $"At 62.5 m only the bishop reaches the opponent (promoted={promoted})");
        while (bishop.PrimaryCooldown > 0f) yield return null;
        Stand(bishop,ArenaOrigin + Vector3.forward * 18f,180f);
        float surface = bishop.transform.position.y;
        var side = new GamepadState { leftStick = Vector2.right };
        Input(bishop,side.WithButton(GamepadButton.East));
        yield return new WaitForSecondsRealtime(.38f);
        Input(bishop,side);
        float drift = GroundSpeed(bishop);
        int counterHit = gunner.Hp.Current;
        float firstShot = Time.time;
        float lastHeight = 0f;
        int hitCount = 0;
        for (int shot = 0; shot < 3; shot++)
        {
            while (lance.PrimaryCooldown > 0f) yield return null;
            lastHeight = bishop.transform.position.y - surface;
            int before = flyer.Hp.Current;
            AimAt(lance,bishop);
            yield return FireOnce(lance);
            if (before > 0 && before - flyer.Hp.Current == Mathf.Min(before,promoted ? 42 : 36)) hitCount++;
            if (shot == 0)
            {
                AimAt(bishop,lance);
                Input(bishop,new GamepadState { leftStick = Vector2.right, rightTrigger = 1 });
                yield return new WaitForSecondsRealtime(.08f);
                Input(bishop,side);
            }
        }
        Check(hitCount == 3 && flyer.Hp.Current == 0 && lastHeight > 6.5f &&
            Mathf.Abs(drift - (promoted ? 1.89f : 1.75f)) < .15f && gunner.Hp.Current > 0 && gunner.Hp.Current < counterHit,
            $"Three aimed short-gun shots defeat a moving, firing flyer in {Time.time-firstShot:F2}s; height {lastHeight:F1} m, hits {hitCount}, promoted={promoted}");
    }

    IEnumerator KnightChasesBow()
    {
        yield return Matchup(new Hisya(PlayerType.PlayerOne),new Keima(PlayerType.PlayerTwo));
        var archer = One(); var knight = Two();
        Arena(knight,archer,17f,true);
        Input(archer,new GamepadState { leftStick = Vector2.up, rightTrigger = 1 });
        Input(knight,Forward);
        yield return new WaitForSecondsRealtime(1.4f);
        float bowSpeed = GroundSpeed(archer), knightSpeed = GroundSpeed(knight);
        float gap = Vector3.Distance(archer.transform.position,knight.transform.position);
        int hp = archer.playerStatus.Hp.Current;
        Input(knight,Forward.WithButton(GamepadButton.East));
        yield return new WaitForSecondsRealtime(.48f);
        Check(archer.BowDrawAmount == 1f && Mathf.Abs(bowSpeed - 4.35f) < .12f && Mathf.Abs(knightSpeed - 8.2f) < .12f &&
            gap < 13f && archer.playerStatus.Hp.Current < hp,
            $"Knight closes on retreating full-draw rook ({knightSpeed:F2} vs {bowSpeed:F2} m/s) and lands its dash");
        Input(archer,Forward);
        Input(knight,new GamepadState());
        yield return new WaitForSecondsRealtime(.22f);
        Check(Mathf.Abs(GroundSpeed(archer) - 5.8f) < .15f,"Rook recovers normal speed on release");
    }

    IEnumerator GuardMatchups()
    {
        yield return Matchup(new Kin(PlayerType.PlayerOne),new Kyosya(PlayerType.PlayerTwo));
        var gold = One(); var lance = Two();
        Arena(lance,gold,18f);
        yield return Tap(gold,GamepadButton.East);
        int start = gold.playerStatus.Hp.Current;
        AimAt(lance,gold); yield return FireOnce(lance);
        int armored = gold.playerStatus.Hp.Current;
        yield return new WaitForSecondsRealtime(3.6f);
        AimAt(lance,gold); yield return FireOnce(lance);
        Check(start - armored == 20 && armored - gold.playerStatus.Hp.Current == 36,
            "Gold reduces the same lance hit from 36 to 20 only during armor");

        yield return Matchup(new Gin(PlayerType.PlayerOne),new Hisya(PlayerType.PlayerTwo));
        var silver = One(); var archer = Two();
        Arena(archer,silver,12f);
        silver.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
        AimAt(archer,silver);
        Input(archer,Shoot);
        yield return new WaitForSecondsRealtime(1.25f);
        yield return Tap(silver,GamepadButton.East);
        Input(archer,new GamepadState());
        yield return new WaitForSecondsRealtime(.38f);
        bool blocked = silver.playerStatus.ImpactBlocked && silver.playerStatus.Hp.Current == silver.playerStatus.Hp.Max;
        while (archer.PrimaryCooldown > 0f) yield return null;
        Input(archer,Shoot);
        yield return new WaitForSecondsRealtime(1.25f);
        Input(archer,new GamepadState());
        yield return new WaitForSecondsRealtime(.38f);
        Check(blocked && silver.playerStatus.Hp.Max - silver.playerStatus.Hp.Current == 84,
            "Silver can time its guard to stop a full-draw arrow; the next unguarded arrow deals 84");
    }
}
#endif
