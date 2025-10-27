namespace App.Main.Player
{
    class Hp
    {
        public int Current { get; private set; }
        public int Max { get; private set; }

        public Hp(int max)
        {
            Max = max;
            Current = max;
        }

        public void Add(int amount)
        {
            Current += amount;
            if (Current > Max)
            {
                Current = Max;
            }
        }

        public void Subtract(int amount)
        {
            Current -= amount;
            if (Current < 0)
            {
                Current = 0;
            }
        }

        public void DumpStatus()
        {
            UnityEngine.Debug.Log($"HP Status - Current: {Current}, Max: {Max}");
        }
    }
}