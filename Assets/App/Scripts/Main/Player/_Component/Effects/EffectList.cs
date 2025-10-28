using System.Collections.Generic;
using System.Linq;


namespace App.Main.Player
{
    public class EffectList
    {
        private List<IEffect> effects = new List<IEffect>();
        private PlayerStatus playerStatus;

        public EffectList(PlayerStatus playerStatus)
        {
            this.playerStatus = playerStatus;
        }

        public void AddEffect(IEffect effect)
        {
            if (effects.Any(e => e.GetType() == effect.GetType()))
            {
                return;
            }
            effect.Effect(playerStatus, () => OnEffectComplete(effect.GetType()));
            effects.Add(effect);
        }

        public void UpdateEffects(System.Type effectType)
        {
            effects.Where(e => e.GetType() == effectType).ToList().ForEach(e => e.UpdateEffect());
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
