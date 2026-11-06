using System.Collections.Generic;

namespace App.Main.Player
{
    public class MoveSpeed
    {
        private float unmodified;
        private readonly Dictionary<object, float> multipliers = new Dictionary<object, float>();
        public float Current
        {
            get
            {
                float speed = unmodified;
                foreach (float multiplier in multipliers.Values) speed *= multiplier;
                return speed;
            }
        }
        public float Default { get; private set; }

        public MoveSpeed(float defaultSpeed)
        {
            Default = defaultSpeed;
            unmodified = defaultSpeed;
        }

        public void Add(float amount)
        {
            unmodified += amount;
        }

        public void Subtract(float amount)
        {
            unmodified -= amount;
            if (unmodified < 0f)
            {
                unmodified = 0f;
            }
        }

        public void Multiply(float factor)
        {
            if (factor < 0f) factor = 0f;
            unmodified *= factor;
        }

        // Temporary effects own their multiplier, so refreshing or removing one
        // cannot compound it or erase another active effect.
        public void SetMultiplier(object source, float factor)
        {
            multipliers[source] = factor < 0f ? 0f : factor;
        }

        public void RemoveMultiplier(object source)
        {
            multipliers.Remove(source);
        }

        public void Reset()
        {
            unmodified = Default;
            multipliers.Clear();
        }

        public void DumpStatus()
        {
            UnityEngine.Debug.Log($"Move Speed Status - Current: {Current}, Default: {Default}");
        }
    }
}
