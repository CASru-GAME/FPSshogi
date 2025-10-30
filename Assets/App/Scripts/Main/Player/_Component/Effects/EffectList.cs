using System;
using System.Collections.Generic;
using System.Linq;


namespace App.Main.Player
{
    public class EffectList
    {
        private List<IEffect> effects = new List<IEffect>();
        private PlayerStatus playerStatus;
        private Player player;
        private Action OnUpdate;

        public EffectList(Player player, PlayerStatus playerStatus, Action onUpdate = null)
        {
            this.player = player;
            this.playerStatus = playerStatus;
            OnUpdate = onUpdate;
        }

        public void AddEffect(IEffect effect)
        {
            if (effects.Any(e => e.GetType() == effect.GetType()))
            {
                return;
            }
            effect.Effect(player,playerStatus, () => OnEffectComplete(effect.GetType()));
            effects.Add(effect);
        }

        public void UpdateEffects()
        {
            if (effects.Count == 0) return;
            foreach (var effect in effects.ToList())
            {
                effect.UpdateEffect();
            }
        }
        private void OnEffectComplete(System.Type effectType)
        {
            effects.RemoveAll(e => e.GetType() == effectType);
        }

        public void DumpStatus()
        {
            UnityEngine.Debug.Log($"Effect List Status - Total Effects: {effects.Count}");
            foreach (var effect in effects)
            {
                UnityEngine.Debug.Log($"Effect: {effect.effectName}, Duration: {effect.duration}, Last Time: {effect.lastTime}");
            }
        }
    }
}
