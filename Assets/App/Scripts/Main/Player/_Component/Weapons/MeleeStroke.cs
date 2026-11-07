using System;
using UnityEngine;

namespace App.Main.Player
{
    // The HUD previews the same contact poses that the attack visits in time order.
    // Sampling in attack time also keeps a short swing from skipping its tip at low frame rates.
    sealed class MeleeStroke
    {
        const int Intervals = 32;
        readonly Vector3[] centers = new Vector3[Intervals + 1];
        readonly Quaternion[] rotations = new Quaternion[Intervals + 1];
        readonly Player owner;
        readonly Transform view;
        readonly Transform weaponRoot;
        readonly Vector3 halfExtents;
        readonly float start, end, reach;
        RaycastHit[] blockers = new RaycastHit[8];

        public int Count => centers.Length;
        public float Age(int index) => Mathf.Lerp(start, end, index / (float)Intervals);

        public MeleeStroke(Player owner, Transform view, WeaponPresentation weapon)
        {
            this.owner = owner;
            this.view = view;
            weaponRoot = weapon.transform;
            start = weapon.hitStart;
            end = weapon.hitEnd;
            for (int i = 0; i < Count; i++)
            {
                weapon.GetMeleeBox(Age(i), out centers[i], out Vector3 half, out rotations[i]);
                if (i == 0) halfExtents = half;
                reach = Mathf.Max(reach, centers[i].magnitude + half.magnitude);
            }
        }

        public bool CanReach(Player target, int layers)
        {
            MeleeBody surface = MeleeBody.For(target);
            if (surface == null || (layers & (1 << surface.Layer)) == 0) return false;
            Vector3 scale = weaponRoot.lossyScale;
            float radius = reach * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            if (surface.WorldBounds.SqrDistance(view.position) > radius * radius) return false;
            for (int i = 0; i < Count; i++)
                if (Touches(i, target, surface, layers)) return true;
            return false;
        }

        public bool Touches(int index, Player target, MeleeBody surface, int layers)
        {
            if (surface == null || (layers & (1 << surface.Layer)) == 0) return false;
            Vector3 scale = weaponRoot.lossyScale;
            Vector3 center = view.position + view.rotation * Vector3.Scale(centers[index], scale);
            Vector3 half = Vector3.Scale(halfExtents, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
            if (!surface.Intersects(center, view.rotation * rotations[index], half, out Vector3 contact)) return false;
            Vector3 direction = contact - view.position;
            float length = direction.magnitude;
            if (length < .001f) return true;
            int count;
            while ((count = Physics.RaycastNonAlloc(view.position, direction / length, blockers, length, layers,
                       QueryTriggerInteraction.Ignore)) == blockers.Length)
                Array.Resize(ref blockers, blockers.Length * 2);
            for (int i = 0; i < count; i++)
            {
                Transform hit = blockers[i].collider.transform;
                if (!hit.IsChildOf(owner.transform) && !hit.IsChildOf(target.transform)) return false;
            }
            return true;
        }
    }
}
