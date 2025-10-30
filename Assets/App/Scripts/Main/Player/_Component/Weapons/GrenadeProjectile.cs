using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace App.Main.Player
{
    public class GrenadeProjectile : MonoBehaviour
    {
        public Player owner;
        public float fuseTime = 2f;
        public float explosionRadius = 5f;
        public LayerMask affectLayers = ~0;
        public float collisionEnableDelay = 0.1f;

        private bool exploded = false;
        private float timer = 0f;

        // 発射体と所有者のコライダー無視管理
        private Collider[] projectileColliders;
        private bool ownerCollisionIgnored = false;

        void Awake()
        {
            // 発射体側のコライダーを取得しておく（無効化はしない）
            projectileColliders = GetComponentsInChildren<Collider>();
        }

        void Start()
        {
            timer = fuseTime;

            // 指定秒数だけ待ってから所有者無視とコライダー設定を行う
            StartCoroutine(EnableCollisionsAfterDelay(collisionEnableDelay));
        }

        // 秒数指定版：delaySeconds 秒待ってから衝突を有効化／所有者無視を設定する
        IEnumerator EnableCollisionsAfterDelay(float delaySeconds)
        {
            if (delaySeconds > 0f)
            {
                yield return new WaitForSeconds(delaySeconds);
            }
            else
            {
                // デフォルトの安全策：FixedUpdate を2フレーム待つ
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();
            }

            // 所有者との衝突無視を試行（owner が未設定なら Update 側で補填）
            TryIgnoreOwnerCollisions();

            // 念のためコライダーが無効化されていたら有効化する（通常は既に true）
            if (projectileColliders != null)
            {
                foreach (var c in projectileColliders)
                {
                    if (c != null && !c.enabled) c.enabled = true;
                }
            }
        }

        void Update()
        {
            // 所有者設定が Start 後に行われる可能性があるので、まだ無視していなければ毎フレーム試す
            if (!ownerCollisionIgnored && owner != null)
            {
                TryIgnoreOwnerCollisions();
            }

            if (exploded) return;
            timer -= Time.deltaTime;
            if (timer <= 0f) Explode();
        }

        void OnCollisionEnter(Collision collision)
        {
            if (exploded) return;

            // 衝突相手が所有者のコライダーであれば無視
            if (owner != null)
            {
                var ownerCollider = collision.collider;
                if (ownerCollider != null && ownerCollider.transform.IsChildOf(owner.transform))
                {
                    return;
                }
            }

            Explode();
        }

        private void TryIgnoreOwnerCollisions()
        {
            if (owner == null || ownerCollisionIgnored) return;

            try
            {
                var ownerColliders = owner.GetComponentsInChildren<Collider>();
                if (ownerColliders == null || ownerColliders.Length == 0 || projectileColliders == null || projectileColliders.Length == 0)
                {
                    ownerCollisionIgnored = true; // 無視対象がないとしてマークしておく
                    return;
                }

                foreach (var pCol in projectileColliders)
                {
                    if (pCol == null) continue;
                    foreach (var oCol in ownerColliders)
                    {
                        if (oCol == null) continue;
                        Physics.IgnoreCollision(pCol, oCol, true);
                    }
                }

                ownerCollisionIgnored = true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[GrenadeProjectile] TryIgnoreOwnerCollisions failed: " + ex.Message);
                ownerCollisionIgnored = true;
            }
        }

        private void Explode()
        {
            if (exploded) return;
            exploded = true;

            Vector3 pos = transform.position;
            var colliders = Physics.OverlapSphere(pos, explosionRadius, affectLayers);
            var applied = new HashSet<Player>();

            foreach (var c in colliders)
            {
                var p = c.GetComponentInParent<Player>();
                if (p == null) continue;
                if (applied.Contains(p)) continue;
                applied.Add(p);

                var ps = p.playerStatus;
                if (ps == null) continue;

                var effect = new FlashBangEffect();
                try
                {
                    ps.EffectList.AddEffect(effect);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[GrenadeProjectile] EffectList.AddEffect failed: " + ex.Message);
                    effect.Effect(p, ps, null);
                }
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