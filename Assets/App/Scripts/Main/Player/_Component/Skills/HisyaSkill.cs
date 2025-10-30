using System;
using System.Collections.Generic;
using UnityEngine;

namespace App.Main.Player
{
    public class HisyaSkill : ISkill
    {
        public string skillName { get; private set; } = "HisyaSkill";
        public float cooldownTime { get; private set; } = 10f;
        public float cooldownTimer { get; private set; } = 0f;
        public bool isOnCooldown { get; private set; } = false;
        public PlayerStatus playerStatus { get; private set; }
        public Player player { get; private set; }

        // フィールド設定（Inspector で調整可）
        public float spawnDistance = 999f;
        public float radius = 7f;
        public float duration = 6f;
        public float slowMultiplier = 0.6f; // 移動速度を掛ける倍率 (0..1)
        public LayerMask affectLayers = ~0;

        public void UseSkill(Player player, PlayerStatus playerStatus)
        {
            if (isOnCooldown) return;
            if (player == null) return;

            this.player = player;
            this.playerStatus = playerStatus;

            // カメラ基準のスポーン位置：カメラからレイを飛ばしてヒット位置に出す
            Camera cam = player.GetComponentInChildren<Camera>();
            if (cam == null) cam = Camera.main;

            Vector3 spawnPos;
            if (cam != null)
            {
                Ray ray = new Ray(cam.transform.position, cam.transform.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, spawnDistance))
                {
                    spawnPos = hit.point;
                }
                else
                {
                    spawnPos = cam.transform.position + cam.transform.forward * spawnDistance;
                }
            }
            else
            {
                spawnPos = player.transform.position + player.transform.forward * spawnDistance;
            }

            // ルートオブジェクト作成
            var root = new GameObject("Hisya_SlowField");
            root.transform.position = spawnPos;
            root.transform.rotation = Quaternion.identity;

            // ビジュアル：薄い紫の半透明円盤（Cylinderを扁平化）
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            UnityEngine.Object.Destroy(visual.GetComponent<Collider>()); // ビジュアル用コライダは不要
            visual.name = "Visual";
            visual.transform.SetParent(root.transform, false);
            // 円盤が地面に埋まらないように少し上にする
            visual.transform.localPosition = Vector3.up * 0.01f;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = new Vector3(radius * 2f, 0.02f, radius * 2f); // 高さは小さくする

            var mr = visual.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                var mat = CreateTransparentMaterial(new Color(0.6f, 0.2f, 0.9f, 0.5f)); // alpha = 0.5
                mr.material = mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }

            // トリガー用コライダー（球形で範囲検出）
            var trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = radius;

            // ライフタイマーとロジックを持つコンポーネントを追加
            var area = root.AddComponent<HisyaFieldArea>();
            area.owner = player;
            area.radius = radius;
            area.duration = duration;
            area.slowMultiplier = slowMultiplier;
            area.affectLayers = affectLayers;

            // 破棄の保険（Area 側でも Destroy するが念のため）
            UnityEngine.Object.Destroy(root, duration + 0.1f);

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

        private Material CreateTransparentMaterial(Color color)
        {
            // 利用可能な透明対応シェーダーを順に探す（URP/HDRP/旧レンダーパイプラインの差に対応）
            Shader shader = Shader.Find("Unlit/Transparent");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            Material mat;

            if (shader != null)
            {
                mat = new Material(shader);
                // Sprites/Default 等はアルファをそのまま扱うので特別な設定は不要
            }
            else
            {
                // 最悪 Standard にフォールバックして透明モードを強制する
                Shader std = Shader.Find("Standard");
                mat = new Material(std ?? Shader.Find("Hidden/InternalErrorShader"));
                if (std != null)
                {
                    mat.SetFloat("_Mode", 3f);
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.renderQueue = 3000;
                }
            }

            // 色と透過度を設定（alpha = 0.5 にしたい場合は呼び出し側でその値を渡してください）
            mat.color = color;

            return mat;
        }
    }

    // フィールドの実体（他クラス名と衝突しない固有名）
    public class HisyaFieldArea : MonoBehaviour
    {
        public Player owner;
        public float radius = 3f;
        public float duration = 6f;
        public float slowMultiplier = 0.6f;
        public LayerMask affectLayers = ~0;

        private Dictionary<Player, HisyaFieldSlowEffect> applied = new Dictionary<Player, HisyaFieldSlowEffect>();
        private float lifeTimer;

        void Start()
        {
            lifeTimer = duration;
        }

        void Update()
        {
            lifeTimer -= Time.deltaTime;
            if (lifeTimer <= 0f)
            {
                DestroySelf();
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if ((affectLayers.value & (1 << other.gameObject.layer)) == 0) return;

            var p = other.GetComponentInParent<Player>();
            if (p == null) return;
            if (applied.ContainsKey(p)) return;

            var ps = p.playerStatus;
            if (ps == null) return;

            var effect = new HisyaFieldSlowEffect(slowMultiplier);
            try
            {
                ps.EffectList.AddEffect(effect);
            }
            catch (Exception)
            {
                // EffectList に問題がある場合は直接起動
                effect.Effect(p, ps, null);
            }

            applied[p] = effect;
        }

        void OnTriggerExit(Collider other)
        {
            var p = other.GetComponentInParent<Player>();
            if (p == null) return;
            if (!applied.TryGetValue(p, out var eff)) return;

            try { eff.EndEffect(); } catch (Exception) { }
            applied.Remove(p);
        }

        private void DestroySelf()
        {
            // 残存効果を解除
            foreach (var kv in new List<KeyValuePair<Player, HisyaFieldSlowEffect>>(applied))
            {
                try { kv.Value.EndEffect(); } catch (Exception) { }
            }
            applied.Clear();

            Destroy(gameObject);
        }
    }

    // フィールド由来の遅延スロー効果（固有名で衝突回避）
    public class HisyaFieldSlowEffect : IEffect
    {
        public string effectName { get; } = "HisyaFieldSlow";
        public float duration { get; } = 999999f; // 基本的にはフィールドが解除する
        public float lastTime { get; private set; }
        public bool isActive { get; private set; } = false;
        public Action onEffectComplete { get; private set; }
        public PlayerStatus playerStatus { get; private set; }
        public Player player { get; private set; }

        private float multiplier = 1f;

        public HisyaFieldSlowEffect(float multiplier)
        {
            this.multiplier = Mathf.Clamp01(multiplier);
        }

        public void Effect(Player player, PlayerStatus playerStatus, Action onEffectComplete)
        {
            this.player = player;
            this.playerStatus = playerStatus;
            this.onEffectComplete = onEffectComplete;

            isActive = true;
            lastTime = duration;

            try
            {
                playerStatus.MoveSpeed.Multiply(multiplier);
            }
            catch (Exception)
            {
            }
        }

        public void UpdateEffect()
        {
            if (!isActive) return;
            lastTime -= UnityEngine.Time.deltaTime;
            if (lastTime <= 0f)
            {
                EndEffect();
                isActive = false;
                lastTime = 0f;
            }
        }

        public void EndEffect()
        {
            if (!isActive && playerStatus == null) return;

            try
            {
                playerStatus.MoveSpeed.Reset();
            }
            catch (Exception)
            {
            }

            isActive = false;
            onEffectComplete?.Invoke();
            onEffectComplete = null;
            playerStatus = null;
            player = null;
        }
    }
}