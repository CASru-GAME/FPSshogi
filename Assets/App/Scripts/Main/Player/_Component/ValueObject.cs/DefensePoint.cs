namespace App.Main.Player
{
    public class DefensePoint
    {
        public int Current { get; private set; }
        public DefensePoint()
        {
            Current = 0;
        }

        public void Add(int amount)
        {
            Current += amount;
        }

        public void Reset()
        {
            Current = 0;
        }

        public void DumpStatus()
        {
            UnityEngine.Debug.Log($"Defense Point Status - Current: {Current}");
        }
    }
}
