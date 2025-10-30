using System;

namespace App.Main.Player
{
    public class SlowEffect : IEffect
    {
        public string effectName { get; } = "Slow";
        public float duration { get; } = 5f;
        public float lastTime { get; private set; }
        public bool isActive { get; private set; } = false;
        public Action onEffectComplete { get; private set; }
        public PlayerStatus playerStatus { get; private set; }
        public Player player { get; private set; }
        public void Effect(Player player, PlayerStatus playerStatus, Action onEffectComplete)
        {
            isActive = true;
            lastTime = duration;
            this.onEffectComplete = onEffectComplete;
            this.playerStatus = playerStatus;
            playerStatus.MoveSpeed.Multiply(0.5f);
        }
        public void EndEffect()
        {
            if (!isActive) return;

            playerStatus.MoveSpeed.Reset();
            isActive = false;
            onEffectComplete?.Invoke();
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
    }
}
