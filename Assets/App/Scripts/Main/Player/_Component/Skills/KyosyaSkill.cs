using System;
using UnityEngine;

namespace App.Main.Player
{
    public class KyosyaSkill : ISkill
    {
        public string skillName { get; } = "KyosyaSkill";
        public float cooldownTime { get; } = 5f;
        public bool isOnCooldown { get; private set; } = false;
        public PlayerStatus playerStatus { get; private set; }
        public Player player { get; private set; }
        public float cooldownTimer { get; private set; } = 0f;

        // 投擲パラメータ
        public float throwSpeed = 30f;
        public float fuseTime = 2f;                // 着弾せずとも fuseTime 後に爆発
        public float explosionRadius = 5f;
        public LayerMask affectLayers = ~0;        // 影響させるレイヤー（デフォルト全て）

        public void UseSkill(Player player, PlayerStatus playerStatus)
        {
            if (isOnCooldown) return;
            if (player == null) return;

            this.player = player;
            this.playerStatus = playerStatus;

            // サブ武器プレハブ参照
            var prefab = player.SubWeaponObject;
            if (prefab == null)
            {
                Debug.LogWarning("[KyosyaSkill] SubWeaponObject is not assigned.");
                return;
            }

            // カメラ優先で生成位置と方向を決定（カメラの上下成分を含める）
            Camera cam = player.GetComponentInChildren<Camera>();
            if (cam == null) cam = Camera.main;
            Vector3 forward;
            Vector3 spawnPos;
            if (cam != null)
            {
                forward = cam.transform.forward.normalized; // カメラの向きそのまま使用（上下含む）
                spawnPos = cam.transform.position + forward * 0.5f + cam.transform.up * -0.2f;
            }
            else
            {
                forward = player.transform.forward.normalized;
                spawnPos = player.transform.position + Vector3.up * 1.2f;
            }

            // インスタンス化
            var go = UnityEngine.Object.Instantiate(prefab, spawnPos, Quaternion.identity);
            go.name = "FlashbangProjectile";

            // Rigidbody を保証
            var rb = go.GetComponent<Rigidbody>();
            if (rb == null) rb = go.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.useGravity = true;
            rb.isKinematic = false;

            // Collider を保証（あればそのまま使う）
            Collider col = go.GetComponent<Collider>();
            if (col == null)
            {
                var sc = go.AddComponent<SphereCollider>();
                sc.radius = 0.2f;
                col = sc;
            }
            col.isTrigger = false;

            // GrenadeProjectile コンポーネントを付与して設定
            var proj = go.GetComponent<GrenadeProjectile>();
            if (proj == null) proj = go.AddComponent<GrenadeProjectile>();
            proj.owner = player;
            proj.fuseTime = fuseTime;
            proj.explosionRadius = explosionRadius;
            proj.affectLayers = affectLayers;

            // 生成直後に所有者との衝突を無視しておく（Instantiate 後すぐ呼ぶ）
            try
            {
                var projCols = go.GetComponentsInChildren<Collider>();
                var ownerCols = player.GetComponentsInChildren<Collider>();
                if (projCols != null && ownerCols != null)
                {
                    foreach (var pcol in projCols)
                    {
                        if (pcol == null) continue;
                        foreach (var ocol in ownerCols)
                        {
                            if (ocol == null) continue;
                            Physics.IgnoreCollision(pcol, ocol, true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[KyosyaSkill] IgnoreCollision setup failed: " + ex.Message);
            }

            // 投げる（初速） - rb.velocity を使う
            rb.linearVelocity = forward * throwSpeed;

            // クールダウン開始
            isOnCooldown = true;
            cooldownTimer = cooldownTime;
        }
        public void UpdateSkill()
        {
            if (isOnCooldown)
            {
                cooldownTimer -= Time.deltaTime;
                if (cooldownTimer <= 0f)
                {
                    isOnCooldown = false;
                    cooldownTimer = 0f;
                }
            }
        }
    }
}