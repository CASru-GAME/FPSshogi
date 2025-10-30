namespace App.Main.Player
{
    public class GinSkill : ISkill
    {
        public string skillName => "GinSkill";
        public float cooldownTime => 30f;
        public float cooldownTimer { get; private set; }
        public bool isOnCooldown => cooldownTimer > 0f;
        public PlayerStatus playerStatus { get; private set; }
        public Player player { get; private set; }
        int amount = 99999; // 防御力上昇量

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
        }

        public void UpdateSkill()
        {
            if (isOnCooldown)
            {
                cooldownTimer -= UnityEngine.Time.deltaTime;
                if (cooldownTimer < 0f)
                {
                    playerStatus.DefensePoint.Reset();
                    cooldownTimer = 0f;
                }
            }
        }
    }
}
