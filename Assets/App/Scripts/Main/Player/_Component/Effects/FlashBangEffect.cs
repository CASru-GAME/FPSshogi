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

        // 追加: この効果が属するカメラ（分割画面対策）
        private Camera targetCamera;

        public void Effect(Player player, PlayerStatus playerStatus, Action onEffectComplete)
        {
            if (playerStatus == null || player == null) { /* still set local refs below */ }

            isActive = true;
            lastTime = duration;
            this.onEffectComplete = onEffectComplete;
            this.playerStatus = playerStatus;
            this.player = player;

            // カメラを特定（プレイヤーの子カメラ優先、なければ Camera.main）
            targetCamera = player.GetComponentInChildren<Camera>();
            if (targetCamera == null) targetCamera = Camera.main;

            // 移動を止める（既存実装に合わせる）
            playerStatus.MoveSpeed.Multiply(0.2f);

            // オーバーレイ生成（プレイヤーカメラに紐づける）
            CreateOverlay();
        }

        private void CreateOverlay()
        {
            // 既にあるなら破棄してから作り直す
            if (overlayRoot != null)
            {
                UnityEngine.Object.Destroy(overlayRoot);
                overlayRoot = null;
                overlayImage = null;
            }

            overlayRoot = new GameObject("FlashbangOverlay");
            // Canvas コンポーネントを追加
            var canvas = overlayRoot.AddComponent<Canvas>();

            if (targetCamera != null)
            {
                // カメラ単位で表示させる（分割画面対応）
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = targetCamera;
                // Canvas がカメラに対して表示される距離（必要に応じて調整）
                canvas.planeDistance = 1f;
            }
            else
            {
                // 最低限のフォールバック（全画面）
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            // sortingOrder を高くして上に表示（他のUIより前に）
            canvas.sortingOrder = 10000;

            // CanvasScaler と GraphicRaycaster を追加
            var scaler = overlayRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            overlayRoot.AddComponent<GraphicRaycaster>();

            // Image を作成して全面に広げる
            var imgGO = new GameObject("FlashImage");
            imgGO.transform.SetParent(overlayRoot.transform, false);
            overlayImage = imgGO.AddComponent<Image>();
            // 最初は不透明（フラッシュ）
            overlayImage.color = new Color(1f, 1f, 1f, 1f);

            var rt = overlayImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // 重要: Canvas をカメラの表示領域（Viewport）に合わせるため、
            // overlayRoot をカメラに紐づけた場合は Canvas の transform をカメラの子にしない。
            // ScreenSpaceCamera で worldCamera を設定すればそのカメラのビューポートに表示されます。
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
            targetCamera = null;
        }
    }
}