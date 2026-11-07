using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using App.Main.Player;
using UnityEngine;
using Object = UnityEngine.Object;

// Invoked by the shared gameplay runner in Play Mode; owns no menu or trigger.
public static class GameplayEffectsChecks
{
    public static IEnumerator Run(Action<string, bool> check)
    {
        var owned = new List<GameObject>();
        var effects = new List<IEffect>();
        var parameter = ScriptableObject.CreateInstance<StatusParameter>();
        var playerRoot = new GameObject("EffectsChecks_Player");
        owned.Add(playerRoot);
        var player = playerRoot.AddComponent<Player>();
        player.enabled = false;
        var cameraRoot = new GameObject("EffectsChecks_Camera");
        cameraRoot.transform.SetParent(playerRoot.transform, false);
        cameraRoot.AddComponent<Camera>().enabled = false;

        try
        {
            // A nonzero configured defense must change the first damage taken.
            parameter.hpMax = 100;
            parameter.attackPointDefault = 10;
            parameter.defensePointDefault = 7;
            parameter.moveSpeedDefault = 10f;
            parameter.hpMaxUra = 120;
            parameter.attackPointDefaultUra = 20;
            parameter.defensePointDefaultUra = 11;
            parameter.moveSpeedDefaultUra = 12f;
            var front = parameter.CreatePlayerStatus(false, player);
            front.TakeDamage(20);
            check("effects: normal configured defense reduces damage to 13 (HP=" + front.Hp.Current + ")", front.Hp.Current == 87);
            var promoted = parameter.CreatePlayerStatus(true, player);
            promoted.TakeDamage(20);
            check("effects: promoted configured defense reduces damage to 9 (HP=" + promoted.Hp.Current + ")", promoted.Hp.Current == 111);

            var status = Status(player);
            var slow = new SlowEffect();
            effects.Add(slow);
            check("effects: first Slow is added", status.EffectList.AddEffect(slow));
            slow.UpdateEffect();
            check("effects: repeated Slow refreshes the existing effect", !status.EffectList.AddEffect(new SlowEffect()));
            Equal(check, "repeated Slow stays at half speed", status.MoveSpeed.Current, 5f);
            Equal(check, "repeated Slow refreshes its duration", slow.lastTime, 5f);
            slow.EndEffect();
            Equal(check, "ending repeated Slow restores original speed", status.MoveSpeed.Current, 10f);

            status = Status(player);
            status.MoveSpeed.Multiply(1.5f);
            slow = new SlowEffect();
            effects.Add(slow);
            status.EffectList.AddEffect(slow);
            slow.EndEffect();
            Equal(check, "ending Slow preserves an existing speed boost", status.MoveSpeed.Current, 15f);

            status = Status(player);
            var flash = new FlashBangEffect();
            effects.Add(flash);
            status.EffectList.AddEffect(flash);
            status.EffectList.AddEffect(new FlashBangEffect());
            Equal(check, "repeated FlashBang keeps one movement penalty", status.MoveSpeed.Current, 2f);
            flash.EndEffect();
            Equal(check, "ending repeated FlashBang restores original speed", status.MoveSpeed.Current, 10f);

            status = Status(player);
            slow = new SlowEffect();
            flash = new FlashBangEffect();
            effects.Add(slow);
            effects.Add(flash);
            status.EffectList.AddEffect(slow);
            status.EffectList.AddEffect(flash);
            Equal(check, "different slow kinds combine", status.MoveSpeed.Current, 1f);
            flash.EndEffect();
            Equal(check, "FlashBang expiry preserves remaining Slow", status.MoveSpeed.Current, 5f);
            slow.EndEffect();

            status = Status(player);
            slow = new SlowEffect();
            flash = new FlashBangEffect();
            effects.Add(slow);
            effects.Add(flash);
            status.EffectList.AddEffect(slow);
            status.EffectList.AddEffect(flash);
            slow.EndEffect();
            Equal(check, "Slow expiry preserves remaining FlashBang", status.MoveSpeed.Current, 2f);
            flash.EndEffect();

            // A stopped effect cannot be undone by dividing by its multiplier.
            status = Status(player);
            status.MoveSpeed.Multiply(1.5f);
            var stopped = new HisyaFieldSlowEffect(0f);
            effects.Add(stopped);
            status.EffectList.AddEffect(stopped);
            Equal(check, "zero multiplier stops movement", status.MoveSpeed.Current, 0f);
            stopped.EndEffect();
            Equal(check, "ending zero multiplier preserves speed boost", status.MoveSpeed.Current, 15f);

            // Simulate Unity trigger messages using real player colliders and areas.
            var body = playerRoot.AddComponent<CapsuleCollider>();
            var childRoot = new GameObject("EffectsChecks_ChildCollider");
            childRoot.transform.SetParent(playerRoot.transform, false);
            var childCollider = childRoot.AddComponent<BoxCollider>();
            var areaA = Area(owned, "A");
            var areaB = Area(owned, "B");
            status = Status(player);
            Enter(areaA, body);
            Enter(areaB, body);
            Equal(check, "overlapping Hisya fields keep one slow strength", status.MoveSpeed.Current, 6f);
            Exit(areaA, body);
            Equal(check, "leaving one Hisya field keeps the other active", status.MoveSpeed.Current, 6f);
            Exit(areaB, body);
            Equal(check, "leaving all Hisya fields removes the slow", status.MoveSpeed.Current, 10f);

            status = Status(player);
            Enter(areaA, body);
            Enter(areaA, childCollider);
            Exit(areaA, body);
            Equal(check, "one collider leaving does not release a player still in the field", status.MoveSpeed.Current, 6f);
            Exit(areaA, childCollider);
            Equal(check, "last collider leaving removes the field slow", status.MoveSpeed.Current, 10f);

            status = Status(player);
            slow = new SlowEffect();
            effects.Add(slow);
            status.EffectList.AddEffect(slow);
            Enter(areaA, body);
            Equal(check, "Hisya field combines with another slow kind", status.MoveSpeed.Current, 3f);
            Exit(areaA, body);
            Equal(check, "leaving Hisya field preserves another slow kind", status.MoveSpeed.Current, 5f);
            slow.EndEffect();

            status = Status(player);
            Enter(areaA, body);
            Enter(areaB, body);
            Object.Destroy(areaA.gameObject);
            yield return .1d;
            Equal(check, "destroying one field preserves an overlapping field", status.MoveSpeed.Current, 6f);
            Exit(areaB, body);
            Equal(check, "remaining field releases after another was destroyed", status.MoveSpeed.Current, 10f);

            status = Status(player);
            slow = new SlowEffect();
            flash = new FlashBangEffect();
            effects.Add(slow);
            effects.Add(flash);
            status.EffectList.AddEffect(slow);
            status.EffectList.AddEffect(flash);
            var clear = typeof(EffectList).GetMethod("ClearEffects", BindingFlags.Instance | BindingFlags.Public);
            check("effects: effect list exposes duel cleanup", clear != null);
            if (clear != null)
            {
                clear.Invoke(status.EffectList, null);
                check("effects: duel cleanup ends every active effect", !slow.isActive && !flash.isActive);
                Equal(check, "duel cleanup restores speed", status.MoveSpeed.Current, 10f);
                clear.Invoke(status.EffectList, null);
                Equal(check, "repeated duel cleanup is harmless", status.MoveSpeed.Current, 10f);
            }
            slow.EndEffect();
            flash.EndEffect();

            // The rejected activation must not redirect the pending guard removal.
            status = Status(player);
            status.DefensePoint.Add(7);
            var otherStatus = new PlayerStatus(100, 10, 10f, player);
            var guard = new SecondaryGuard();
            guard.SecondaryAction(player, status);
            guard.SecondaryAction(player, otherStatus);
            Expire(guard, "buffTimer");
            guard.UpdateSecondaryAction();
            check("effects: guard cleanup targets the status it buffed (defense=" + status.DefensePoint.Current + ")", status.DefensePoint.Current == 7);
            check("effects: rejected guard activation leaves other status untouched (defense=" + otherStatus.DefensePoint.Current + ")", otherStatus.DefensePoint.Current == 0);

            const int baseDefense = 7;
            const int goldArmor = 16;
            const int silverGuard = 99999;
            status = Status(player);
            status.DefensePoint.Add(baseDefense);
            var gin = new GinSkill();
            var kin = new KinSkill();
            guard = new SecondaryGuard();
            gin.UseSkill(player, status);
            kin.UseSkill(player, status);
            guard.SecondaryAction(player, status);
            gin.UseSkill(player, status);
            kin.UseSkill(player, status);
            guard.SecondaryAction(player, status);
            Equal(check, "cooldown blocks duplicate Gin Kin and guard buffs", status.DefensePoint.Current,
                baseDefense + goldArmor + silverGuard + guard.defenseIncrease);
            Expire(gin, "timer");
            gin.UpdateSkill();
            Equal(check, "Gin expiry preserves Kin guard and base defense", status.DefensePoint.Current,
                baseDefense + goldArmor + guard.defenseIncrease);
            Expire(guard, "buffTimer");
            guard.UpdateSecondaryAction();
            Equal(check, "guard expiry preserves Kin and base defense", status.DefensePoint.Current, baseDefense + goldArmor);
            Expire(kin, "timer");
            kin.UpdateSkill();
            Equal(check, "all defense buffs expire back to base defense", status.DefensePoint.Current, baseDefense);

            var physicsChecks = RunPhysicsFieldChecks(check, owned);
            while (physicsChecks.MoveNext()) yield return physicsChecks.Current;
        }
        finally
        {
            foreach (var effect in effects) effect.EndEffect();
            foreach (var go in owned) if (go != null) Object.Destroy(go);
            Object.Destroy(parameter);
        }
        yield return .1d;
    }

