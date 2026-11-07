#if FPS_GAMEPLAY_SMOKE
using System;
using System.Collections;
using System.Linq;
using App.Main.Player;
using App.Main.ShogiThings;
using App.PlayData;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public sealed partial class GameplayPlayerSmoke
{
    readonly bool dashRun = Environment.GetCommandLineArgs().Contains("-dash-checks");

    IEnumerator CheckDashContacts()
    {
        yield return Matchup(new Keima(PlayerType.PlayerOne),new King(PlayerType.PlayerTwo));
        Player knight = One(), target = Two();
        int lost = 0, impacts = 0;
        float sourceZ = 0f, travel = 0f;

        IEnumerator Dash(Vector3 offset, Action arrange = null, bool guard = false)
        {
            while (knight.SkillCooldown > 0f) yield return null;
            Place(knight,ArenaOrigin,0f);
            Place(target,ArenaOrigin + offset,180f);
            target.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
            arrange?.Invoke();
            Physics.SyncTransforms();
            if (guard) yield return Tap(target,GamepadButton.East);
            int hp = target.playerStatus.Hp.Current, version = target.playerStatus.ImpactVersion;
            yield return Tap(knight,GamepadButton.East);
            yield return new WaitForSecondsRealtime(.25f);
            lost = hp - target.playerStatus.Hp.Current;
            impacts = target.playerStatus.ImpactVersion - version;
            sourceZ = impacts > 0 ? (target.playerStatus.ImpactSource?.z ?? float.NaN) - ArenaOrigin.z : float.NaN;
            travel = knight.transform.position.z - ArenaOrigin.z;
        }

        Vector3 grazing = new Vector3(2.25f,0f,4.5f);
        yield return Dash(grazing);
        Check(Mathf.Abs(travel - 12f) < .15f && lost == 38 && impacts == 1 && sourceZ < 8f,
            $"A passing knight hits during the dash (travel {travel:F2} m, hit at {sourceZ:F2} m, HP lost {lost})");
        yield return Dash(new Vector3(3.9f,0f,12f));
        Check(lost == 38 && impacts == 1 && Mathf.Abs(travel - sourceZ) < .3f,
            $"The finishing slash hits beside the actual stopping point ({travel:F2} m, hit at {sourceZ:F2} m, HP lost {lost})");
        yield return Dash(new Vector3(2.25f,0f,10f));
        Check(lost == 38 && impacts == 1 && sourceZ < 11f,
            $"Contact and the finishing slash hit the same opponent only once ({impacts} impacts, HP lost {lost})");

        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.position = ArenaOrigin + Vector3.forward * 5f;
        wall.transform.localScale = new Vector3(12f,9f,.3f);
        yield return Dash(Vector3.forward * 7.4f);
        Check(travel < 5f && impacts == 0 && lost == 0,$"A solid wall stops the knight and shields the opponent (HP lost {lost})");
        Destroy(wall);
        var decoy = new GameObject("Opponent ability trigger").AddComponent<BoxCollider>();
        decoy.transform.SetParent(target.transform,true);
        decoy.isTrigger = true;
        yield return Dash(new Vector3(7f,0f,7f), () => decoy.transform.position = ArenaOrigin + Vector3.forward * 12f);
        Check(impacts == 0 && lost == 0,"An off-path opponent's trigger does not count as a body hit");
        Destroy(decoy.gameObject);

        float originalStep = Time.fixedDeltaTime;
        try
        {
            // The target lies between the first two sampled poses. The last step is only part of a tick.
            Time.fixedDeltaTime = .07f;
            yield return Dash(new Vector3(2.25f,0f,1.75f));
            Check(Mathf.Abs(travel - 12f) < .15f && lost == 38 && impacts == 1 && Mathf.Abs(sourceZ - 1.75f) < .1f,
                $"A single physics step sweeps its full path and the last step ends at 12 m (hit at {sourceZ:F2} m, travel {travel:F2} m)");
        }
        finally { Time.fixedDeltaTime = originalStep; }

        PlayRecorder.Current.Save();
        var first = ReadMatches().SelectMany(match => match.duels).Single().seats[0];
        Check(first.skillUses == 6 && first.skillHits == 4 && first.skillDamage == 152 && first.primaryDamage == 0,
            $"Dash telemetry records four skill hits from six uses ({first.skillHits} hits, {first.skillDamage} damage)");

        var promoted = new Keima(PlayerType.PlayerTwo);
        promoted.Promote();
        yield return Matchup(new Gin(PlayerType.PlayerOne),promoted);
        knight = Two(); target = One();
        yield return Dash(grazing,guard:true);
        Check(lost == 0 && impacts == 1 && target.playerStatus.ImpactBlocked,"Silver can block contact during the promoted knight's dash");
        yield return Dash(grazing);
        Check(lost == 44 && impacts == 1 && sourceZ < 8f,
            $"Second-seat promoted knight hits during the dash with its own attack power (HP lost {lost})");
        yield return Dash(Vector3.forward * 6f);
        Check(lost == 44 && impacts == 1 && sourceZ > 0f && sourceZ < 6f,
            $"A head-on dash strikes once on approach (hit at {sourceZ:F2} m, {impacts} impacts, HP lost {lost})");
    }
}
#endif
