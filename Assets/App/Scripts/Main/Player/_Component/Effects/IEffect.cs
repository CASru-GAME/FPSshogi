using System;

namespace App.Main.Player
{
    public interface IEffect
    {

        public string effectName { get; }
        public float duration { get; }
        public float lastTime { get; }
        public void Effect(PlayerStatus playerStatus, Action onEffectComplete);
        public void UpdateEffect();
    }
}
