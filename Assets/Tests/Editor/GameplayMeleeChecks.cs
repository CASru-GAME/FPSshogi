using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using App.Main.Player;
using UnityEngine;
using Object = UnityEngine.Object;

// Uses the real attack action and weapon callbacks, with isolated collider fixtures.
public static class GameplayMeleeChecks
{
    static readonly Vector3 Origin = new Vector3(2100f, 200f, 2000f);

    public static IEnumerator Run(Action<string, bool> check)
    {
        MeleeBodyChecks.Run(check);
        using (var fixture = new Fixture())
        {
            var owner = fixture.Player("Owner", Origin);
            var target = fixture.Player("Distant body", Origin + Vector3.forward * 5f);
            var weapon = fixture.Weapon(owner);
            var enemyWeapon = fixture.Box("Enemy weapon trigger", Origin + Vector3.forward * 2f, Vector3.one * .5f);
            enemyWeapon.transform.SetParent(target.transform, true);
            var enemyWeaponLogic = enemyWeapon.gameObject.AddComponent<PlayerWeapon>();
            enemyWeaponLogic.SetOwner(target);
            enemyWeapon.enabled = true;
            Physics.SyncTransforms();
            var action = new CloseWeaponPrimary();
            action.PrimaryAction(owner, owner.playerStatus);
            Physics.SyncTransforms();
            check("Melee trigger fixture overlaps only the enemy weapon, not the body",
                weapon.GetComponent<Collider>().bounds.Intersects(enemyWeapon.bounds) &&
                !weapon.GetComponent<Collider>().bounds.Intersects(target.GetComponent<Collider>().bounds));
            check("Melee initial overlap ignores the opponent weapon trigger", target.playerStatus.Hp.Current == 1000);
            Enter(weapon, enemyWeapon);
            check("Melee entering overlap ignores the opponent weapon trigger", target.playerStatus.Hp.Current == 1000);
        }

        CheckBody(check, "Melee initial overlap damages a clear body once", false, false, false, 980);
        CheckBody(check, "Melee initial overlap cannot damage a body behind a thin solid wall", true, false, false, 1000);
        CheckBody(check, "Melee entering overlap damages a clear body once", false, true, false, 980);
        CheckBody(check, "Melee entering overlap cannot damage a body behind a thin solid wall", true, true, false, 1000);
        CheckBody(check, "Melee can damage a body through an ability trigger volume", false, false, true, 980);

        using (var fixture = new Fixture())
        {
            var owner = fixture.Player("Owner", Origin);
            var target = fixture.Player("Target after cancellation", Origin + Vector3.forward * 5f);
            var weapon = fixture.Weapon(owner);
            new CloseWeaponPrimary().PrimaryAction(owner, owner.playerStatus);
            weapon.gameObject.SetActive(false);
            target.transform.position = Origin + Vector3.forward * 2f;
            Physics.SyncTransforms();
            Enter(weapon, target.GetComponent<Collider>());
            check("Cancelling a melee attack closes its hitbox and damage callback", target.playerStatus.Hp.Current == 1000 && !weapon.GetComponent<Collider>().enabled);
        }
        yield return null;
    }

    static void CheckBody(Action<string, bool> check, string name, bool wall, bool entering, bool field, int expectedHp)
    {
        using (var fixture = new Fixture())
        {
            var owner = fixture.Player("Owner", Origin);
            var target = fixture.Player("Target body", Origin + Vector3.forward * (entering ? 5f : 2f));
            var weapon = fixture.Weapon(owner);
            if (wall || field)
            {
                var obstacle = fixture.Box(wall ? "Thin solid cover" : "Ability trigger", Origin + Vector3.forward, new Vector3(4f, 4f, .05f));
                obstacle.isTrigger = field;
            }
            Physics.SyncTransforms();
            new CloseWeaponPrimary().PrimaryAction(owner, owner.playerStatus);
            if (entering)
            {
                target.transform.position = Origin + Vector3.forward * 2f;
                Physics.SyncTransforms();
                Enter(weapon, target.GetComponent<Collider>());
            }
            // The same body receiving another callback must not gain another hit.
            Enter(weapon, target.GetComponent<Collider>());
            check(name + $" (HP={target.playerStatus.Hp.Current})", target.playerStatus.Hp.Current == expectedHp);
        }
    }

    static void Enter(PlayerWeapon weapon, Collider collider) => typeof(PlayerWeapon)
        .GetMethod("OnTriggerEnter", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(weapon, new object[] { collider });

    sealed class Fixture : IDisposable
    {
        readonly List<GameObject> objects = new List<GameObject>();

        public BoxCollider Box(string name, Vector3 position, Vector3 size)
        {
            var go = new GameObject("Melee check: " + name);
            go.transform.position = position;
            objects.Add(go);
            var collider = go.AddComponent<BoxCollider>();
            collider.size = size;
            return collider;
        }

        public Player Player(string name, Vector3 position)
        {
            var body = Box(name, position, new Vector3(.8f, 2f, .8f));
            var player = body.gameObject.AddComponent<Player>();
            player.enabled = false;
            player.playerStatus = new PlayerStatus(1000, 20, 5f, player);
            var camera = new GameObject("Melee fixture camera").AddComponent<Camera>();
            camera.transform.SetParent(player.transform, false);
            camera.enabled = false;
            return player;
        }

        public PlayerWeapon Weapon(Player owner)
        {
            var hitbox = Box("Active blade", Origin, Vector3.one);
            hitbox.transform.SetParent(owner.transform, true);
            hitbox.center = Vector3.forward * 2f;
            return hitbox.gameObject.AddComponent<PlayerWeapon>();
        }

        public void Dispose()
        {
            foreach (var go in objects)
                if (go != null) Object.DestroyImmediate(go);
            Physics.SyncTransforms();
        }
    }
}
