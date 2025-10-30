using System;
using UnityEngine;
using UnityEngine.UI;

namespace App.Main.Player
{
    public class FlashBangEffect : IEffect
    {
        public string effectName { get; } = "FlashBang";
        public float duration { get; } = 2f;
        public float lastTime { get; private set; }
        public bool isActive { get; private set; } = false;
        public Action onEffectComplete { get; private set; }
        public PlayerStatus playerStatus { get; private set; }
        public Player player { get; private set; }

        // オーバーレイ参照
        private GameObject overlayRoot;
        private Image overlayImage;

        public void Effect(Player player, PlayerStatus playerStatus, Action onEffectComplete)
        {
            if (playerStatus == null || player == null) { /* still set local refs below */ }

            isActive = true;
            lastTime = duration;
            this.onEffectComplete = onEffectComplete;
            this.playerStatus = playerStatus;
            this.player = player;

            // 移動を止める（既存実装に合わせる）
            playerStatus.MoveSpeed.Multiply(0.2f);

            // オーバーレイ生成（Canvas + Image）
            CreateOverlay();
        }

        private void CreateOverlay()
        {
            // 安全性: 既にあるなら破棄してから作り直す
            if (overlayRoot != null)
            {
                UnityEngine.Object.Destroy(overlayRoot);
                overlayRoot = null;
                overlayImage = null;
            }

            // Canvas を ScreenSpaceOverlay で作る（最前面に表示）
            overlayRoot = new GameObject("FlashbangOverlay");
            var canvas = overlayRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10000;

            overlayRoot.AddComponent<CanvasScaler>();
            overlayRoot.AddComponent<GraphicRaycaster>();

            // Image を作成して全面に広げる
            var imgGO = new GameObject("FlashImage");
            imgGO.transform.SetParent(overlayRoot.transform, false);
            overlayImage = imgGO.AddComponent<Image>();
            overlayImage.color = Color.white; // 最初は不透明（フラッシュ）
            var rt = overlayImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public void UpdateEffect()
        {
            if (!isActive) return;

            // 時間経過
            lastTime -= UnityEngine.Time.deltaTime;

            // アルファを残り時間比で設定（開始時 1 -> 終了時 0）
            if (overlayImage != null && duration > 0f)
            {
                float alpha = Mathf.Clamp01(lastTime / duration);
                var c = overlayImage.color;
                overlayImage.color = new Color(c.r, c.g, c.b, alpha);
            }

            if (lastTime <= 0f)
            {
                EndEffect();
                isActive = false;
                lastTime = 0f;
            }
        }

        public void EndEffect()
        {
            if (!isActive && overlayRoot == null) return;

            // リセット処理
            try
            {
                playerStatus?.MoveSpeed.Reset();
            }
            catch (Exception)
            {
                // ignore
            }

            // オーバーレイ破棄
            if (overlayRoot != null)
            {
                UnityEngine.Object.Destroy(overlayRoot);
                overlayRoot = null;
                overlayImage = null;
            }

            isActive = false;
            onEffectComplete?.Invoke();
            onEffectComplete = null;
            playerStatus = null;
            player = null;
        }
    }
}