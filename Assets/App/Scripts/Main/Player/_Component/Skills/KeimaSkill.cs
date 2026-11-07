using System.Collections.Generic;
using App.Audio;
using App.PlayData;
using UnityEngine;

namespace App.Main.Player
{
    public class KeimaSkill : ISkill
    {
        public string skillName => "KeimaSkill";
        public float cooldownTime => 5f;
        public float cooldownTimer { get; private set; }
        public bool isOnCooldown => cooldownTimer > 0f;
        public PlayerStatus playerStatus { get; private set; }
        public Player player { get; private set; }

        public float moveDistance = 12f;
        public float moveSpeed = 50f;
        public float contactRadius = 1.5f;
        public float attackRadius = 3.5f;
        public float damageMultiplier = 1.35f;
        public LayerMask targetMask = ~0;

        private readonly HashSet<Player> hitPlayers = new();
        private Vector3 previousPosition;
        private bool dashing;

        public void UseSkill(Player player, PlayerStatus playerStatus)
        {
            if (isOnCooldown || player == null || playerStatus == null || playerStatus.Hp.Current <= 0) return;
            this.player = player;
            this.playerStatus = playerStatus;

            var camera = player.DuelCamera;
            Vector3 forward = Vector3.ProjectOnPlane(camera != null ? camera.transform.forward : player.transform.forward, Vector3.up);
            if (forward.sqrMagnitude < .001f) forward = Vector3.ProjectOnPlane(player.transform.forward, Vector3.up);
            float speed = Mathf.Max(.01f, moveSpeed);
            float duration = Mathf.Max(.01f, moveDistance / speed);
            player.SetMovementOverride(forward.normalized * speed, duration, preserveY: true);

            previousPosition = player.transform.position;
            hitPlayers.Clear();
            dashing = true;
            cooldownTimer = cooldownTime;
            PlayRecorder.Current?.Skill(playerStatus);
            GameAudio.Play(SoundCue.Dash, player);
            player.WeaponObject?.GetComponent<WeaponPresentation>()?.PlaySkill(WeaponPresentation.SkillMotion.Dash, duration + .45f, duration);
        }

        public void UpdateSkill()
        {
            cooldownTimer = Mathf.Max(0f, cooldownTimer - Time.deltaTime);
            if (!dashing || player == null || playerStatus.Hp.Current <= 0) return;

            // FixedUpdate runs before physics. Sweep the distance the previous step actually covered.
            Vector3 position = player.transform.position;
            HitAlong(previousPosition, position, contactRadius);
            previousPosition = position;
            if (player.AppliedMovementOverrideThisStep) return;

            dashing = false;
            GameAudio.Play(SoundCue.DashStrike, player);
            HitAlong(position, position, attackRadius);
        }

        private void HitAlong(Vector3 from, Vector3 to, float radius)
        {
            Vector3 segment = to - from;
            float lengthSquared = segment.sqrMagnitude;
            var colliders = lengthSquared > .000001f
                ? Physics.OverlapCapsule(from, to, radius, targetMask, QueryTriggerInteraction.Ignore)
                : Physics.OverlapSphere(to, radius, targetMask, QueryTriggerInteraction.Ignore);
            foreach (var collider in colliders)
            {
                var target = collider.GetComponentInParent<Player>();
                if (target == null || target == player || hitPlayers.Contains(target)) continue;
                var status = target.playerStatus;
                if (status == null || status.Hp.Current <= 0) continue;

                float fraction = lengthSquared > .000001f
                    ? Mathf.Clamp01(Vector3.Dot(collider.bounds.center - from, segment) / lengthSquared)
                    : 0f;
                Vector3 source = from + segment * fraction;
                if (!ShotPhysics.HasClearPathTo(source, collider, target, Physics.DefaultRaycastLayers, player)) continue;

                hitPlayers.Add(target);
                int damage = Mathf.CeilToInt(playerStatus.AttackPoint.Current * damageMultiplier);
                status.TakeDamage(damage, source, playerStatus, HitKind.Skill);
            }
        }
    }
}
