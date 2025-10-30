using System;

namespace App.Main.Player
{
    public interface IEffect
    {

        public string effectName { get; }
        public float duration { get; }
        public float lastTime { get; }
        public bool isActive { get; }
        Action onEffectComplete { get; }
        PlayerStatus playerStatus { get; }
        Player player { get; }
        public void Effect(Player player, PlayerStatus playerStatus, Action onEffectComplete);
        public void EndEffect();
        public void UpdateEffect();
    }
}
