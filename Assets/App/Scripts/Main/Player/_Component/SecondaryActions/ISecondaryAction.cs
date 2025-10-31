namespace App.Main.Player
{
    public interface ISecondaryAction
    {
        public string secondaryActionName { get; }
        public Player player { get; }
        public PlayerStatus playerStatus { get; }
        public void SecondaryAction(Player player, PlayerStatus playerStatus);
        public void UpdateSecondaryAction();
    }
}
