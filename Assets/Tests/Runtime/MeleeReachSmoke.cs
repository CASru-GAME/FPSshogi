#if FPS_GAMEPLAY_SMOKE
using System;
using System.Collections;
using System.Linq;
using App.Main.Player;
using App.Main.ShogiThings;
using UnityEngine;

public sealed partial class GameplayPlayerSmoke
{
    readonly bool reachRun = Environment.GetCommandLineArgs().Contains("-reach-checks");

    static Transform ReachMark(int seat) =>
        GameObject.Find($"P{seat} Crosshair")?.transform.Find($"P{seat} melee reach");

    static IEnumerator SettleHud() { yield return null; yield return new WaitForEndOfFrame(); }

    IEnumerator CheckReachFor(Player attacker, Player defender, int seat, float expectedFrontReach)
    {
        var marker = ReachMark(seat);
        string name = attacker.PieceType.ToString();
        Check(marker != null, name + " has its own melee reach brackets");
        if (marker == null) yield break;
        Place(attacker,ArenaOrigin,0);
        attacker.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
        defender.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
        void Opposite(float distance, float height = 0f) =>
            Place(defender,ArenaOrigin + Vector3.forward * distance + Vector3.up * height,180);
        float inside = 2.5f, outside = 16f;
        Opposite(inside); yield return SettleHud();
        Check(marker.gameObject.activeInHierarchy, name + " indicates a close exposed body");
        Opposite(outside); yield return SettleHud();
        Check(!marker.gameObject.activeInHierarchy, name + " keeps the normal reticle at a distance");
        for (int i = 0; i < 9; i++)
        {
            float distance = (inside + outside) * .5f;
            Opposite(distance); yield return SettleHud();
            if (marker.gameObject.activeInHierarchy) inside = distance; else outside = distance;
        }
        float measured = (inside + outside) * .5f;
        Check(Mathf.Abs(measured - expectedFrontReach) < .14f,
            $"{name} against {defender.PieceType} has {measured:F2} m frontal reach (target {expectedFrontReach:F2} m)");
        int hp = defender.playerStatus.Hp.Current;
        Opposite(outside + .07f); yield return SettleHud();
        bool unmarked = !marker.gameObject.activeInHierarchy;
        yield return FireOnce(attacker); yield return new WaitForSecondsRealtime(.6f);
        Check(unmarked && hp == defender.playerStatus.Hp.Current, $"{name} misses just outside its displayed reach ({outside+.07f:F2} m)");
        while (attacker.PrimaryCooldown > 0) yield return null;
        Opposite(inside - .07f); yield return SettleHud();
        bool marked = marker.gameObject.activeInHierarchy;
        yield return FireOnce(attacker); yield return new WaitForSecondsRealtime(.6f);
        Check(marked && hp - defender.playerStatus.Hp.Current == attacker.playerStatus.AttackPoint.Current,
            $"{name} hits exactly once just inside its displayed reach ({inside-.07f:F2} m)");
        Opposite(inside - .07f,10f); yield return SettleHud();
        Check(!marker.gameObject.activeInHierarchy,name + " excludes a body overhead");
        Opposite(inside - .07f);
        var cover = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cover.transform.position = ArenaOrigin + Vector3.forward * inside * .5f;
        cover.transform.localScale = new Vector3(7,8,.2f);
        Physics.SyncTransforms(); yield return SettleHud();
        hp = defender.playerStatus.Hp.Current;
        while (attacker.PrimaryCooldown > 0) yield return null;
        unmarked = !marker.gameObject.activeInHierarchy;
        yield return FireOnce(attacker); yield return new WaitForSecondsRealtime(.6f);
        Check(unmarked && hp == defender.playerStatus.Hp.Current,name + " hides the brackets and cannot strike through solid cover");
        Destroy(cover); yield return null;
        if (attacker.PieceType == PieceType.Fuhyo)
        {
            // Put the spear tip on the actual lower side of the king, outside his movement capsule.
            var spear = attacker.WeaponObject.GetComponent<WeaponPresentation>();
            spear.GetMeleeBox(Mathf.Lerp(spear.hitStart,spear.hitEnd,.6f),out var center,out var half,out var rotation);
            Vector3 tip = spear.transform.TransformPoint(center + rotation * Vector3.up * (half.y - .12f));
            var wood = defender.GetComponentsInChildren<MeshFilter>().First(m => m.gameObject.layer == defender.BodyLayer);
            Bounds bounds = wood.sharedMesh.bounds;
            Vector3 edge = wood.transform.TransformPoint(bounds.center + Vector3.Scale(bounds.extents,new Vector3(.8f,-.3f,0f)));
            Place(defender,defender.transform.position + tip - edge,180f);
            yield return SettleHud();
            bool edgeMarked = marker.gameObject.activeInHierarchy;
            float pastCapsule = Vector3.Distance(tip,defender.GetComponent<Collider>().ClosestPoint(tip));
            while (attacker.PrimaryCooldown > 0) yield return null;
            hp = defender.playerStatus.Hp.Current;
            yield return FireOnce(attacker); yield return new WaitForSecondsRealtime(.6f);
            Check(pastCapsule > .2f && edgeMarked && hp - defender.playerStatus.Hp.Current == attacker.playerStatus.AttackPoint.Current,
                $"Spear brackets and contact include the king's visible edge {pastCapsule:F2} m beyond his movement capsule");
        }
    }

    IEnumerator CheckMeleeReach()
    {
        yield return Matchup(new Fuhyo(PlayerType.PlayerOne),new King(PlayerType.PlayerTwo));
        yield return CheckReachFor(One(),Two(),1,9.7f);
        yield return CheckReachFor(Two(),One(),2,6.7f);
        yield return Matchup(new Gin(PlayerType.PlayerOne),new Kin(PlayerType.PlayerTwo));
        yield return CheckReachFor(One(),Two(),1,9f);
        yield return CheckReachFor(Two(),One(),2,9f);
        yield return Matchup(new Keima(PlayerType.PlayerOne),new Hisya(PlayerType.PlayerTwo));
        yield return CheckReachFor(One(),Two(),1,8.45f);
        Check(ReachMark(2)?.gameObject.activeInHierarchy == false,"The bow never shows melee reach brackets");
    }
}
#endif
