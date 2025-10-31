using System;
using System.Collections;
using UnityEngine;

namespace App.Main.Player
{
    public class KingSkill : ISkill
    {
        public string skillName => "KingSkill";
        public float cooldownTime => 12f;
        public float cooldownTimer { get; private set; }
        public bool isOnCooldown => cooldownTimer > 0f;
        public PlayerStatus playerStatus { get; private set; }
        public Player player { get; private set; }

        // 調整パラメータ（Inspector から変更可）
        public GameObject turretPrefab;        // 出現させる ObjectForKing（Prefab または ObjectForKing の実体）
        public GameObject gunbaiPrefab;        // 振る軍配のプレハブ
        public string turretShootAnim = "火縄銃長_001|火縄銃長用";   // 銃の子オブジェクトにある射撃アニメ名
        public string gunbaiAnim = "アーマチュア|軍配用";        // 軍配のアニメ名
        public float skillDuration = 6f;       // 出現からの持続時間（秒）
        public float turretFireInterval = 0.6f;
        public float turretRange = 40f;
        public float turretFalloffStart = 20f;
        public float turretMinDamageMul = 0.5f;
        public float turretDamageMultiplier = 1.0f;
        public LayerMask turretHitLayers = ~0;

        // 内部管理
        private readonly GameObject[] spawnedTurrets = new GameObject[3];
        private readonly Coroutine[] turretDestroyCoroutines = new Coroutine[3];

        public void UseSkill(Player player, PlayerStatus playerStatus)
        {
            if (isOnCooldown) return;
            if (player == null || playerStatus == null) return;

            this.player = player;
            this.playerStatus = playerStatus;

            // クールダウン開始
            cooldownTimer = cooldownTime;

            // 1) 軍配（gunbai）を出してアニメ再生・削除
            if (gunbaiPrefab != null)
            {
                var g = UnityEngine.Object.Instantiate(gunbaiPrefab, player.transform.position, Quaternion.identity);
                // 位置はプレイヤー正面や手元に合わせる。ここではプレイヤー前方に少し出す
                var cam = player.GetComponentInChildren<Camera>() ?? Camera.main;
                if (cam != null)
                {
                    g.transform.position = cam.transform.position + cam.transform.forward * 0.8f;
                    g.transform.rotation = Quaternion.LookRotation(cam.transform.forward);
                }
                else
                {
                    g.transform.position = player.transform.position + player.transform.forward * 1f;
                    g.transform.rotation = Quaternion.LookRotation(player.transform.forward);
                }

                // 再生と自動削除
                var anim = g.GetComponentInChildren<Animator>();
                float duration = 0.5f;
                if (anim != null && !string.IsNullOrEmpty(gunbaiAnim))
                {
                    // クリップ長取得（安全に）
                    try
                    {
                        var clips = anim.runtimeAnimatorController?.animationClips;
                        if (clips != null)
                        {
                            foreach (var c in clips)
                            {
                                if (c != null && c.name == gunbaiAnim)
                                {
                                    duration = c.length;
                                    break;
                                }
                            }
                        }
                        anim.Play(gunbaiAnim, 0, 0f);
                    }
                    catch { }
                }
                UnityEngine.Object.Destroy(g, duration + 0.05f);
            }

            // 2) プレイヤー前方に3つのタレット（ObjectForKing のインスタンス）をスポーン
            // offsets: 中央, 左, 右 (ローカル)
            Vector3[] localOffsets = new Vector3[]
            {
                new Vector3(0f, 0f, 3.0f),
                new Vector3(-1.2f, 0f, 2.6f),
                new Vector3(1.2f, 0f, 2.6f)
            };

            var camRef = player.GetComponentInChildren<Camera>() ?? Camera.main;
            Vector3 forward = (camRef != null) ? camRef.transform.forward : player.transform.forward;
            Vector3 right = (camRef != null) ? camRef.transform.right : player.transform.right;
            forward.y = 0; right.y = 0;
            forward.Normalize(); right.Normalize();

            for (int i = 0; i < 3; i++)
            {
                // base spawn position relative to player
                Vector3 spawnPos = player.transform.position + forward * localOffsets[i].z + right * localOffsets[i].x + Vector3.up * 0.5f;

                GameObject proto = turretPrefab != null ? turretPrefab : player.ObjectForKingSkill;
                if (proto == null) continue;

                var go = UnityEngine.Object.Instantiate(proto, spawnPos, Quaternion.LookRotation(forward));
                go.name = $"KingTurret_{player.name}_{i}";
                // 所有者を持つ KingTurret コンポーネントをアタッチして初期化
                var kt = go.GetComponent<KingTurret>();
                if (kt == null) kt = go.AddComponent<KingTurret>();
                kt.Initialize(player, playerStatus,
                    turretFireInterval, turretRange, turretFalloffStart, turretMinDamageMul, turretDamageMultiplier, turretHitLayers, turretShootAnim);

                spawnedTurrets[i] = go;
                // 自動破棄をスケジュール
                turretDestroyCoroutines[i] = player.StartCoroutine(DestroyTurretAfter(go, skillDuration));
            }
        }

        private IEnumerator DestroyTurretAfter(GameObject go, float seconds)
        {
            yield return new WaitForSeconds(Mathf.Max(0.01f, seconds));
            if (go != null) UnityEngine.Object.Destroy(go);
            yield break;
        }

        public void UpdateSkill()
        {
            if (isOnCooldown)
            {
                cooldownTimer -= UnityEngine.Time.deltaTime;
                if (cooldownTimer < 0f) cooldownTimer = 0f;
            }
        }
    }
}
