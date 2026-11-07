using System;
using UnityEngine;

namespace App.Main.Player
{
    public class FlashBangEffect : IEffect
    {
        public string effectName { get; } = "FlashBang";
        public float duration { get; } = 2f;
        public float lastTime { get; private set; }
        public bool isActive { get; private set; }
        public Action onEffectComplete { get; private set; }
        public PlayerStatus playerStatus { get; private set; }
        public Player player { get; private set; }
        public float Opacity => Mathf.Clamp01(lastTime / duration);

        public void Effect(Player player, PlayerStatus playerStatus, Action onEffectComplete)
        {
            isActive = true;
            lastTime = duration;
            this.onEffectComplete = onEffectComplete;
            this.playerStatus = playerStatus;
            this.player = player;
            playerStatus.MoveSpeed.SetMultiplier(this, 0.2f);
        }

        public void UpdateEffect()
        {
            if (!isActive) return;

            lastTime -= Time.deltaTime;
            if (lastTime <= 0f) EndEffect();
        }

        public void EndEffect()
        {
            if (!isActive) return;

            playerStatus.MoveSpeed.RemoveMultiplier(this);
            isActive = false;
            lastTime = 0f;
            var complete = onEffectComplete;
            onEffectComplete = null;
            playerStatus = null;
            player = null;
            complete?.Invoke();
        }
    }
}
