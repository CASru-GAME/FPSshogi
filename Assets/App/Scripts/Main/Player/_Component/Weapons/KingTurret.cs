using System;
using System.Collections;
using UnityEngine;

namespace App.Main.Player
{
    // タレット本体：自動索敵してヒットスキャン攻撃を行う
    public class KingTurret : MonoBehaviour
    {
        Player owner;
        PlayerStatus ownerStatus;

        float fireInterval = 0.6f;
        float range = 40f;
        float falloffStart = 20f;
        float minDamageMul = 0.5f;
        float damageMultiplier = 1f;
        LayerMask hitLayers = ~0;

        float nextFireTime = 0f;

        // 射出位置（子オブジェクト名 "Muzzle" を探すか、自身の位置を使う）
        Transform muzzle;
        // 子の銃オブジェクトにある Animator を使って射撃アニメを再生
        Animator gunAnimator;
        string shootAnimName;

        public void Initialize(Player owner, PlayerStatus ownerStatus,
            float fireInterval, float range, float falloffStart, float minDamageMul, float damageMultiplier, LayerMask hitLayers, string shootAnimName)
        {
            this.owner = owner;
            this.ownerStatus = ownerStatus;
            this.fireInterval = Mathf.Max(0.01f, fireInterval);
            this.range = Mathf.Max(0.01f, range);
            this.falloffStart = Mathf.Max(0f, falloffStart);
            this.minDamageMul = Mathf.Clamp01(minDamageMul);
            this.damageMultiplier = damageMultiplier;
            this.hitLayers = hitLayers;
            this.shootAnimName = shootAnimName;

            // probe for muzzle and gun animator
            muzzle = transform.Find("Muzzle") ?? transform;
            // gun object might be a child; try to find Animator on children
            gunAnimator = GetComponentInChildren<Animator>();

            nextFireTime = Time.time + UnityEngine.Random.Range(0f, fireInterval * 0.5f); // 少しずらす
        }

        void Update()
        {
            if (Time.time < nextFireTime) return;

            // ターゲットを取得（最も近い敵プレイヤー）
            var target = FindNearestEnemyPlayer();
            if (target == null)
            {
                nextFireTime = Time.time + fireInterval;
                return;
            }

            // 向きを合わせる（ゆっくりや即時でも可）
            Vector3 aimDir = (GetTargetAimPoint(target) - muzzle.position).normalized;
            if (aimDir.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(new Vector3(aimDir.x, 0f, aimDir.z)); // 水平回転のみ
            }

            // アニメ再生（銃子オブジェクトの Animator を利用）
            if (gunAnimator != null && !string.IsNullOrEmpty(shootAnimName))
            {
                try { gunAnimator.Play(shootAnimName, 0, 0f); } catch { }
            }

            // 即時ヒットスキャン
            Ray ray = new Ray(muzzle.position, aimDir);
            if (Physics.Raycast(ray, out RaycastHit hit, range, hitLayers.value))
            {
                var hitPlayer = hit.collider.GetComponentInParent<Player>();
                if (hitPlayer != null && hitPlayer != owner)
                {
                    ApplyDamageTo(hitPlayer, hit.distance);
                }
            }

            nextFireTime = Time.time + fireInterval;
        }

        private Player FindNearestEnemyPlayer()
        {
            Player best = null;
            float bestDist = float.MaxValue;
            var all = UnityEngine.Object.FindObjectsOfType<Player>();
            foreach (var p in all)
            {
                if (p == null) continue;
                if (p == owner) continue;
                // 敵味方判定が必要ならここで判定する（現状は単純に owner 以外を敵とする）
                Vector3 aimPoint = GetTargetAimPoint(p);
                float d = Vector3.Distance(transform.position, aimPoint);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = p;
                }
            }
            return best;
        }

        private Vector3 GetTargetAimPoint(Player p)
        {
            if (p == null) return transform.position;
            // 目標位置: player の中心（transform.position）＋頭方向オフセット
            var cam = p.GetComponentInChildren<Camera>();
            if (cam != null) return cam.transform.position;
            return p.transform.position + Vector3.up * 1.0f;
        }

        private void ApplyDamageTo(Player target, float distance)
        {
            if (target == null) return;

            int baseAtk = 0;
            try
            {
                if (ownerStatus != null && ownerStatus.AttackPoint != null)
                    baseAtk = ownerStatus.AttackPoint.Current;
            }
            catch { baseAtk = 0; }

            float fallMul = 1f;
            if (distance <= falloffStart) fallMul = 1f;
            else if (distance >= range) fallMul = minDamageMul;
            else
            {
                float t = (distance - falloffStart) / Mathf.Max(0.0001f, (range - falloffStart));
                fallMul = Mathf.Lerp(1f, minDamageMul, Mathf.Clamp01(t));
            }

            int damage = Mathf.CeilToInt(baseAtk * damageMultiplier * fallMul);

            try
            {
                var ts = target.playerStatus;
                if (ts != null)
                {
                    ts.TakeDamage(damage);
                }
            }
            catch { }
        }

        void OnDestroy()
        {
            // 追加のクリーンアップがあればここに
        }
    }
}
