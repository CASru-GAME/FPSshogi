namespace App.Main.Player
{
    interface IEffect
    {
        
        public string effectName { get; }
        public float duration { get; }
        public float lastTime { get; private set; }
        public void Effect(PlayerStatus playerStatus, Action<System.Type> onEffectComplete);
        public void UpdateEffect();
    }
}
