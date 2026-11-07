using App.Audio;
using System;
using UnityEngine;

namespace App.Main.Player
{
    public class ProjectileArrow : MonoBehaviour
    {
        Player owner;
        PlayerStatus ownerStatus;
        float damageMultiplier = 1f;
        float falloffStart = 30f;
        float maxRange = 80f;
        float minDamageMultiplier = 0.5f;
        LayerMask hitLayers = ~0;

        Vector3 launchPosition;
        float lifeTimer = 8f;
        Rigidbody rb;
        bool launched = false;
        bool consumed = false;

        public void Initialize(Player owner, PlayerStatus ownerStatus, float damageMultiplier, float falloffStart, float maxRange, float minDamageMultiplier, LayerMask hitLayers)
        {
            this.owner = owner;
            this.ownerStatus = ownerStatus;
            this.damageMultiplier = damageMultiplier;
            this.falloffStart = Mathf.Max(0f, falloffStart);
            this.maxRange = Mathf.Max(0.001f, maxRange);
            this.minDamageMultiplier = Mathf.Clamp01(minDamageMultiplier);
            this.hitLayers = hitLayers;
            rb = GetComponent<Rigidbody>();
        }

        public void Launch(Vector3 dir, float speed, float life)
        {
            launchPosition = transform.position;
            lifeTimer = Mathf.Max(0.01f, life);
            launched = true;
            rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = dir * speed;
            }
            else
            {
                // 自前移動
                StartCoroutine(MoveByTransform(dir * speed));
            }
            StartCoroutine(AutoDestroyAfter(lifeTimer));
        }

        public void SetAutoDestroy(float seconds)
        {
            launchPosition = transform.position;
            lifeTimer = Mathf.Max(0.01f, seconds);
            launched = true;
            StartCoroutine(AutoDestroyAfter(lifeTimer));
        }

        System.Collections.IEnumerator AutoDestroyAfter(float s)
        {
            yield return new WaitForSeconds(s);
            TryDestroy();
        }

        void FixedUpdate()
        {
            if (launched && !consumed && rb != null && rb.linearVelocity.sqrMagnitude > .01f)
                rb.MoveRotation(Quaternion.LookRotation(rb.linearVelocity));
        }

        System.Collections.IEnumerator MoveByTransform(Vector3 velocity)
        {
            while (!consumed)
            {
                Vector3 movement = velocity * Time.deltaTime;
                float distance = movement.magnitude;
                if (distance > 0f && ShotPhysics.Raycast(new Ray(transform.position, movement / distance),
                    distance, hitLayers, owner, out RaycastHit hit, transform))
                {
                    transform.position = hit.point;
                    HandleHit(hit.collider);
                    yield break;
                }
                transform.position += movement;
                yield return null;
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            HandleHit(collision.collider);
        }

        void OnTriggerEnter(Collider other)
        {
            HandleHit(other);
        }

        private void HandleHit(Collider col)
        {
            if (consumed || col == null || col.isTrigger) return;

            // レイヤーフィルタ
            if ((hitLayers.value & (1 << col.gameObject.layer)) == 0) return;

            var target = col.GetComponentInParent<Player>();
            if (target == null)
            {
                GameAudio.Play(SoundCue.ArrowImpact,owner,transform.position);
                // 地面やオブジェクトに当たったら消す
                TryDestroy();
                return;
            }

            if (owner != null && target == owner) return; // 所有者は無視

            // Destroy is deferred until the end of the frame. Consume first so
            // another body collider cannot apply this projectile's damage twice.
            consumed = true;

            // ダメージ計算
            int baseAtk = 0;
            try
            {
                if (ownerStatus != null && ownerStatus.AttackPoint != null)
                    baseAtk = ownerStatus.AttackPoint.Current;
            }
            catch { baseAtk = 0; }

            float traveled = Vector3.Distance(launchPosition, transform.position);
            float fallMul = 1f;
            if (traveled <= falloffStart) fallMul = 1f;
            else if (traveled >= maxRange) fallMul = minDamageMultiplier;
            else
            {
                float t = (traveled - falloffStart) / Mathf.Max(0.0001f, (maxRange - falloffStart));
                fallMul = Mathf.Lerp(1f, minDamageMultiplier, Mathf.Clamp01(t));
            }

            int damage = Mathf.CeilToInt(baseAtk * damageMultiplier * fallMul);

            try
            {
                var ts = target.playerStatus;
                if (ts != null)
                {
                    ts.TakeDamage(damage, launchPosition, ownerStatus);
                }
            }
            catch { }

            // 衝突で消える
            TryDestroy();
        }

        private void TryDestroy()
        {
            consumed = true;
            try { Destroy(gameObject); } catch { }
        }
    }
}
