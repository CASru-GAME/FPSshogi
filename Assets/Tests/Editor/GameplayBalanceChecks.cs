using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using App.Main.GameMaster;
using App.Main.Player;
using App.Main.ShogiThings;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class GameplayBalanceChecks
{
    static readonly FieldInfo Primary = typeof(Player).GetField("currentPrimaryAction", BindingFlags.NonPublic | BindingFlags.Instance);
    static readonly Vector3 Origin = new Vector3(2100f, 200f, 2000f);

    // Use the action selected by the real match, with an unobstructed body as the target.
    // The melee contact box fixes the contact point so a miss cannot be mistaken for low damage.
    static IEnumerator MeasureHit(Player equipped, float distance, Action<int> record)
    {
        using var fixture = new GameplayProjectileChecks.Fixture();
        var attacker = fixture.Player("Balance attacker", Origin);
        var target = fixture.Player("Balance target", Origin + Vector3.forward * distance);
        attacker.SetPlayerStatus(equipped.playerStatus);
        var action = (IPrimaryAction)Primary.GetValue(equipped);
        if (action is CloseWeaponPrimary)
        {
            var contact = fixture.Collider("Melee contact", target.transform.position);
            contact.transform.SetParent(attacker.transform, true);
            contact.gameObject.AddComponent<PlayerWeapon>();
        }
        if (action is BowPrimary bow)
        {
            attacker.SetSubWeaponObject(equipped.SubWeaponObject);
            // Aim the physical arrow on its low arc at a target at the same elevation.
            float angle = .5f * Mathf.Asin(Mathf.Clamp01(-Physics.gravity.y * distance / (bow.arrowSpeed * bow.arrowSpeed)));
            attacker.GetComponentInChildren<Camera>().transform.localRotation = Quaternion.Euler(-angle * Mathf.Rad2Deg, 0f, 0f);
        }
        Physics.SyncTransforms();
        int before = target.playerStatus.Hp.Current;
        action.PrimaryAction(attacker, attacker.playerStatus);
        if (action is BowPrimary)
        {
            float deadline = Time.realtimeSinceStartup + 4f;
            while (target.playerStatus.Hp.Current == before && Time.realtimeSinceStartup < deadline)
                yield return .05d;
        }
        record(before - target.playerStatus.Hp.Current);
    }

    public static IEnumerator Run(Action<string, bool> check)
    {
        var profiles = new List<(PieceType type, bool promoted, int hp, int damage, float interval)>();
        var openingShots = new List<(PieceType type, bool promoted, int damage)>();
        foreach (PieceType type in Enum.GetValues(typeof(PieceType)))
        {
            if (type == PieceType.None) continue;
            foreach (bool promoted in new[] { false, true })
            {
                SceneManager.LoadScene("Main");
                yield return .5d;
                var board = Object.FindFirstObjectByType<ShogiBoard>();
                var grid = board.GetBoard();
                var first = grid.Cast<IPiece>().First(p => p?.Type == type && p.Player == PlayerType.PlayerOne);
                if (promoted && !first.IsPromotable) continue;
                var second = grid.Cast<IPiece>().First(p => p?.Type == type && p.Player == PlayerType.PlayerTwo);
                if (promoted) { first.Promote(); second.Promote(); }
                var delta = first.Movement.First(d => Math.Abs(d[0]) <= 2 && Math.Abs(d[1]) <= 2);
                Array.Clear(grid, 0, grid.Length);
                grid[4, 4] = first;
                grid[4 + delta[0], 4 + delta[1]] = second;
                if (board.MovePiece(4, 4, 4 + delta[0], 4 + delta[1], PlayerType.PlayerOne) != ShogiBoard.MoveResult.DuelMove)
                    throw new InvalidOperationException("Could not start balance fixture for " + type);

                var manager = Object.FindFirstObjectByType<PlayerManager>();
                var one = manager.PlayerOne.GetComponent<Player>();
                var two = manager.PlayerTwo.GetComponent<Player>();
                var status = one.playerStatus;
                string name = type + (promoted ? "+" : "");
                check($"balance: {name} has the same loadout and stats for both seats",
                    one.PieceType == type && two.PieceType == type && status.Hp.Max == two.playerStatus.Hp.Max &&
                    status.AttackPoint.Current == two.playerStatus.AttackPoint.Current &&
                    status.MoveSpeed.Current == two.playerStatus.MoveSpeed.Current &&
                    Primary.GetValue(one).GetType() == Primary.GetValue(two).GetType() &&
                    Mathf.Approximately(one.PrimaryCooldownDuration, two.PrimaryCooldownDuration));
                int damage = 0;
                var near = MeasureHit(one, 2f, value => damage = value);
                while (near.MoveNext()) yield return near.Current;
                check($"balance: {name} HP={status.Hp.Max}, contact damage={damage}, interval={one.PrimaryCooldownDuration:F2}s",
                    damage > 0 && damage == status.AttackPoint.Current && one.PrimaryCooldownDuration > 0);
                profiles.Add((type, promoted, status.Hp.Max, damage, one.PrimaryCooldownDuration));
                if (Primary.GetValue(two) is CloseWeaponPrimary) continue;
                float openingDistance = Vector3.Distance(one.transform.position, two.transform.position);
                int atSpawn = 0;
                var far = MeasureHit(two, openingDistance, value => atSpawn = value);
                while (far.MoveNext()) yield return far.Current;
                check($"balance: {name} at opening distance {openingDistance:F1}m deals {atSpawn}",
                    type == PieceType.Kyosya ? atSpawn == 0 : atSpawn > 0 && atSpawn < damage);
                openingShots.Add((type, promoted, atSpawn));
            }
        }
        check("balance: covered all 14 playable forms", profiles.Count == 14);
        var pawn = profiles.Single(p => p.type == PieceType.Fuhyo && !p.promoted);
        var king = profiles.Single(p => p.type == PieceType.King);
        foreach (var attacker in profiles)
        {
            foreach (var defender in profiles)
            {
                int hits = attacker.damage > 0 ? Mathf.CeilToInt((float)defender.hp / attacker.damage) : 0;
                check($"balance: {attacker.type}{(attacker.promoted ? "+" : "")} vs {defender.type}{(defender.promoted ? "+" : "")}: {hits} unguarded hits, {(hits-1)*attacker.interval:F2}s from first hit",
                    hits >= 2 && hits <= 7);
            }
        }
        check("balance: pawn can defeat a full-health king in five clear thrusts", pawn.damage * 4 < king.hp && pawn.damage * 5 >= king.hp);
        check("balance: pawn survives two opening shots from any ranged form", openingShots.All(shot => shot.damage * 2 < pawn.hp));
        var support = new KingSkill();
        int salvo = 3 * Mathf.CeilToInt(king.damage * support.turretDamageMultiplier);
        int volleys = Mathf.CeilToInt(support.skillDuration / support.turretFireInterval);
        check($"balance: three turrets alone deal at most {salvo} x {volleys} at full damage", salvo > 0 && salvo * volleys < profiles.Min(p => p.hp));
    }
}
