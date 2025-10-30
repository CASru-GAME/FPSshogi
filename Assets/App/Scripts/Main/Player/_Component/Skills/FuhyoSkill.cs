using UnityEngine;

namespace App.Main.Player
{
    public class FuhyoSkill : ISkill
    {
        public string skillName { get; } = "FuhyoSkill";
        public float cooldownTime { get; } = 5f;
        public bool isOnCooldown { get; private set; } = false;
        public PlayerStatus playerStatus { get; private set; }
        public Player player { get; private set; }
        public float cooldownTimer { get; private set; } = 0f;

        // 調整可能なパラメータ
        public float forwardForce = 20f;   // 前方への勢い（水平速度として設定）
        public float upForce = 2f;         // 上方向の勢い（速度に加える）
        public float groundCheckDistance = 5f; // 地面判定距離（小さめ）
        public float jumpDuration = 0.8f;      // ジャンプ継続時間（短め）
        public float fallForce = -2f;          // 半分経過後に与える下方向速度（負の値）

        // 実行中ステート
        private bool skillActive = false;
        private float skillTimer = 0f;
        private bool downwardApplied = false;
        private Vector3 cachedForwardDir = Vector3.forward;

        public void UseSkill(Player player, PlayerStatus playerStatus)
        {
            if (isOnCooldown) return;

            this.player = player;
            this.playerStatus = playerStatus;

            if (player == null)
            {
                Debug.LogWarning("[FuhyoSkill] player is null.");
                return;
            }

            // カメラ優先の前方ベクトル（水平化）
            Vector3 forwardDir = Vector3.zero;
            var cam = player.GetComponentInChildren<Camera>();
            if (cam == null) cam = Camera.main;
            if (cam != null)
            {
                forwardDir = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
            }
            if (forwardDir.sqrMagnitude < 0.001f)
            {
                forwardDir = Vector3.ProjectOnPlane(player.transform.forward, Vector3.up).normalized;
            }
            if (forwardDir.sqrMagnitude < 0.001f)
            {
                forwardDir = Vector3.forward;
            }

            cachedForwardDir = forwardDir;

            // 期待する即時速度ベクトル（X/Z 水平成分は camera 基準、Y は上方向）
            Vector3 desiredVelocity = new Vector3(forwardDir.x * forwardForce, upForce, forwardDir.z * forwardForce);

            // プレイヤーに移動上書きを依頼（ここで duration を設定）
            player.SetMovementOverride(desiredVelocity, jumpDuration, preserveY: false);

            // 任意で小さなインパルスも与えておく（冗長）
            var rb = player.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                Vector3 impulse = forwardDir * (forwardForce * 0.2f) + Vector3.up * (upForce * 0.5f);
                rb.AddForce(impulse, ForceMode.VelocityChange);
            }

            // スキル実行ステート初期化
            skillActive = true;
            skillTimer = 0f;
            downwardApplied = false;

            Debug.Log("[FuhyoSkill] Fuhyo Skill Used! desiredVelocity=" + desiredVelocity);

            // クールダウン開始（別途 cooldown を維持）
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

            if (!skillActive) return;

            // スキル経過時間更新
            skillTimer += Time.deltaTime;

            // 半分経過したら下方向の力（速度上書き）を与える
            if (!downwardApplied && skillTimer >= (jumpDuration * 0.5f))
            {
                downwardApplied = true;

                // 残り時間分だけ下方向を維持する（水平速度は現在の速度を維持または補強）
                float remaining = Mathf.Max(0f, jumpDuration - skillTimer);

                // 現在の水平速度を取得（Rigidbody があれば優先）
                var rb = player.GetComponent<Rigidbody>();
                Vector3 currentHorizontal = Vector3.zero;
                if (rb != null)
                {
                    currentHorizontal = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                }
                else
                {
                    currentHorizontal = cachedForwardDir * forwardForce * 0.3f;
                }

                // もし水平速度が小さければ、スキル発動時の forwardForce をベースに補強する
                float currentSpeed = currentHorizontal.magnitude;
                float minHorizontalSpeed = forwardForce * 0.5f; // 必要に応じて調整
                Vector3 desiredHorizontal;
                if (currentSpeed < minHorizontalSpeed)
                {
                    // 保持向きは cachedForwardDir（カメラ基準）
                    desiredHorizontal = cachedForwardDir * minHorizontalSpeed;
                }
                else
                {
                    desiredHorizontal = currentHorizontal.normalized * currentSpeed;
                }

                // 下向きの速度を設定（水平は維持／補強、Y は fallForce）
                Vector3 downVelocity = new Vector3(desiredHorizontal.x, fallForce, desiredHorizontal.z);

                // duration が 0 だと即時解除されるので小さな値を保証
                float applyDuration = Mathf.Max(0.02f, remaining);

                player.SetMovementOverride(downVelocity, applyDuration, preserveY: false);

                // 追加で Rigidbody に弱い下向きインパルスを与える（存在すれば）
                if (rb != null && !rb.isKinematic)
                {
                    // 下向きの補強インパルス。水平は維持したいので小さめに
                    Vector3 impulse = Vector3.up * (fallForce * 0.12f);
                    rb.AddForce(impulse, ForceMode.VelocityChange);

                    // もし水平速度が不足しているなら、水平補強を確実に与える
                    if (currentSpeed < minHorizontalSpeed)
                    {
                        Vector3 horizImpulse = cachedForwardDir * (minHorizontalSpeed - currentSpeed);
                        rb.AddForce(horizImpulse, ForceMode.VelocityChange);
                    }
                }

                Debug.Log($"[FuhyoSkill] downward applied for {applyDuration}s, downVelocity={downVelocity}, currentHorz={currentHorizontal}");
            }

            // スキル全体時間が終わったら終了処理
            if (skillTimer >= jumpDuration)
            {
                skillActive = false;
                skillTimer = 0f;
                downwardApplied = false;
                // 解除は Player 側で SetMovementOverride の残り時間で自動解除されるが念のためクリア
                player.ClearMovementOverride();
            }
        }
    }
}