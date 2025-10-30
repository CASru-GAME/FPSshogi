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

        // 戻り値: true = 新規追加, false = 既存の効果を再適用した（追加しなかった）
        public bool AddEffect(IEffect effect)
        {
            var existing = effects.FirstOrDefault(e => e.GetType() == effect.GetType());
            if (existing != null)
            {
                // 既に同種の効果がある -> それを再適用（時間リセットなどは Effect 側で処理）
                existing.Effect(player, playerStatus, () => OnEffectComplete(existing.GetType()));
                return false;
            }

            // 新規追加
            effect.Effect(player, playerStatus, () => OnEffectComplete(effect.GetType()));
            effects.Add(effect);
            return true;
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
