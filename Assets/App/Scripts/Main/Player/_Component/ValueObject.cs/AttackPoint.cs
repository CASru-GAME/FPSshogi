using System.Runtime.CompilerServices;

namespace App.Main.Player
{
    class AttackPoint
    {
        public int Current { get; private set; }
        public int Default { get; private set; }
        public AttackPoint(int Default)
        {
            Current = Default;
            Default = Default;
        }

        public void Add(int amount)
        {
            Current += amount;
        }

        public void Subtract(int amount)
        {
            Current -= amount;
            if (Current < 0)
            {
                Current = 0;
            }
        }

        public void Reset()
        {
            Current = Default;
        }

        public void DumpStatus()
        {
            UnityEngine.Debug.Log($"Attack Point Status - Current: {Current}");
        }
    }
}
