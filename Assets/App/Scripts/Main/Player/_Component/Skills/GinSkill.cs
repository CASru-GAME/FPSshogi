using App.Audio;
namespace App.Main.Player
{
    public class GinSkill : ISkill
    {
        public string skillName => "GinSkill";
        public float cooldownTime => 12f;
        public float cooldownTimer { get; private set; }
        public bool isOnCooldown => cooldownTimer > 0f;
        public PlayerStatus playerStatus { get; private set; }
        public Player player { get; private set; }
        int amount = 99999; // 防御力上昇量
        float duration = 1.5f; // 効果持続時間
        float timer;

        public void UseSkill(Player player, PlayerStatus playerStatus)
        {
            if (isOnCooldown) return;

            this.player = player;
            this.playerStatus = playerStatus;

            // スキルの効果をここに実装
            // 例: 一定時間移動速度を上げる、シールドを展開するなど
            playerStatus.DefensePoint.Add(amount);

            // クールダウンを開始
            cooldownTimer = cooldownTime;
            App.PlayData.PlayRecorder.Current?.Skill(playerStatus);
            GameAudio.Play(SoundCue.GinShield,player);
            timer = duration;
            CombatVisuals.Aura(player,"GinSkill_Aura",new UnityEngine.Color(.55f,.82f,1f,.9f),duration);
        }

        public void UpdateSkill()
        {
            if (isOnCooldown)
            {
                cooldownTimer -= UnityEngine.Time.deltaTime;
                if (cooldownTimer < 0f)
                {
                    cooldownTimer = 0f;
                }
            }

            if (timer > 0f)
            {
                timer -= UnityEngine.Time.deltaTime;
                if (timer <= 0f)
                {
                    playerStatus.DefensePoint.Add(-amount);
                    GameAudio.Play(SoundCue.BuffEnd,player);
                    timer = 0f;
                }
            }
        }
    }
}
