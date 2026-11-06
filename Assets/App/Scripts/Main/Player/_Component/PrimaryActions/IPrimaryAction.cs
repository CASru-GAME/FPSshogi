namespace App.Main.Player
{
    public interface IPrimaryAction
    {
            public string primaryActionName { get; }
            float CooldownRemaining { get; }
            float CooldownDuration { get; }
            public Player player { get; }
            PlayerStatus playerStatus { get; }
        public void PrimaryAction(Player player, PlayerStatus playerStatus);
    }
}
