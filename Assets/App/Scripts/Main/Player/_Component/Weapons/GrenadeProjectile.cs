using System.Collections.Generic;
using App.Audio;
using UnityEngine;

namespace App.Main.Player
{
    public class GrenadeProjectile : MonoBehaviour
    {
        // The tetuhau mesh has a local radius of 0.01 at a uniform prefab scale of 52.592438.
        const float ProjectileRadius = .526f;
        const float LevelThrowAngle = 20f;
        // Bring a 48 m/s throw back down within the length of the duel board.
        const float GravityMultiplier = 6f;
        static RaycastHit[] sweepHits = new RaycastHit[8];

        public Player owner;
        public float fuseTime = 2f;
        public float explosionRadius = 5f;
        public LayerMask affectLayers = ~0;

        Vector3 velocity;
        Vector3 impactNormal = Vector3.up;
        float remaining;
        bool launched;
        bool exploded;

        internal static Vector3 LaunchOrigin(Player owner)
        {
            Camera camera = owner.DuelCamera != null ? owner.DuelCamera : owner.GetComponentInChildren<Camera>(true);
            Transform aim = camera != null ? camera.transform : owner.transform;
            Vector3 toHand = aim.right * .3f - aim.up * .3f + aim.forward * .45f;
            Vector3 hand = Sweep(owner, aim.position, toHand, out bool blocked, out _);
            return blocked ? aim.position : hand;
        }

        internal static Vector3 LobVelocity(Transform view, float speed)
        {
            float elevation = view.forward.y;
            // Keep raised throws below the angle of maximum range so looking higher still reaches farther.
            float limit = elevation >= 0f ? 42f : -50f;
            float angle = Mathf.Lerp(LevelThrowAngle, limit, Mathf.Abs(elevation)) * Mathf.Deg2Rad;
            Vector3 heading = Vector3.Cross(view.right, Vector3.up).normalized;
            return speed * (heading * Mathf.Cos(angle) + Vector3.up * Mathf.Sin(angle));
        }

        // The guide and the live throw advance in identical fixed steps, including the last partial fuse step.
        internal static Vector3 Step(Player owner, Vector3 position, ref Vector3 velocity, float delta,
            out bool hit, out Vector3 normal)
        {
            Vector3 gravity = Physics.gravity * GravityMultiplier;
            Vector3 displacement = (velocity + .5f * gravity * delta) * delta;
            velocity += gravity * delta;
            return Sweep(owner, position, displacement, out hit, out normal);
        }

        static Vector3 Sweep(Player owner, Vector3 origin, Vector3 displacement, out bool hit, out Vector3 normal)
        {
            hit = false;
            normal = Vector3.up;
            float distance = displacement.magnitude;
            if (distance == 0f) return origin;

            Vector3 direction = displacement / distance;
            int count;
            // A full buffer can omit the nearest wall. Grow only in unusually crowded geometry.
            while ((count = Physics.SphereCastNonAlloc(origin, ProjectileRadius, direction, sweepHits, distance,
                       Physics.AllLayers, QueryTriggerInteraction.Ignore)) == sweepHits.Length)
                sweepHits = new RaycastHit[sweepHits.Length * 2];

            float nearest = distance;
            for (int i = 0; i < count; i++)
            {
                RaycastHit candidate = sweepHits[i];
                if (candidate.distance > nearest) continue;
                if (owner != null && candidate.collider.transform.IsChildOf(owner.transform)) continue;
                hit = true;
                nearest = candidate.distance;
                normal = candidate.normal;
            }

            // With a starting overlap Unity returns distance zero and an empty contact point.
            // The center along the sweep is also the position the blast radius must use.
            return hit ? origin + direction * nearest : origin + displacement;
        }

        public void Launch(Player owner, Vector3 velocity, float fuse, float radius, LayerMask affectLayers)
        {
            this.owner = owner;
            this.velocity = velocity;
            this.affectLayers = affectLayers;
            fuseTime = Mathf.Max(0f, fuse);
            explosionRadius = Mathf.Max(0f, radius);
            remaining = fuseTime;
            launched = true;

            // The recovered model includes a dynamic convex collider. A second physics simulation
            // would move the model away from the guide and expose it to its own sweep.
            foreach (var collider in GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            if (TryGetComponent<Rigidbody>(out var body))
            {
                body.collisionDetectionMode = CollisionDetectionMode.Discrete;
                body.useGravity = false;
                body.isKinematic = true;
            }
            if (remaining == 0f) Explode();
        }

        void FixedUpdate()
        {
            if (!launched || exploded) return;
            float delta = Mathf.Min(Time.fixedDeltaTime, remaining);
            transform.position = Step(owner, transform.position, ref velocity, delta, out bool hit, out impactNormal);
            remaining -= delta;
            if (hit || remaining <= 0f) Explode();
        }

        void Explode()
        {
            if (exploded) return;
            exploded = true;
            Vector3 center = transform.position;
            GameAudio.Play(SoundCue.FlashBurst, owner, center);
            CombatVisuals.FlashbangBurst(owner, center, impactNormal, explosionRadius);

            var applied = new HashSet<Player>();
            foreach (var collider in Physics.OverlapSphere(center, explosionRadius, affectLayers, QueryTriggerInteraction.Ignore))
            {
                var player = collider.GetComponentInParent<Player>();
                if (player != null && player.playerStatus != null && applied.Add(player))
                    player.playerStatus.EffectList.AddEffect(new FlashBangEffect());
            }
            Destroy(gameObject);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}
