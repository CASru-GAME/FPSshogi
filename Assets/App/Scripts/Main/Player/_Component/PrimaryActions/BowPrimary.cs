using App.Audio;
using UnityEngine;

namespace App.Main.Player
{
    public class BowPrimary : IPrimaryAction
    {
        public const float FullDrawSeconds = 1.2f;
        const float QuickTapSeconds = .12f;
        const float FullDrawDamageScale = 1.5f;
        const float FullDrawSpeedScale = 1.6f;

        public string primaryActionName => "BowPrimary";
        public Player player { get; private set; }
        public PlayerStatus playerStatus { get; private set; }
        public float cooldown = 1.7f;
        public float arrowSpeed = 40f;
        public float arrowLifeTime = 8f;
        public float maxRange = 100f;
        public float falloffStart = 28f;
        public float minDamageMultiplier = .55f;
        public float damageMultiplier = 1f;
        public LayerMask hitLayers = ~0;
        public Transform muzzleTransform;

        float lastUseTime = float.NegativeInfinity;
        float drawStarted;
        public bool IsDrawing { get; private set; }
        public float DrawAmount => IsDrawing ? Mathf.Clamp01((Time.time - drawStarted) / FullDrawSeconds) : 0f;
        public float MovementScale => Mathf.Lerp(1f, .75f, DrawAmount);
        public float CooldownRemaining => Mathf.Max(0f, cooldown - (Time.time - lastUseTime));
        public float CooldownDuration => cooldown;

        public void BeginDraw(Player archer, PlayerStatus status)
        {
            if (IsDrawing || archer == null || status == null || status.Hp.Current <= 0)
                return;
            player = archer;
            playerStatus = status;
            // Holding through recovery starts pulling the next arrow when it is ready.
            drawStarted = Time.time + CooldownRemaining;
            IsDrawing = true;
        }

        public void CancelDraw() => IsDrawing = false;

        public void ReleaseDraw()
        {
            if (IsDrawing) PrimaryAction(player, playerStatus);
        }

        // A direct attack is a quick shot. Live input calls this on release with
        // the draw still active so the shot leaves the same pose the player sees.
        public void PrimaryAction(Player archer, PlayerStatus status)
        {
            if (archer == null || status == null || status.Hp.Current <= 0 || CooldownRemaining > 0)
            {
                CancelDraw();
                return;
            }
            player = archer;
            playerStatus = status;
            float draw = DrawAmount;
            float power = Mathf.InverseLerp(QuickTapSeconds / FullDrawSeconds, 1f, draw);
            float damageScale = damageMultiplier * Mathf.Lerp(1f, FullDrawDamageScale, power);
            float speed = arrowSpeed * Mathf.Lerp(1f, FullDrawSpeedScale, power);

            var display = archer.WeaponObject != null ? archer.WeaponObject.GetComponent<WeaponPresentation>() : null;
            display?.RefreshPose();
            Camera view = archer.GetComponentInChildren<Camera>() ?? Camera.main;
            Transform aim = view != null ? view.transform : archer.transform;
            Vector3 origin = aim.position + aim.forward * (view != null ? .5f : 1f);
            Vector3 direction = aim.forward;
            RaycastHit obstruction = default;
            if (muzzleTransform != null)
            {
                origin = muzzleTransform.position;
                direction = muzzleTransform.forward;
            }
            else if (display != null && display.nockedArrow != null)
            {
                origin = display.nockedArrow.position;
                var sight = new Ray(aim.position, aim.forward);
                Vector3 target = ShotPhysics.Raycast(sight, maxRange, hitLayers, archer, out var hit)
                    ? hit.point : sight.GetPoint(maxRange);
                var toNock = origin - aim.position;
                if (toNock.sqrMagnitude > .0001f)
                    ShotPhysics.Raycast(new Ray(aim.position, toNock.normalized), toNock.magnitude, hitLayers, archer, out obstruction);
                direction = (target - origin).normalized;
            }
            if (obstruction.collider == null && archer.SubWeaponObject == null)
            {
                CancelDraw();
                Debug.LogWarning("[BowPrimary] Arrow prefab is missing.");
                return;
            }

            if (display != null) display.Attack();
            else archer.GetComponentInChildren<PlayerWeapon>()?.PlayAnimation("弓_003|矢用");
            CancelDraw();
            lastUseTime = Time.time;
            App.PlayData.PlayRecorder.Current?.Attack(status);
            GameAudio.Play(SoundCue.BowRelease, archer);

            // Resolve a wall or opponent between the camera and the visible arrow
            // before spawning a solid projectile inside that collider.
            if (obstruction.collider != null)
            {
                var target = obstruction.collider.GetComponentInParent<Player>();
                if (target != null && target != archer)
                    target.playerStatus?.TakeDamage(Mathf.CeilToInt(status.AttackPoint.Current * damageScale), aim.position, status);
                if (target == null) GameAudio.Play(SoundCue.ArrowImpact, archer, obstruction.point);
                CombatVisuals.Impact(archer, obstruction.point);
                return;
            }

            var arrow = Object.Instantiate(archer.SubWeaponObject, origin, Quaternion.LookRotation(direction));
            if (display != null && display.nockedArrow != null)
                arrow.transform.localScale = display.nockedArrow.lossyScale;
            var lifetime = arrow.AddComponent<CombatVisualLifetime>();
            lifetime.owner = archer;
            lifetime.remaining = arrowLifeTime;
            var projectile = arrow.GetComponent<ProjectileArrow>() ?? arrow.AddComponent<ProjectileArrow>();
            projectile.Initialize(archer, status, damageScale, falloffStart, maxRange, minDamageMultiplier, hitLayers);
            projectile.Launch(direction, speed, arrowLifeTime);
        }
    }
}
