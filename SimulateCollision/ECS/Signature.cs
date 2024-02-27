using System.Collections;
using System.Diagnostics;

namespace SimulateCollision.ECS
{
    public record struct Signature(int Value)
    {
        public void Reset() => Value = 0;

        public void Set(int index, bool value)
        {
            Debug.Assert(index >= 0 && index < 32);

            int mask = 1 << index;

            Value = value ? (Value | mask) : (Value & ~mask);
        }

        public void Set(int index) => Set(index, true);
        public void Unset(int index) => Set(index, false);

        public override string ToString()
        {
            return $"Signature : {Value:X8}";
        }
    }
}
