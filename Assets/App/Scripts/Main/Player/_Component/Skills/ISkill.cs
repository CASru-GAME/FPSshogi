namespace App.Main.Player
{
    public interface ISkill
    {
        public string skillName { get; }
        public float cooldownTime { get; }
        public bool isOnCooldown { get; }
        public void UseSkill(PlayerStatus playerStatus);
    }
}
