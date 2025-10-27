namespace App.Main.Player
{
    class EffectList
    {
        private List<T> effects = new List<T>();
        private PlayerStatus playerStatus;

        public EffectList(PlayerStatus playerStatus)
        {
            this.playerStatus = playerStatus;
        }

        public void AddEffect(T effect) where T : IEffect
        {
            if (effects.Any(e => e.GetType() == effect.GetType()))
            {
                return;
            }
            effect.Effect(playerStatus, OnEffectComplete);
            effects.Add(effect);
        }

        public void UpdateEffects(T effect) where T : IEffect
        {
            effects.OfType<T>().FirstOrDefault()?.UpdateEffect();
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