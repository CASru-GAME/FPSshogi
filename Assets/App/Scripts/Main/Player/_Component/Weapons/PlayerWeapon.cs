using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace App.Main.Player
{
    // 当たり判定のみ管理する武器コンポーネント。
    // ダメージ計算・クールダウン等は外部（PrimaryAction側）で行う想定。
    public class PlayerWeapon : MonoBehaviour
    {
        [Header("設定")]
        public Player owner;                            // この武器の所有者（外部で設定）
        public string attackAnimationName = "Attack";   // Animator 内のクリップ名（外部参照用）
        public float fallbackAttackDuration = 0.6f;     // アニメが見つからない時の当たり時間
        public float hitRadius = 0.5f;                  // 当たり判定半径（自動生成する場合）
        public LayerMask hitLayers = ~0;                // 当たり判定で検出するレイヤー

        Animator animator;
        Collider hitCollider;

        // ヒットを通知するイベント（外部で購読してダメージ等を処理する）
        public event Action<Player> OnPlayerHit;

        void Awake()
        {
            animator = GetComponent<Animator>();

            // 既存のコライダを利用するか、無ければ自前で SphereCollider を作る
            hitCollider = GetComponent<Collider>();
            if (hitCollider == null)
            {
                var sc = gameObject.AddComponent<SphereCollider>();
                sc.isTrigger = true;
                sc.radius = hitRadius;
                hitCollider = sc;
            }
            else
            {
                hitCollider.isTrigger = true;
            }

            // 初期は無効化（外部から ActivateHitbox で有効化する）
            hitCollider.enabled = false;
        }

        // アニメ再生。再生可能なら再生してそのクリップ長を返す。
        // 使い方例: duration = weapon.PlayAnimation(weapon.attackAnimationName);
        public float PlayAnimation(string clipName)
        {
            float duration = fallbackAttackDuration;
            if (animator != null && animator.runtimeAnimatorController != null && !string.IsNullOrEmpty(clipName))
            {
                var clips = animator.runtimeAnimatorController.animationClips;
                foreach (var c in clips)
                {
                    if (c != null && c.name == clipName)
                    {
                        duration = c.length;
                        break;
                    }
                }

                try
                {
                    animator.Play(clipName, 0, 0f);
                    Debug.Log($"[PlayerWeapon] Playing animation '{clipName}' with duration {duration} seconds.");
                }
                catch
                {
                    // アニメ再生不可でも続行
                }
            }
            return duration;
        }

        // 当たり判定を指定秒数だけ有効化する（duration 秒後に自動で無効化され、onCompleted が呼ばれる）
        // 既に相手が当たり判定内にいる場合でもすぐ当たりを発生させるため、OverlapSphere による即時判定を行います。
        public void ActivateHitbox(float duration, Action onCompleted = null)
        {
            if (hitCollider == null)
            {
                onCompleted?.Invoke();
                return;
            }

            StopAllCoroutines();
            StartCoroutine(ActivateRoutine(duration, onCompleted));
        }

        IEnumerator ActivateRoutine(float duration, Action onCompleted)
        {
            hitCollider.enabled = true;

            // 即時重なり判定：コライダーに既に入っている相手にもヒット通知を出す
            DoImmediateOverlapHits();

            yield return new WaitForSeconds(Mathf.Max(0.01f, duration));

            hitCollider.enabled = false;
            onCompleted?.Invoke();
        }

        // Collider が有効化された直後に既に入っているターゲットを検出して OnPlayerHit を呼ぶ
        private void DoImmediateOverlapHits()
        {
            float radiusToUse = hitRadius;

            // If collider is a SphereCollider, use its effective radius (considering scale)
            if (hitCollider is SphereCollider sc)
            {
                // account for lossyScale (approx using x)
                float scale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
                radiusToUse = sc.radius * scale;
            }
            else
            {
                // fallback: use configured hitRadius
                radiusToUse = hitRadius;
            }

            int layerMask = hitLayers.value;
            Collider[] cols = Physics.OverlapSphere(transform.position, radiusToUse, layerMask);
            foreach (var c in cols)
            {
                if (c == null) continue;
                var target = c.GetComponentInParent<Player>();
                if (target == null) continue;
                if (target == owner) continue;

                try
                {
                    OnPlayerHit?.Invoke(target);
                }
                catch (Exception)
                {
                    // 安全に無視
                }
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (hitCollider == null || !hitCollider.enabled) return;

            // レイヤーフィルタ
            if ((hitLayers.value & (1 << other.gameObject.layer)) == 0) return;

            var target = other.GetComponentInParent<Player>();
            if (target == null) return;
            if (target == owner) return; // 所有者はヒットしない

            // 当たり判定は通知のみ。外部でダメージや重複制御を行う。
            try
            {
                OnPlayerHit?.Invoke(target);
            }
            catch (Exception)
            {
                // 安全に無視
            }
        }

        // 所有者を設定するユーティリティ
        public void SetOwner(Player p)
        {
            owner = p;
        }
    }
}
