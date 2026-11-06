using System;
using System.Collections;
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
        Action completed;

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
            var presentation=GetComponent<WeaponPresentation>();
            if(presentation!=null)return presentation.Attack();
            float duration = fallbackAttackDuration;
            if (animator != null && animator.runtimeAnimatorController != null && !string.IsNullOrEmpty(clipName))
            {
                var clips = animator.runtimeAnimatorController.animationClips;
                foreach (var c in clips)
                {
                    if (c != null && c.name.Replace('.', '_') == clipName.Replace('.', '_'))
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
            completed?.Invoke();
            completed=onCompleted;
            StartCoroutine(ActivateRoutine(duration, onCompleted));
        }

        IEnumerator ActivateRoutine(float duration, Action onCompleted)
        {
            var presentation=GetComponent<WeaponPresentation>();
            if(presentation!=null)
            {
                yield return new WaitForSeconds(presentation.hitStart);
                duration=Mathf.Max(.01f,presentation.hitEnd-presentation.hitStart);
            }
            presentation?.RefreshPose();
            hitCollider.enabled = true;

            // 即時重なり判定：コライダーに既に入っている相手にもヒット通知を出す
            DoImmediateOverlapHits();

            yield return new WaitForSeconds(Mathf.Max(0.01f, duration));

            hitCollider.enabled = false;
            completed=null;
            onCompleted?.Invoke();
        }

        void OnDisable()
        {
            StopAllCoroutines();if(hitCollider!=null)hitCollider.enabled=false;
            var callback=completed;completed=null;callback?.Invoke();
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
            Collider[] cols = hitCollider is BoxCollider box
                ? Physics.OverlapBox(transform.TransformPoint(box.center),Vector3.Scale(box.size*.5f,transform.lossyScale),transform.rotation,layerMask,QueryTriggerInteraction.Ignore)
                : Physics.OverlapSphere(transform.position, radiusToUse, layerMask,QueryTriggerInteraction.Ignore);
            foreach (var c in cols)
            {
                if (c == null) continue;
                var target = c.GetComponentInParent<Player>();
                if (target == null) continue;
                if (target == owner) continue;
                if (!HasClearPathTo(c, target)) continue;

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
            if (other == null || other.isTrigger) return;

            // レイヤーフィルタ
            if ((hitLayers.value & (1 << other.gameObject.layer)) == 0) return;

            var target = other.GetComponentInParent<Player>();
            if (target == null) return;
            if (target == owner) return; // 所有者はヒットしない
            if (!HasClearPathTo(other, target)) return;

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

        private bool HasClearPathTo(Collider targetCollider, Player target)
        {
            // An animated blade can overlap a body on the far side of cover.
            // Check from its wielder so starting the query beyond the wall cannot bypass it.
            Vector3 origin = owner != null ? owner.transform.position : transform.position;
            Vector3 direction = targetCollider.ClosestPoint(origin) - origin;
            float distance = direction.magnitude;
            if (distance <= Mathf.Epsilon) return true;

            return !ShotPhysics.Raycast(new Ray(origin, direction / distance), distance, hitLayers,
                owner, out RaycastHit hit, transform) || hit.collider.GetComponentInParent<Player>() == target;
        }

        // 所有者を設定するユーティリティ
        public void SetOwner(Player p)
        {
            owner = p;
        }
    }
}