    static PlayerStatus Status(Player player)
    {
        var status = new PlayerStatus(100, 10, 10f, player);
        player.SetPlayerStatus(status);
        return status;
    }

    static HisyaFieldArea Area(List<GameObject> owned, string suffix)
    {
        var go = new GameObject("EffectsChecks_Field" + suffix);
        owned.Add(go);
        var area = go.AddComponent<HisyaFieldArea>();
        area.enabled = false;
        return area;
    }

    static void Enter(HisyaFieldArea area, Collider collider) => area.SendMessage("OnTriggerEnter", collider, SendMessageOptions.RequireReceiver);
    static void Exit(HisyaFieldArea area, Collider collider) => area.SendMessage("OnTriggerExit", collider, SendMessageOptions.RequireReceiver);

    sealed class PhysicsFieldFixture
    {
        public Player player;
        public Rigidbody rigidbody;
        public CapsuleCollider body;
        public BoxCollider weapon;
        public Vector3 center;

        public void MoveInside() => Move(center);
        public void MoveOutside() => Move(center + Vector3.right * 6f);

        void Move(Vector3 position)
        {
            rigidbody.position = position;
            Physics.SyncTransforms();
        }
    }

    static PhysicsFieldFixture PhysicsField(List<GameObject> owned, string suffix)
    {
        var fixture = new PhysicsFieldFixture { center = new Vector3(1000f + owned.Count * 20f, 1000f, 1000f) };
        var root = new GameObject("EffectsChecks_PhysicsPlayer_" + suffix);
        owned.Add(root);
        root.transform.position = fixture.center + Vector3.right * 6f;
        fixture.player = root.AddComponent<Player>();
        fixture.player.enabled = false;
        Status(fixture.player);
        fixture.rigidbody = root.AddComponent<Rigidbody>();
        fixture.rigidbody.isKinematic = true;
        fixture.rigidbody.useGravity = false;
        fixture.body = root.AddComponent<CapsuleCollider>();
        var weaponRoot = new GameObject("TemporaryMeleeTrigger");
        weaponRoot.transform.SetParent(root.transform, false);
        fixture.weapon = weaponRoot.AddComponent<BoxCollider>();
        fixture.weapon.isTrigger = true;

        var fieldRoot = new GameObject("EffectsChecks_PhysicsField_" + suffix);
        owned.Add(fieldRoot);
        fieldRoot.transform.position = fixture.center;
        var fieldTrigger = fieldRoot.AddComponent<SphereCollider>();
        fieldTrigger.isTrigger = true;
        fieldTrigger.radius = 2f;
        var area = fieldRoot.AddComponent<HisyaFieldArea>();
        area.duration = 60f;
        Physics.SyncTransforms();
        return fixture;
    }

