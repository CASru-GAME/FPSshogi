namespace App.Main.Player
{
    public class MoveSpeed
    {
        public float Current { get; private set; }
        public float Default { get; private set; }

        public MoveSpeed(float defaultSpeed)
        {
            Default = defaultSpeed;
            Current = defaultSpeed;
        }

        public void Add(float amount)
        {
            Current += amount;
        }

        public void Subtract(float amount)
        {
            Current -= amount;
            if (Current < 0f)
            {
                Current = 0f;
            }
        }

        public void Multiply(float factor)
        {
            if (factor < 0f) factor = 0f;
            Current *= factor;
        }

        public void Reset()
        {
            Current = Default;
        }

        public void DumpStatus()
        {
            UnityEngine.Debug.Log($"Move Speed Status - Current: {Current}, Default: {Default}");
        }
    }
}