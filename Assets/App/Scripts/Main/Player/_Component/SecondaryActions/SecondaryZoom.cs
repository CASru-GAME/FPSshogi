using System;
using UnityEngine;

namespace App.Main.Player
{
    public class SecondaryZoom : ISecondaryAction
    {
        public string secondaryActionName => "SecondaryZoom";
        public Player player { get; private set; }
        public PlayerStatus playerStatus { get; private set; }

        // 調整可能パラメータ
        public float cooldown = 0.1f;
        public float zoomMultiplier = 2f;
        public float zoomDuration = 0f; // 0以下なら自動解除なし（トグルのみ）

        // 実行状態
        private float cooldownTimer = 0f;
        private bool zoomActive = false;
        private float zoomTimer = 0f;

        // カメラの元のFOVを保持して正確に復元する
        private Camera cachedCamera = null;
        private float originalFOV = -1f;

        public void SecondaryAction(Player player, PlayerStatus playerStatus)
        {
            if (player == null || playerStatus == null) return;

            this.player = player;
            this.playerStatus = playerStatus;

            // トグル動作:
            // - 既にズームが有効なら即解除（解除はクールダウンの影響を受けない）
            // - 無効でクールダウン中なら何もしない
            // - 無効かつクールダウン無しなら有効化してクールを回す
            if (zoomActive)
            {
                DeactivateZoom();
                // オフにした際にクールダウンを開始したい場合は以下を有効にする（コメント外す）
                // cooldownTimer = cooldown;
                return;
            }

            if (cooldownTimer > 0f) return;

            ActivateZoom();

            // 有効化時にクールダウンを開始
            cooldownTimer = cooldown;
        }

        public void UpdateSecondaryAction()
        {
            // クールダウン更新
            if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.deltaTime;
                if (cooldownTimer < 0f) cooldownTimer = 0f;
            }

            // 自動解除タイマー（zoomDuration > 0 の場合のみ有効）
            if (zoomActive && zoomDuration > 0f)
            {
                zoomTimer -= Time.deltaTime;
                if (zoomTimer <= 0f)
                {
                    DeactivateZoom();
                    // 自動解除後にクールダウンを既に開始していないなら開始する
                    // （Activate 時に既にクールダウンをセットしているため通常不要）
                    // cooldownTimer = cooldown;
                }
            }
        }

        private void ActivateZoom()
        {
            if (zoomActive) return;

            cachedCamera = player.GetComponentInChildren<Camera>();
            if (cachedCamera == null) return;

            // 元のFOVを保存してから変更（重ね掛けを避ける）
            originalFOV = cachedCamera.fieldOfView;
            cachedCamera.fieldOfView = originalFOV / Mathf.Max(0.0001f, zoomMultiplier);

            zoomActive = true;
            zoomTimer = (zoomDuration > 0f) ? zoomDuration : Mathf.Infinity;
        }

        private void DeactivateZoom()
        {
            if (!zoomActive) return;

            if (cachedCamera != null)
            {
                try
                {
                    // 元のFOV が有効なら戻す
                    if (originalFOV > 0f)
                        cachedCamera.fieldOfView = originalFOV;
                }
                catch { }
            }

            // 状態リセット
            zoomActive = false;
            zoomTimer = 0f;
            originalFOV = -1f;
            cachedCamera = null;
        }
    }
}
