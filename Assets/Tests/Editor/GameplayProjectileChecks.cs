using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using App.Main.Player;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Invoked by the gameplay runner in Play Mode. No menu or automatic execution.
public static class GameplayProjectileChecks
{
    static readonly Vector3 Origin = new Vector3(2000f, 200f, 2000f);
    const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    public static IEnumerator Run(Action<string, bool> check)
    {
        foreach (bool longGun in new[] { false, true })
        {
            string label = longGun ? "Long gun" : "Short gun";
            int fullDamage = longGun ? 20 : 60;
            CheckGun(check, longGun, label + " damages once and respects cooldown", null, fullDamage);
            CheckGun(check, longGun, label + " passes through a slow-field trigger", "trigger", fullDamage);
            CheckGun(check, longGun, label + " ignores its owner's colliders", "owner", fullDamage);
            CheckGun(check, longGun, label + " is stopped by a solid wall", "wall", 0);
            CheckGun(check, longGun, label + " retains linear distance falloff", "falloff", longGun ? 15 : 45);
            CheckGun(check, longGun, label + " cannot hit beyond its range", "range", 0);
        }

        CheckTurret(check, "Turret hits once per firing interval", null, 20);
        CheckTurret(check, "Turret passes through a slow-field trigger", "trigger", 20);
        CheckTurret(check, "Turret is stopped by a solid wall", "wall", 0);
        CheckPausedTurret(check);

        using (var fixture = new Fixture())
        {
            var owner = fixture.Player("Arrow owner", Origin);
            var target = fixture.Player("Arrow target", Origin + Vector3.forward * 5f);
            var extra = fixture.Collider("Second target collider", target.transform.position);
            extra.transform.SetParent(target.transform, true);
            var arrow = fixture.Arrow(owner);
            Hit(arrow, target.GetComponent<Collider>());
            Hit(arrow, extra);
            check("One arrow damages a multi-collider target only once", target.playerStatus.Hp.Current == 980);
        }

        using (var fixture = new Fixture())
        {
            var owner = fixture.Player("Arrow owner", Origin);
            var target = fixture.Player("Arrow target", Origin + Vector3.forward * 5f);
            var arrow = fixture.Arrow(owner);
            Hit(arrow, owner.GetComponent<Collider>());
            Hit(arrow, target.GetComponent<Collider>());
            check("Arrow owner immunity does not consume the shot", owner.playerStatus.Hp.Current == 1000 && target.playerStatus.Hp.Current == 980);
        }

        using (var fixture = new Fixture())
        {
            var owner = fixture.Player("Arrow owner", Origin);
            var target = fixture.Player("Arrow target", Origin + Vector3.forward * 5f);
            var arrow = fixture.Arrow(owner);
            Hit(arrow, fixture.Collider("Solid wall", Origin + Vector3.forward * 2f));
            Hit(arrow, target.GetComponent<Collider>());
            check("An arrow stopped by a wall cannot damage another collider in the same frame", target.playerStatus.Hp.Current == 1000);
        }

        using (var fixture = new Fixture())
        {
            var owner = fixture.Player("Arrow owner", Origin);
            var arrow = fixture.Arrow(owner);
            var trigger = fixture.Collider("Slow-field trigger", Origin + Vector3.forward * 2f);
            trigger.isTrigger = true;
            Hit(arrow, trigger);
            yield return .15d;
            check("A slow-field trigger does not destroy an arrow", arrow != null);
        }

        using (var fixture = new Fixture())
        {
            var owner = fixture.Player("Bow owner", Origin);
            var target = fixture.Player("Bow target", Origin + Vector3.forward * 5f);
            var bow = new BowPrimary();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/App/Prefabs/MainWeapons/Arrow.prefab");
            check("The live bow arrow prefab is available", prefab != null);
            if (prefab != null)
            {
                owner.SetSubWeaponObject(prefab);
                bow.PrimaryAction(owner, owner.playerStatus);
                var arrow = fixture.OwnedArrow(owner);
                check("Bow creates the configured Rigidbody projectile", arrow != null && arrow.GetComponent<Rigidbody>() != null);
                bow.PrimaryAction(owner, owner.playerStatus);
                check("Bow cooldown prevents a second projectile", Object.FindObjectsByType<ProjectileArrow>(FindObjectsSortMode.None).Count(p => p.GetComponent<CombatVisualLifetime>()?.owner == owner) == 1);
                if (arrow != null)
                {
                    arrow.transform.position += Vector3.forward * 5f;
                    Hit(arrow, target.GetComponent<Collider>());
                    check("Rigidbody arrow falloff is measured from its launch point", target.playerStatus.Hp.Current == 980);
                }
            }
        }

        using (var fixture = new Fixture())
        {
            var owner = fixture.Player("Bow physical owner", Origin);
            var target = fixture.Player("Bow physical target", Origin + Vector3.forward * 5f);
            owner.SetSubWeaponObject(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/App/Prefabs/MainWeapons/Arrow.prefab"));
            Physics.SyncTransforms();
            new BowPrimary().PrimaryAction(owner, owner.playerStatus);
            var arrow = fixture.OwnedArrow(owner);
            yield return .45d;
            check($"The live bow projectile leaves its owner and damages a nearby target exactly once (owner HP={owner.playerStatus.Hp.Current}, target HP={target.playerStatus.Hp.Current}, arrow alive={arrow != null})", owner.playerStatus.Hp.Current == 1000 && target.playerStatus.Hp.Current == 980);
        }

        using (var fixture = new Fixture())
        {
            var owner = fixture.Player("Thin-wall bow owner", Origin);
            var target = fixture.Player("Thin-wall bow target", Origin + Vector3.forward * 5f);
            owner.SetSubWeaponObject(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/App/Prefabs/MainWeapons/Arrow.prefab"));
            // Place cover halfway between the first two simulated arrow positions.
            // At speed 40 a discrete step crosses more than the arrow plus this wall's thickness.
            float wallDistance = .5f + 40f * Time.fixedDeltaTime * 1.5f;
            var wall = fixture.Collider("Thin live-projectile cover", Origin + Vector3.forward * wallDistance);
            wall.size = new Vector3(4f, 4f, .02f);
            Physics.SyncTransforms();
            new BowPrimary().PrimaryAction(owner, owner.playerStatus);
            var arrow = fixture.OwnedArrow(owner);
            yield return .45d;
            check($"The live Rigidbody arrow is stopped by thin solid cover (target HP={target.playerStatus.Hp.Current}, arrow alive={arrow != null})",
                arrow == null && target.playerStatus.Hp.Current == 1000);
        }

        using (var fixture = new Fixture())
        {
            var owner = fixture.Player("Transform arrow owner", Origin);
            var target = fixture.Player("Transform arrow target", Origin + Vector3.forward * 5f);
            var go = fixture.GameObject("Transform-only arrow", Origin + Vector3.forward);
            var arrow = go.AddComponent<ProjectileArrow>();
            arrow.Initialize(owner, owner.playerStatus, 1f, 100f, 200f, .5f, ~0);
            Physics.SyncTransforms();
            arrow.Launch(Vector3.forward, 40f, 1f);
            yield return .3d;
            check("Transform-only arrows sweep their movement and hit a target", target.playerStatus.Hp.Current == 980);
        }

        using (var fixture = new Fixture())
        {
            var owner = fixture.Player("Blocked transform arrow owner", Origin);
            var target = fixture.Player("Blocked transform arrow target", Origin + Vector3.forward * 5f);
            var wall = fixture.Collider("Thin solid wall", Origin + Vector3.forward * 2.5f);
            wall.size = new Vector3(4f, 4f, .02f);
            var go = fixture.GameObject("Blocked transform-only arrow", Origin + Vector3.forward);
            var arrow = go.AddComponent<ProjectileArrow>();
            arrow.Initialize(owner, owner.playerStatus, 1f, 100f, 200f, .5f, ~0);
            Physics.SyncTransforms();
            arrow.Launch(Vector3.forward, 40f, 1f);
            yield return .3d;
            check("Transform-only arrows are consumed by a thin solid wall", arrow == null && target.playerStatus.Hp.Current == 1000);
        }

        using (var fixture = new Fixture())
        {
            var owner = fixture.Player("Grenade owner", Origin);
            var target = fixture.Player("Grenade target", Origin + Vector3.forward * 2f);
            var extra = fixture.Collider("Second grenade target collider", target.transform.position);
            extra.transform.SetParent(target.transform, true);
            var go = fixture.GameObject("Flash grenade", Origin + Vector3.forward);
            var grenade = go.AddComponent<GrenadeProjectile>();
            grenade.owner = owner;
            grenade.explosionRadius = 5f;
            Physics.SyncTransforms();
            Invoke(grenade, "Explode");
            Invoke(grenade, "Explode");
            check("A flash grenade applies its effect only once per player", Mathf.Approximately(target.playerStatus.MoveSpeed.Current, 1f));
            check("Flash grenade retains its existing effect on the nearby thrower", Mathf.Approximately(owner.playerStatus.MoveSpeed.Current, 1f));
        }
        yield return null;
    }

    static void CheckGun(Action<string, bool> check, bool longGun, string name, string obstacle, int damage)
    {
        using (var fixture = new Fixture())
        {
            var owner = fixture.Player("Gun owner", Origin);
            var target = fixture.Player("Gun target", Origin + Vector3.forward * (obstacle == "falloff" ? 6.5f : 10f));
            var action = longGun ? (IPrimaryAction)new LongGunPrimary() : new ShortGunPrimary();
            if (obstacle == "trigger" || obstacle == "wall" || obstacle == "owner")
            {
                var blocker = fixture.Collider(obstacle, Origin + Vector3.forward * 3f);
                blocker.isTrigger = obstacle == "trigger";
                if (obstacle == "owner") blocker.transform.SetParent(owner.transform, true);
            }
            if (obstacle == "falloff" || obstacle == "range")
            {
                if (action is LongGunPrimary rifle) { rifle.maxRange = obstacle == "range" ? 5f : 10f; rifle.falloffStart = 2f; rifle.minDamageMultiplier = .5f; }
                if (action is ShortGunPrimary pistol) { pistol.maxRange = obstacle == "range" ? 5f : 10f; pistol.falloffStart = 2f; pistol.minDamageMultiplier = .5f; }
            }
            Physics.SyncTransforms();
            action.PrimaryAction(owner, owner.playerStatus);
            check(name, target.playerStatus.Hp.Current == 1000 - damage);
            int afterFirst = target.playerStatus.Hp.Current;
            action.PrimaryAction(owner, owner.playerStatus);
            check(name + " (immediate reuse blocked)", target.playerStatus.Hp.Current == afterFirst && action.CooldownRemaining > 0f);
        }
    }

    static void CheckTurret(Action<string, bool> check, string name, string obstacle, int damage)
    {
        using (var fixture = new Fixture())
        {
            var owner = fixture.Player("Turret owner", Origin + Vector3.left * 3f);
            var target = fixture.Player("Turret target", Origin + Vector3.forward * 10f);
            if (obstacle != null)
            {
                var blocker = fixture.Collider(obstacle, Origin + Vector3.forward * 3f);
                blocker.isTrigger = obstacle == "trigger";
            }
            var turret = fixture.GameObject("Turret", Origin).AddComponent<KingTurret>();
            turret.enabled = false;
            turret.Initialize(owner, owner.playerStatus, .6f, 40f, 20f, .5f, 1f, ~0, "");
            typeof(KingTurret).GetField("nextFireTime", PrivateInstance).SetValue(turret, -1f);
            Physics.SyncTransforms();
            Invoke(turret, "Update");
            Invoke(turret, "Update");
            check(name, target.playerStatus.Hp.Current == 1000 - damage);
        }
    }

    static void CheckPausedTurret(Action<string, bool> check)
    {
        using (var fixture = new Fixture())
        {
            var owner = fixture.Player("Paused turret owner", Origin + Vector3.left * 3f);
            var target = fixture.Player("Paused turret target", Origin + Vector3.forward * 10f);
            var turret = fixture.GameObject("Paused turret", Origin).AddComponent<KingTurret>();
            turret.enabled = false;
            turret.Initialize(owner, owner.playerStatus, .6f, 40f, 20f, .5f, 1f, ~0, "");
            typeof(KingTurret).GetField("nextFireTime", PrivateInstance).SetValue(turret, -1f);
            Physics.SyncTransforms();
            float previousTimeScale = Time.timeScale;
            try
            {
                Time.timeScale = 0f;
                Invoke(turret, "Update");
                check("A due turret shot cannot damage while time is paused", target.playerStatus.Hp.Current == 1000);
                int beforeResume = target.playerStatus.Hp.Current;
                Time.timeScale = 1f;
                Invoke(turret, "Update");
                check("The paused turret's pending shot fires after time resumes", target.playerStatus.Hp.Current == beforeResume - 20);
            }
            finally { Time.timeScale = previousTimeScale; }
        }
    }

    static void Hit(ProjectileArrow arrow, Collider collider) => Invoke(arrow, "HandleHit", collider);
    static void Invoke(object target, string method, params object[] args) => target.GetType().GetMethod(method, PrivateInstance).Invoke(target, args);

    sealed class Fixture : IDisposable
    {
        readonly List<GameObject> objects = new List<GameObject>();
        readonly List<Player> players = new List<Player>();

        public GameObject GameObject(string name, Vector3 position)
        {
            var go = new GameObject("Projectile check: " + name);
            go.transform.position = position;
            objects.Add(go);
            return go;
        }

        public Player Player(string name, Vector3 position)
        {
            var go = GameObject(name, position);
            var player = go.AddComponent<Player>();
            player.enabled = false;
            player.playerStatus = new PlayerStatus(1000, 20, 5f, player);
            var collider = go.AddComponent<BoxCollider>();
            collider.size = new Vector3(1f, 3f, 1f);
            var camera = new GameObject("Fixture camera").AddComponent<Camera>();
            camera.transform.SetParent(go.transform, false);
            camera.enabled = false;
            players.Add(player);
            return player;
        }

        public BoxCollider Collider(string name, Vector3 position) => GameObject(name, position).AddComponent<BoxCollider>();

        public ProjectileArrow Arrow(Player owner)
        {
            var arrow = GameObject("Arrow", Origin + Vector3.forward).AddComponent<ProjectileArrow>();
            arrow.Initialize(owner, owner.playerStatus, 1f, 100f, 200f, .5f, ~0);
            arrow.Launch(Vector3.zero, 0f, 2f);
            return arrow;
        }

        public ProjectileArrow OwnedArrow(Player owner)
        {
            var arrow = Object.FindObjectsByType<ProjectileArrow>(FindObjectsSortMode.None).FirstOrDefault(p => p.GetComponent<CombatVisualLifetime>()?.owner == owner);
            if (arrow != null) objects.Add(arrow.gameObject);
            return arrow;
        }

        public void Dispose()
        {
            // Production feedback is detached from the fixture hierarchy.
            foreach (var effect in Object.FindObjectsByType<CombatVisualLifetime>(FindObjectsSortMode.None))
                if (players.Contains(effect.owner)) Object.DestroyImmediate(effect.gameObject);
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (canvas.name == "FlashbangOverlay" && canvas.worldCamera != null && players.Contains(canvas.worldCamera.GetComponentInParent<Player>())) Object.DestroyImmediate(canvas.gameObject);
            foreach (var go in objects)
                if (go != null) Object.DestroyImmediate(go);
            Physics.SyncTransforms();
        }
    }
}
