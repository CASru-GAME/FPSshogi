using System;
using UnityEngine;

namespace App.Main.Player
{
    public class LongGunPrimary : IPrimaryAction
    {
        public string primaryActionName => "LongGunPrimary";
        public Player player { get; private set; }
        public PlayerStatus playerStatus { get; private set; }

        // 調整パラメータ
        public float cooldown = 4f;                 // 発射間隔（秒）
        public float maxRange = 999f;                  // ヒットスキャン最大距離
        public float falloffStart = 100f;              // ここからダメージ減衰開始
        public float minDamageMultiplier = 0.4f;      // 最大距離での最小倍率
        public float damageMultiplier = 1f;           // 基本倍率（AttackPoint に掛ける）
        public LayerMask hitLayers = ~0;              // レイヤーフィルタ
        public string shootAnimationName = "火縄銃長_001|火縄銃長用";   // 撃つアニメ（存在すれば再生）
        public GameObject muzzleFlashPrefab;          // 任意：マズルフラッシュを出すプレハブ
        public Transform muzzleTransform;             // 任意：マズルの位置（無ければカメラ位置）

        // 実行管理
        private float lastUseTime = -999f;

        public void PrimaryAction(Player player, PlayerStatus playerStatus)
        {
            if (player == null || playerStatus == null) return;
            this.player = player;
            this.playerStatus = playerStatus;

            // クールダウンチェック（Time ベースで管理）
            if (Time.time - lastUseTime < cooldown) return;

            // カメラ取得
            Camera cam = player.GetComponentInChildren<Camera>();
            if (cam == null) cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[LongGunPrimary] Camera not found.");
                return;
            }

            // アニメ再生（PlayerWeapon があればそれを優先してクリップ長を取得して再生）
            float animDuration = 0f;
            PlayerWeapon pw = null;
            if (player.WeaponObject != null)
                pw = player.WeaponObject.GetComponentInChildren<PlayerWeapon>();
            if (pw == null)
                pw = player.GetComponentInChildren<PlayerWeapon>();

            if (pw != null)
            {
                animDuration = pw.PlayAnimation(shootAnimationName);
            }
            else
            {
                // 武器に PlayerWeapon が無ければ任意の Animator を探して再生を試みる
                var weaponGO = player.WeaponObject ?? player.gameObject;
                var anim = weaponGO.GetComponentInChildren<Animator>();
                if (anim != null && !string.IsNullOrEmpty(shootAnimationName))
                {
                    try { anim.Play(shootAnimationName, 0, 0f); }
                    catch { }
                }
            }

            // マズルフラッシュ
            try
            {
                if (muzzleFlashPrefab != null)
                {
                    Transform mtx = muzzleTransform != null ? muzzleTransform : (pw != null ? pw.transform : player.transform);
                    if (mtx != null)
                    {
                        var mf = UnityEngine.Object.Instantiate(muzzleFlashPrefab, mtx.position, mtx.rotation);
                        mf.transform.SetParent(mtx, true);
                        UnityEngine.Object.Destroy(mf, 1.0f);
                    }
                }
            }
            catch { }

            // ヒットスキャン（即時）
            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, maxRange, hitLayers.value))
            {
                // プレイヤーに当たったか確認
                var targetPlayer = hit.collider.GetComponentInParent<Player>();
                if (targetPlayer != null && targetPlayer != player)
                {
                    // 基本ダメージ
                    int baseAtk = 0;
                    try
                    {
                        if (playerStatus != null && playerStatus.AttackPoint != null)
                            baseAtk = playerStatus.AttackPoint.Current;
                    }
                    catch { baseAtk = 0; }

                    // 距離減衰：falloffStart までフル、そこから maxRange で minDamageMultiplier まで線形補間
                    float dist = hit.distance;
                    float fallMul = 1f;
                    if (dist <= falloffStart)
                    {
                        fallMul = 1f;
                    }
                    else if (dist >= maxRange)
                    {
                        fallMul = minDamageMultiplier;
                    }
                    else
                    {
                        float t = (dist - falloffStart) / Mathf.Max(0.0001f, (maxRange - falloffStart));
                        fallMul = Mathf.Lerp(1f, minDamageMultiplier, Mathf.Clamp01(t));
                    }

                    int damage = Mathf.CeilToInt(baseAtk * damageMultiplier * fallMul);

                    // ダメージ適用（PlayerStatus の TakeDamage を使う）
                    try
                    {
                        var ts = targetPlayer.playerStatus;
                        if (ts != null)
                        {
                            ts.TakeDamage(damage);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[LongGunPrimary] ApplyDamage failed: {ex.Message}");
                    }
                }
                else
                {
                    // 環境ヒット時のエフェクト等をここで出しても良い（省略）
                }
            }

            // 発射成功としてクールダウンを記録（アニメ長を反映させたい場合は Mathf.Max を使う）
            lastUseTime = Time.time;
            // もしアニメがクールダウンに影響するなら以下のようにすることも可能:
            // lastUseTime = Time.time - (animDuration > cooldown ? -animDuration : 0f);
        }
    }
}
