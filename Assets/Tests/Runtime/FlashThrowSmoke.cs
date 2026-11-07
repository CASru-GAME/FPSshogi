#if FPS_GAMEPLAY_SMOKE
using System;
using System.Collections;
using System.Linq;
using App.Main.Player;
using App.Main.ShogiThings;
using App.PlayData;
using App.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

public sealed partial class GameplayPlayerSmoke
{
    readonly bool flashRun = Environment.GetCommandLineArgs().Contains("-flash-checks");

    static LineRenderer FlashLine(Player player, string name) =>
        player.transform.Find("Flashbang aim/" + name)?.GetComponent<LineRenderer>();

    static IEnumerator PressKey(Keyboard keyboard, Key key)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
        yield return new WaitForSecondsRealtime(.08f);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return new WaitForSecondsRealtime(.08f);
    }

    IEnumerator CheckFlashThrows()
    {
        var promoted = new Kyosya(PlayerType.PlayerTwo);
        promoted.Promote();
        yield return Matchup(new Kyosya(PlayerType.PlayerOne), promoted);
        var caster = One();
        var target = Two();
        Place(caster, ArenaOrigin, 0);
        Place(target, ArenaOrigin + new Vector3(3f, 0f, 21f), 180);
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.position = ArenaOrigin + Vector3.forward * 22f;
        wall.transform.localScale = new Vector3(40f, 30f, .1f);
        var field = GameObject.CreatePrimitive(PrimitiveType.Cube);
        field.GetComponent<Collider>().isTrigger = true;
        field.GetComponent<Renderer>().enabled = false;
        field.transform.position = ArenaOrigin + Vector3.forward * 3f;
        field.transform.localScale = new Vector3(8f, 8f, .1f);
        Physics.SyncTransforms();

        yield return Tap(caster, GamepadButton.East);
        var line = FlashLine(caster, "Trajectory");
        var ring = FlashLine(caster, "Blast radius");
        Check(line != null && ring != null && caster.SkillCooldown == 0f && Find<GrenadeProjectile>() == null,
            "B prepares a flashbang with both guides, zero cooldown and no projectile");
        if (line == null || ring == null)
        {
            yield break;
        }
        var camera = caster.GetComponentInChildren<Camera>();
        var otherCamera = target.GetComponentInChildren<Camera>();
        bool Private(LineRenderer guide) => (camera.cullingMask & 1 << guide.gameObject.layer) != 0 &&
            (otherCamera.cullingMask & 1 << guide.gameObject.layer) == 0;
        Check(Private(line) && Private(ring) && ring.loop && line.positionCount >= 3,
            "The predicted arc and blast circle are visible only through the thrower's camera");
        Vector3 EndPoint() => line.GetPosition(line.positionCount - 1);
        Vector3 impact = EndPoint();
        Check(impact.z > ArenaOrigin.z + 20f && impact.z < wall.transform.position.z &&
            Mathf.Abs(Vector3.Distance(ring.GetPosition(0), ring.GetPosition(ring.positionCount / 2)) - 10f) < .1f,
            "Preview ignores ability triggers and shows a five-metre blast radius at the thin wall");
        Check(Mathf.Abs(ring.GetPosition(0).y - ring.GetPosition(ring.positionCount / 4).y) < .05f,
            "The radius at a vertical wall remains readable across the board");
        Set(caster, "pitch", 15f);
        yield return SettleHud();
        Check(EndPoint().y < impact.y - 3f, "Aiming down moves the prediction before throwing");
        Set(caster, "pitch", 0f);
        yield return SettleHud();
        Check(GameObject.Find("P1 throw controls") != null && GameObject.Find("P2 throw controls") == null,
            "Only the prepared gamepad seat shows throw and cancel controls");
        yield return Tap(caster, GamepadButton.East);
        Check(FlashLine(caster, "Trajectory") == null && Find<GrenadeProjectile>() == null && caster.SkillCooldown == 0f,
            "Pressing B again cancels without spending the ability");

        yield return Tap(caster, GamepadButton.East);
        yield return Tap(target, GamepadButton.East);
        Check(FlashLine(caster, "Trajectory") != null && FlashLine(target, "Trajectory") != null,
            "Both controller seats can prepare independently");
        yield return Tap(target, GamepadButton.East);
        Check(FlashLine(caster, "Trajectory") != null && FlashLine(target, "Trajectory") == null && target.SkillCooldown == 0f,
            "The promoted opponent's cancel leaves the first seat aiming");
        line = FlashLine(caster, "Trajectory");
        impact = EndPoint();
        int hp = target.playerStatus.Hp.Current;
        Input(caster, Shoot);
        float sent = Time.time;
        var timeout = new WaitForSecondsRealtime(.06f);
        yield return timeout;
        var projectile = Find<GrenadeProjectile>();
        Check(projectile != null && caster.SkillCooldown > 7f && caster.PrimaryCooldown == 0f && FlashLine(caster, "Trajectory") == null,
            "RT releases the grenade once, preserves the short gun, and starts the eight-second cooldown");
        Input(caster, new GamepadState());
        GameObject burst = null;
        while (Time.time - sent < 2.4f && (burst = GameObject.Find("FlashbangBurst")) == null) yield return null;
        float flight = Time.time - sent;
        Check(burst != null && Vector3.Distance(burst.transform.position, impact) < .12f && flight < .6f,
            $"The faster grenade detonates at the marked wall ({flight:F2} s for 21 m)");
        yield return SettleHud();
        var victimFlash = GameObject.Find("P2 flashbang")?.GetComponent<Image>();
        var throwerFlash = GameObject.Find("P1 flashbang")?.GetComponent<Image>();
        Check(victimFlash != null && victimFlash.color.a > .75f && !victimFlash.raycastTarget &&
            victimFlash.transform.IsChildOf(GameObject.Find("P2 viewport HUD").transform) &&
            (throwerFlash == null || throwerFlash.color.a == 0f) &&
            target.playerStatus.Hp.Current == hp && Mathf.Abs(target.playerStatus.MoveSpeed.Current - 1.38f) < .015f,
            "Only the nearby victim is blinded and slowed to 20 percent without losing HP");
        var flash = target.playerStatus.EffectList.GetEffect<FlashBangEffect>();
        GameSessionUI.Instance.OpenPause();
        float pausedTime = flash?.lastTime ?? 0f;
        yield return new WaitForSecondsRealtime(.25f);
        Check(flash != null && flash.lastTime == pausedTime && victimFlash != null && !victimFlash.gameObject.activeInHierarchy,
            "Opening the menu hides the flash and freezes the remaining blind time");
        GameSessionUI.Instance.Resume();
        yield return new WaitForSeconds(2.02f);
        Check(Mathf.Abs(target.playerStatus.MoveSpeed.Current - 6.9f) < .01f &&
            (victimFlash == null || !victimFlash.gameObject.activeInHierarchy || victimFlash.color.a == 0f),
            "Both vision and speed recover after two seconds");
        yield return Tap(caster, GamepadButton.East);
        Check(caster.SkillCooldown > 0f && FlashLine(caster, "Trajectory") == null, "Cooldown prevents a second preparation");
        PlayRecorder.Current.Save();
        var seats = ReadMatches().SelectMany(match => match.duels).Single().seats;
        Check(seats[0].skillUses == 1 && seats[0].attacks == 0 && seats[1].skillUses == 0,
            "Only the actual throw is recorded, separately from gunshots and cancellations");

        yield return Tap(target, GamepadButton.East);
        yield return Tap(target, GamepadButton.Start);
        Check(GameSessionUI.BlocksGameplay && FlashLine(target, "Trajectory") == null && target.SkillCooldown == 0f,
            "Pausing clears the aim guide without spending the promoted lance's ability");
        yield return Tap(target, GamepadButton.Start);
        Place(target, ArenaOrigin + Vector3.right * 100f, 0f);
        Place(caster, ArenaOrigin + Vector3.right * 120f, 0f);
        yield return Tap(target, GamepadButton.East);
        var airArc = FlashLine(target, "Trajectory");
        var airEnd = airArc.GetPosition(airArc.positionCount - 1);
        Place(caster, airEnd + Vector3.right * 7f, 180f);
        var trigger = new GameObject("Remote ability trigger").AddComponent<BoxCollider>();
        trigger.transform.SetParent(caster.transform, false);
        trigger.transform.position = airEnd;
        trigger.isTrigger = true;
        Physics.SyncTransforms();
        Input(target, Shoot);
        sent = Time.time;
        yield return new WaitForSecondsRealtime(.07f);
        Input(target, new GamepadState());
        while (Time.time - sent < 2.4f && (burst = GameObject.Find("FlashbangBurst")) == null) yield return null;
        Check(burst != null && Mathf.Abs(Time.time - sent - 2f) < .12f &&
            Vector3.Distance(airEnd, burst.transform.position) < .12f && caster.playerStatus.EffectList.GetEffect<FlashBangEffect>() == null,
            "The fuse bursts at the airborne marker after two seconds and a remote trigger cannot blind its owner");
        Destroy(trigger.gameObject);
        while (caster.SkillCooldown > 0f) yield return null;
        yield return Tap(caster, GamepadButton.East);
        One().playerStatus.TakeDamage(10000);
        yield return new WaitForSecondsRealtime(2.7f);
        Check(FlashLine(caster, "Trajectory") == null && Find<GrenadeProjectile>() == null, "Duel end clears prepared throws");

        var keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
        if (Mouse.current == null) InputSystem.AddDevice<Mouse>();
        GameSessionUI.Instance.ReturnTitle();
        yield return new WaitForSecondsRealtime(.8f);
        GameSessionUI.Instance.StartMatch();
        yield return new WaitForSecondsRealtime(.4f);
        foreach (var device in new InputDevice[] { keyboard, second, keyboard, second })
        {
            var tap = SelectionTestInput.Tap(device);
            while (tap.MoveNext()) yield return new WaitForSecondsRealtime((float)(double)tap.Current);
        }
        yield return new WaitForSecondsRealtime(1.5f);
        yield return Matchup(new Kyosya(PlayerType.PlayerOne), new Gin(PlayerType.PlayerTwo));
        caster = One(); target = Two();
        Place(caster, ArenaOrigin, 0);
        Place(target, ArenaOrigin + Vector3.forward * 16f, 180);
        var mouse = caster.GetComponent<PlayerInput>().devices.OfType<Mouse>().Single();
        yield return PressKey(keyboard, Key.E);
        Check(GameObject.Find("P1 throw controls")?.GetComponent<Text>()?.text.Contains("左") == true,
            "Keyboard aim shows the mouse throw binding");
        yield return PressKey(keyboard, Key.E);
        Check(caster.SkillCooldown == 0 && FlashLine(caster, "Trajectory") == null, "A second E cancels for free");
        yield return PressKey(keyboard, Key.E);
        InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
        yield return new WaitForSecondsRealtime(.05f);
        InputSystem.QueueStateEvent(mouse, new MouseState());
        yield return new WaitForSecondsRealtime(.45f);
        Check(caster.SkillCooldown > 7f && caster.PrimaryCooldown == 0f && target.playerStatus.EffectList.GetEffect<FlashBangEffect>() != null,
            "A mouse click throws at the opponent and consumes no gunshot");
        InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
        yield return new WaitForSecondsRealtime(.06f);
        InputSystem.QueueStateEvent(mouse, new MouseState());
        Check(caster.PrimaryCooldown > 0f, "The next mouse click fires the short gun normally");
        while (caster.SkillCooldown > 0f) yield return null;
        var closeWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var eye = caster.GetComponentInChildren<Camera>().transform;
        closeWall.transform.position = eye.position + eye.forward * .26f;
        closeWall.transform.rotation = eye.rotation;
        closeWall.transform.localScale = new Vector3(8f, 8f, .025f);
        // Keep the fighter at the intended eye position while testing the projectile sweep.
        Physics.IgnoreCollision(caster.GetComponent<Collider>(), closeWall.GetComponent<Collider>());
        Physics.SyncTransforms();
        yield return PressKey(keyboard, Key.E);
        var closeArc = FlashLine(caster, "Trajectory");
        var closeEnd = closeArc.GetPosition(closeArc.positionCount - 1);
        InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
        yield return new WaitForSecondsRealtime(.06f);
        InputSystem.QueueStateEvent(mouse, new MouseState());
        burst = GameObject.Find("FlashbangBurst");
        float stopDistance = Vector3.Distance(closeEnd, eye.position);
        float miss = burst != null ? Vector3.Distance(burst.transform.position, closeEnd) : float.PositiveInfinity;
        Check(burst != null && Vector3.Distance(burst.transform.position, closeEnd) < .08f &&
            stopDistance < .3f && caster.playerStatus.EffectList.GetEffect<FlashBangEffect>() != null,
            $"Cover between the eye and hand catches the grenade and flashes the thrower (stopped at {stopDistance:F2} m, marker error {miss:F2} m)");
    }
}
#endif
