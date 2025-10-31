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
        public float cooldown = 3f;
        public float arrowSpeed = 40f;
        public float arrowLifeTime = 8f;
        public float maxRange = 200f;
        public float falloffStart = 100f; // maxRange と同値だと無効化されるためデフォルトを調整
        public float minDamageMultiplier = 0.5f;
        public float damageMultiplier = 1.0f;
        public LayerMask hitLayers = ~0;

        // 任意: マズル位置があればセット（無ければカメラ位置）
        public Transform muzzleTransform;

        // 内部管理
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
            if (muzzleTransform != null)
            {
                origin = muzzleTransform.position;
                dir = muzzleTransform.forward;
            }
            else if (cam != null)
            {
                origin = cam.transform.position + cam.transform.forward * 0.5f;
                dir = cam.transform.forward;
            }
            else
            {
                origin = player.transform.position + player.transform.forward * 1f;
                dir = player.transform.forward;
            }

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