    static IEnumerator RunPhysicsFieldChecks(Action<string, bool> check, List<GameObject> owned)
    {
        // These tests let Unity deliver real trigger events. No SendMessage or
        // private contact bookkeeping is used, including for collider removal.
        var weaponOnly = PhysicsField(owned, "WeaponOnly");
        weaponOnly.weapon.transform.localPosition = Vector3.left * 6f;
        Physics.SyncTransforms();
        yield return .2d;
        Equal(check, "physics weapon trigger alone does not slow a body outside", weaponOnly.player.playerStatus.MoveSpeed.Current, 10f);
        weaponOnly.weapon.transform.localPosition = Vector3.zero;
        weaponOnly.MoveInside();
        yield return .2d;
        Equal(check, "physics body entering the field is slowed", weaponOnly.player.playerStatus.MoveSpeed.Current, 6f);
        weaponOnly.MoveOutside();
        yield return .2d;
        Equal(check, "physics body and enabled weapon leaving releases slow", weaponOnly.player.playerStatus.MoveSpeed.Current, 10f);

        var disabledWeapon = PhysicsField(owned, "DisabledWeapon");
        disabledWeapon.MoveInside();
        yield return .2d;
        Equal(check, "physics body and melee trigger enter the field", disabledWeapon.player.playerStatus.MoveSpeed.Current, 6f);
        disabledWeapon.weapon.enabled = false;
        yield return .2d;
        Equal(check, "physics ending an attack keeps a body inside slowed", disabledWeapon.player.playerStatus.MoveSpeed.Current, 6f);
        disabledWeapon.MoveOutside();
        yield return .2d;
        Equal(check, "physics disabled melee trigger cannot retain slow after body exit", disabledWeapon.player.playerStatus.MoveSpeed.Current, 10f);

        var twoBodies = PhysicsField(owned, "MultipleBodies");
        twoBodies.weapon.isTrigger = false;
        twoBodies.MoveInside();
        yield return .2d;
        Equal(check, "physics two legitimate body colliders receive one slow", twoBodies.player.playerStatus.MoveSpeed.Current, 6f);
        twoBodies.body.enabled = false;
        yield return .2d;
        Equal(check, "physics disabling one body collider preserves another inside", twoBodies.player.playerStatus.MoveSpeed.Current, 6f);
        twoBodies.MoveOutside();
        yield return .2d;
        Equal(check, "physics disabled body collider cannot retain slow after last body exits", twoBodies.player.playerStatus.MoveSpeed.Current, 10f);

        var disabledBody = PhysicsField(owned, "DisabledBody");
        disabledBody.weapon.enabled = false;
        disabledBody.MoveInside();
        yield return .2d;
        Equal(check, "physics body is slowed before its collider is disabled", disabledBody.player.playerStatus.MoveSpeed.Current, 6f);
        disabledBody.body.enabled = false;
        yield return .2d;
        Equal(check, "physics disabling the last body collider releases slow", disabledBody.player.playerStatus.MoveSpeed.Current, 10f);

        var destroyedBody = PhysicsField(owned, "DestroyedBody");
        destroyedBody.weapon.enabled = false;
        destroyedBody.MoveInside();
        yield return .2d;
        Equal(check, "physics body is slowed before its collider is destroyed", destroyedBody.player.playerStatus.MoveSpeed.Current, 6f);
        Object.Destroy(destroyedBody.body);
        yield return .2d;
        Equal(check, "physics destroying the last body collider releases slow", destroyedBody.player.playerStatus.MoveSpeed.Current, 10f);
    }

    static void Expire(object target, string timer)
    {
        // Advance only the timer boundary; expiration is executed by production code.
        target.GetType().GetField(timer, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, float.Epsilon);
    }

    static void Equal(Action<string, bool> check, string name, float actual, float expected)
    {
        check("effects: " + name + " (actual=" + actual + ", expected=" + expected + ")", Mathf.Abs(actual - expected) < .0001f);
    }
}
