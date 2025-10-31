namespace App.Main.Player
{
    public interface IPrimaryAction
    {
            public string primaryActionName { get; }
            public Player player { get; }
            PlayerStatus playerStatus { get; }
        public void PrimaryAction(Player player, PlayerStatus playerStatus);
    }
}
