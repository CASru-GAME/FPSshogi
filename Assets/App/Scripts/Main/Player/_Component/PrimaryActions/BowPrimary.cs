using App.Audio;
using System;
using UnityEngine;

namespace App.Main.Player
{
    public class BowPrimary : IPrimaryAction
    {
        public string primaryActionName => "BowPrimary";
        public Player player { get; private set; }
        public PlayerStatus playerStatus { get; private set; }

        // 調整パラメータ
        public float cooldown = 1.7f;
        public float arrowSpeed = 40f;
        public float arrowLifeTime = 8f;
        public float maxRange = 100f;
        public float falloffStart = 28f;
        public float minDamageMultiplier = 0.55f;
        public float damageMultiplier = 1.0f;
        public LayerMask hitLayers = ~0;

        // 任意: マズル位置があればセット（無ければカメラ位置）
        public Transform muzzleTransform;

        // 内部管理
        public float CooldownRemaining => Mathf.Max(0f, cooldown - (Time.time - lastUseTime));
        public float CooldownDuration => cooldown;
        private float lastUseTime = -999f;

        public void PrimaryAction(Player player, PlayerStatus playerStatus)
        {
            if (player == null || playerStatus == null) return;
            this.player = player;
            this.playerStatus = playerStatus;

            // クールダウンは Time ベースで管理（UpdatePrimaryAction が呼ばれない場合でも動作する）
            if (Time.time - lastUseTime < cooldown) return;

            // 発射元と方向
            Camera cam = player.GetComponentInChildren<Camera>() ?? Camera.main;
            Vector3 origin;
            Vector3 dir;
            RaycastHit blockedLaunch = default;
            if (muzzleTransform != null)
            {
                origin = muzzleTransform.position;
                dir = muzzleTransform.forward;
            }
            else if (cam != null)
            {
                origin = cam.transform.position + cam.transform.forward * 0.5f;
                dir = cam.transform.forward;
                var display = player.WeaponObject?.GetComponent<WeaponPresentation>();
                if (display != null && display.nockedArrow != null)
                {
                    origin = display.nockedArrow.position;
                    // Aim at the reticle from the visible arrow, rather than teleporting
                    // the projectile from the bow to a point in front of the camera.
                    Vector3 target = ShotPhysics.Raycast(new Ray(cam.transform.position, cam.transform.forward), maxRange, hitLayers, player, out var aimHit)
                        ? aimHit.point : cam.transform.position + cam.transform.forward * maxRange;
                    Vector3 toNock = origin - cam.transform.position;
                    if (toNock.sqrMagnitude > .0001f)
                        ShotPhysics.Raycast(new Ray(cam.transform.position, toNock.normalized), toNock.magnitude, hitLayers, player, out blockedLaunch);
                    dir = (target - origin).normalized;
                }
            }
            else
            {
                origin = player.transform.position + player.transform.forward * 1f;
                dir = player.transform.forward;
            }

            if(player.SubWeaponObject==null&&blockedLaunch.collider==null)
            {
                Debug.LogWarning("[BowPrimary] SubWeaponObject (arrow prefab) is not assigned on player.");
                lastUseTime=Time.time;return;
            }
            App.PlayData.PlayRecorder.Current?.Attack(playerStatus);
            GameAudio.Play(SoundCue.BowRelease,player);
            // 射撃アニメ（武器に PlayerWeapon があれば優先して再生）
            try
            {
                PlayerWeapon pw = null;
                if (player.WeaponObject != null) pw = player.WeaponObject.GetComponentInChildren<PlayerWeapon>();
                if (pw == null) pw = player.GetComponentInChildren<PlayerWeapon>();
                if (pw != null)
                    pw.PlayAnimation("弓_003|矢用");
                else
                {
                    var anim = (player.WeaponObject ?? player.gameObject).GetComponentInChildren<Animator>();
                    if (anim != null) anim.Play("弓_003|矢用", 0, 0f);
                }
            }
            catch { }

            // The bow can visibly cross near cover. Resolve that contact immediately:
            // moving just the projectile pivot back left its solid hull inside the wall.
            if (blockedLaunch.collider != null)
            {
                var target = blockedLaunch.collider.GetComponentInParent<Player>();
                if (target != null && target != player)
                    target.playerStatus?.TakeDamage(Mathf.CeilToInt(playerStatus.AttackPoint.Current * damageMultiplier), cam.transform.position, playerStatus);
                CombatVisuals.Impact(player, blockedLaunch.point);
                if(target==null)GameAudio.Play(SoundCue.ArrowImpact,player,blockedLaunch.point);
                lastUseTime = Time.time;
                return;
            }

            // 矢プレハブは player.SubWeaponObject を想定（プレハブ参照が入っていること）
            GameObject arrowPrefab = player.SubWeaponObject;
            if (arrowPrefab == null)
            {
                Debug.LogWarning("[BowPrimary] SubWeaponObject (arrow prefab) is not assigned on player.");
                lastUseTime = Time.time;
                return;
            }

            // インスタンス作成
            var arrowGO = UnityEngine.Object.Instantiate(arrowPrefab, origin, Quaternion.LookRotation(dir));
            if (player.WeaponObject != null)
            {
                var presentation = player.WeaponObject.GetComponent<WeaponPresentation>();
                if (presentation != null && presentation.nockedArrow != null)
                    arrowGO.transform.localScale = presentation.nockedArrow.lossyScale;
            }
            var lifetime=arrowGO.AddComponent<CombatVisualLifetime>();lifetime.owner=player;lifetime.remaining=arrowLifeTime;

            // ProjectileArrow コンポーネントを取得/追加して初期化
            var proj = arrowGO.GetComponent<ProjectileArrow>();
            if (proj == null)
                proj = arrowGO.AddComponent<ProjectileArrow>();

            proj.Initialize(player, playerStatus, damageMultiplier, falloffStart, maxRange, minDamageMultiplier, hitLayers);

            // Rigidbody があれば物理で飛ばす。velocity プロパティを使う（linearVelocity は不正）
            var rb = arrowGO.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = dir.normalized * arrowSpeed;
                proj.SetAutoDestroy(arrowLifeTime);
            }
            else
            {
                proj.Launch(dir.normalized, arrowSpeed, arrowLifeTime);
            }

            // 発射時刻を記録してクールダウン開始
            lastUseTime = Time.time;
        }

        // 外部呼び出し用（もし外部から Update 呼ぶなら補助）
        public void UpdatePrimaryAction()
        {
            // 今回は Time ベースの lastUseTime を使っているため特別な処理不要
        }
    }
}
