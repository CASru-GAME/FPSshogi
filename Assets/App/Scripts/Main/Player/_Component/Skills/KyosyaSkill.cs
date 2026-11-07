using App.Audio;
using App.PlayData;
using UnityEngine;

namespace App.Main.Player
{
    public class KyosyaSkill : ISkill
    {
        public string skillName => "KyosyaSkill";
        public float cooldownTime => 8f;
        public float cooldownTimer { get; private set; }
        public bool isOnCooldown => cooldownTimer > 0f;
        public PlayerStatus playerStatus { get; private set; }
        public Player player { get; private set; }
        public float throwSpeed = 48f;
        public float fuseTime = 2f;
        public float explosionRadius = 5f;
        public LayerMask affectLayers = ~0;

        private FlashbangAimPreview preview;
        internal bool IsAiming => preview != null;
        private Vector3 LaunchVelocity => GrenadeProjectile.LobVelocity(player.DuelCamera.transform, throwSpeed);

        public void UseSkill(Player player, PlayerStatus playerStatus)
        {
            if (IsAiming)
            {
                CancelAim();
                return;
            }
            if (isOnCooldown || player == null || playerStatus == null || playerStatus.Hp.Current <= 0 ||
                player.SubWeaponObject == null || player.DuelCamera == null) return;

            this.player = player;
            this.playerStatus = playerStatus;
            preview = new FlashbangAimPreview(player);
            RefreshAim();
        }

        internal void RefreshAim()
        {
            if (!IsAiming) return;
            if (playerStatus.Hp.Current <= 0)
            {
                CancelAim();
                return;
            }
            preview.Draw(player, GrenadeProjectile.LaunchOrigin(player), LaunchVelocity, fuseTime, explosionRadius);
        }

        internal void CancelAim()
        {
            preview?.Remove();
            preview = null;
        }

        internal void Throw()
        {
            if (!IsAiming) return;
            CancelAim();
            if (playerStatus.Hp.Current <= 0 || player.SubWeaponObject == null) return;

            var prefab = player.SubWeaponObject;
            var grenade = Object.Instantiate(prefab, GrenadeProjectile.LaunchOrigin(player), prefab.transform.rotation);
            grenade.name = "FlashbangProjectile";
            var life = grenade.AddComponent<CombatVisualLifetime>();
            life.owner = player;
            life.remaining = fuseTime + 1f;
            var projectile = grenade.GetComponent<GrenadeProjectile>() ?? grenade.AddComponent<GrenadeProjectile>();
            projectile.Launch(player, LaunchVelocity, fuseTime, explosionRadius, affectLayers);

            cooldownTimer = cooldownTime;
            PlayRecorder.Current?.Skill(playerStatus);
            GameAudio.Play(SoundCue.Throw, player);
            player.WeaponObject?.GetComponent<WeaponPresentation>()?.PlaySkill(WeaponPresentation.SkillMotion.Throw, .55f);
        }

        public void UpdateSkill() => cooldownTimer = Mathf.Max(0f, cooldownTimer - Time.deltaTime);
    }
}
