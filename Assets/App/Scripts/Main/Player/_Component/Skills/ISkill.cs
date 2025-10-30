namespace App.Main.Player
{
    public interface ISkill
    {
        public string skillName { get; }
        public float cooldownTime { get; }
        public float cooldownTimer { get; }
        public bool isOnCooldown { get; }
        PlayerStatus playerStatus { get; }
        Player player { get; }
        public void UseSkill(Player player, PlayerStatus playerStatus);
        public void UpdateSkill();
    }
}
