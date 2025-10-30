namespace App.Main.Player
{
    public class KinSkill : ISkill
    {
        public string skillName => "KinSkill";
        public float cooldownTime => 10f;
        public float cooldownTimer { get; private set; }
        public bool isOnCooldown => cooldownTimer > 0f;
        public PlayerStatus playerStatus { get; private set; }
        public Player player { get; private set; }
        int amount = 10;
        float duration = 5f;
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
            timer = duration;
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
                if (timer < 0f)
                {
                    playerStatus.DefensePoint.Reset();
                    timer = 0f;
                }
            }
        }
    }
}