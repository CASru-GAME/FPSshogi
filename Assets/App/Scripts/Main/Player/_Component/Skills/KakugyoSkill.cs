using System;
using UnityEngine;

namespace App.Main.Player
{
    public class KakugyoSkill : ISkill
    {
        public string skillName { get; } = "KakugyoSkill";
        public float cooldownTime { get; } = 10f;
        public bool isOnCooldown { get; private set; } = false;
        public PlayerStatus playerStatus { get; private set; }
        public Player player { get; private set; }
        public float cooldownTimer { get; private set; } = 0f;

        // 距離／時間で制御するパラメータ
        public float ascendHeight = 20f;      // 上昇する高さ（メートル）
        public float ascendDuration = 0.3f;   // 上昇にかける時間（秒）
        public float hoverDuration = 5.0f;    // 滞空時間（秒）
        public float hoverYVelocity = 0f;     // 滞空中に維持する垂直速度（通常 0）

        // 実行状態
        private enum Phase { Idle, Ascending, Hovering }
        private Phase phase = Phase.Idle;
        private bool skillActive = false;
        private float phaseTimer = 0f;

        public void UseSkill(Player player, PlayerStatus playerStatus)
        {
            if (isOnCooldown) return;
            if (player == null || playerStatus == null) return;
            if (ascendDuration <= 0f) ascendDuration = 0.01f;

            this.player = player;
            this.playerStatus = playerStatus;

            var rb = player.GetComponent<Rigidbody>();
            if (rb == null || rb.isKinematic)
            {
                Debug.LogWarning("[KakugyoSkill] Rigidbody missing or kinematic. Skill aborted.");
                return;
            }

            // 距離/時間で上昇速度を決定（水平速度はプレイヤーの入力で制御可能にするため Y のみ上書き）
            float ascendVel = ascendHeight / ascendDuration; // m/s

            // Yのみ上書きして上昇フェーズ開始
            player.SetMovementYOverride(ascendVel, ascendDuration);

            phase = Phase.Ascending;
            skillActive = true;
            phaseTimer = 0f;

            // クールダウン開始
            isOnCooldown = true;
            cooldownTimer = cooldownTime;
        }

        public void UpdateSkill()
        {
            // クールダウン更新
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

            phaseTimer += Time.deltaTime;

            if (phase == Phase.Ascending)
            {
                if (phaseTimer >= ascendDuration)
                {
                    // 上昇完了 -> 滞空フェーズへ移行
                    phase = Phase.Hovering;
                    phaseTimer = 0f;

                    // 滞空時も水平入力で移動可能にするため Y のみ上書き
                    player.SetMovementYOverride(hoverYVelocity, hoverDuration);
                }
            }
            else if (phase == Phase.Hovering)
            {
                if (phaseTimer >= hoverDuration)
                {
                    // 滞空終了
                    phase = Phase.Idle;
                    skillActive = false;
                    phaseTimer = 0f;
                    player.ClearMovementOverride();
                }
            }
        }
    }
}