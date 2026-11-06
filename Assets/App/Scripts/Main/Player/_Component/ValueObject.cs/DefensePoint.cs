namespace App.Main.Player
{
    public class DefensePoint
    {
        public int Current { get; private set; }
        public int Default { get; private set; }
        public DefensePoint(int defaultPoint = 0)
        {
            Default = defaultPoint;
            Current = defaultPoint;
        }

        public void Add(int amount)
        {
            Current += amount;
        }

        public void Reset()
        {
            Current = Default;
        }

        public void DumpStatus()
        {
            UnityEngine.Debug.Log($"Defense Point Status - Current: {Current}");
        }
    }
}
