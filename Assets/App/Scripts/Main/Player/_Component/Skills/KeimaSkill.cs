// ...existing code...
using System;
using System.Collections.Generic;
using UnityEngine;

namespace App.Main.Player
{
    public class KeimaSkill : ISkill
    {
        public string skillName { get; private set; } = "KeimaSkill";
        public float cooldownTime { get; private set; } = 5f;
        public float cooldownTimer { get; private set; } = 0f;
        public bool isOnCooldown { get; private set; } = false;

        public PlayerStatus playerStatus { get; private set; }
        public Player player { get; private set; }

        // 移動・攻撃パラメータ
        public float moveDistance = 10f;     // 前方に移動する距離
        public float moveSpeed = 50f;       // 水平方向の速度（m/s）
        public float attackRadius = 10f;   // 到達地点での攻撃半径
        public float damageMultiplier = 1.5f; // 自身の AttackPoint に対する倍率
        public LayerMask targetMask = ~0;   // 当たり判定で検出するレイヤー（必要なら調整）

        // 実行状態
        private bool skillActive = false;
        private float skillTimer = 0f;
        private float moveDuration = 0f;
        private Vector3 moveDirection = Vector3.forward;

        public void UseSkill(Player player, PlayerStatus playerStatus)
        {
            if (isOnCooldown) return;
            if (player == null || playerStatus == null) return;

            this.player = player;
            this.playerStatus = playerStatus;

            // カメラ基準の前方向（上下成分は使わず水平化して移動角度のみ反映したい場合は ProjectOnPlane を使う）
            var cam = player.GetComponentInChildren<Camera>();
            Vector3 forward;
            if (cam != null)
            {
                // カメラの向きの水平成分を取り、上下向きは無視して真っ直ぐ進む
                forward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
                if (forward.sqrMagnitude < 0.001f) forward = player.transform.forward;
            }
            else
            {
                forward = Vector3.ProjectOnPlane(player.transform.forward, Vector3.up).normalized;
            }

            moveDirection = forward;

            // 移動時間を計算（ゼロ除算回避）
            float effectiveSpeed = Mathf.Max(0.01f, moveSpeed);
            moveDuration = Mathf.Max(0.01f, moveDistance / effectiveSpeed);

            // 水平速度ベクトル（Yは保持するため 0 にしておく）
            Vector3 desiredVelocity = new Vector3(forward.x * effectiveSpeed, 0f, forward.z * effectiveSpeed);

            // プレイヤーに移動上書きを依頼（Y成分は preserveY=true で保持）
            player.SetMovementOverride(desiredVelocity, moveDuration, preserveY: true);

            // 念のため Rigidbody に軽い水平インパルスを与える（環境により不要）
            var rb = player.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                Vector3 impulse = new Vector3(forward.x * (effectiveSpeed * 0.15f), 0f, forward.z * (effectiveSpeed * 0.15f));
                rb.AddForce(impulse, ForceMode.VelocityChange);
            }

            // ステート初期化
            skillActive = true;
            skillTimer = 0f;

            // クールダウン開始
            isOnCooldown = true;
            cooldownTimer = cooldownTime;
        }

        public void UpdateSkill()
        {
            // cooldown 更新
            if (isOnCooldown)
            {
                cooldownTimer -= Time.deltaTime;
                if (cooldownTimer <= 0f)
                {
                    isOnCooldown = false;
                    cooldownTimer = 0f;
                }
            }

            if (!skillActive || player == null) return;

            skillTimer += Time.deltaTime;

            // 移動時間が終わったら到達地点で攻撃を実行して終了
            if (skillTimer >= moveDuration)
            {
                DoLandingAttack();
                skillActive = false;
                skillTimer = 0f;
                player.ClearMovementOverride();
            }
        }

        private void DoLandingAttack()
        {
            Vector3 origin = player.transform.position;

            // OverlapSphere で周囲を検出
            var cols = Physics.OverlapSphere(origin, attackRadius, targetMask);
            var applied = new HashSet<Player>();
            Debug.Log("[KeimaSkill] Performing landing attack at " + origin + " with radius " + attackRadius + ", detected colliders: " + cols.Length);
            foreach (var c in cols)
            {
                Debug.Log("[KeimaSkill] Detected collider: " + c.name);
                if (c == null) continue;
                var p = c.GetComponentInParent<Player>();
                if (p == null) continue;
                if (p == player) continue; // 自分は攻撃しない（必要なら外す）
                if (applied.Contains(p)) continue;
                applied.Add(p);

                var targetStatus = p.playerStatus;
                Debug.Log("[KeimaSkill] Target player found: " + p.name);
                if (targetStatus == null) continue;
                Debug.Log("[KeimaSkill] Target player status found for: " + p.name);

                // ダメージ計算：自身の AttackPoint を参照して倍率をかける
                int baseAttack = playerStatus.AttackPoint.Current;
                int damage = Mathf.CeilToInt(baseAttack * damageMultiplier);
                Debug.Log("[KeimaSkill] Calculated damage to " + p.name + ": " + damage + " (Base Attack: " + baseAttack + ", Multiplier: " + damageMultiplier + ")");
                // ダメージを適用（PlayerStatus.TakeDamage を使う）
                try
                {
                    targetStatus.TakeDamage(damage);
                    Debug.Log("[KeimaSkill] Applied " + damage + " damage to " + p.name);
                }
                catch (Exception)
                {
                    Debug.LogError("[KeimaSkill] Error occurred while applying damage to " + p.name);
                    // 念のため安全に
                }
            }

            Debug.Log($"[KeimaSkill] Sliding attack at {origin}, radius={attackRadius}, targets={applied.Count}");
        }
    }
}
// ...existing code...