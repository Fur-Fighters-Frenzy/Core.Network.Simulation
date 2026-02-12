using System;

namespace Validosik.Core.Network.Simulation
{
    /// <summary>
    /// Per-tick ring buffer for a single value (0..1) per tick.
    ///
    /// Capacity is power-of-two. Values are keyed by "extended tick" (uint).
    /// When a new tick maps to the same slot, the previous value is overwritten.
    ///
    /// Typical usage:
    /// - snapshots (one snapshot per tick, or every N ticks)
    /// - inputs (one command per tick)
    /// - corrections (one correction per tick)
    /// </summary>
    public sealed class TickValueRing<T>
    {
        // A stamp value that can never be a valid tick in our usage.
        // This avoids "tick 0 ambiguity" and makes empty slots explicit.
        private const uint EmptyStamp = uint.MaxValue;

        private readonly int _mask;
        private readonly uint[] _stamp;
        private readonly T[] _values;

        public int CapacityTicks { get; }

        public TickValueRing(int capacityTicksPowerOfTwo)
        {
            if (capacityTicksPowerOfTwo < 2 || (capacityTicksPowerOfTwo & (capacityTicksPowerOfTwo - 1)) != 0)
            {
                throw new ArgumentException("Capacity must be power of two and >= 2", nameof(capacityTicksPowerOfTwo));
            }

            CapacityTicks = capacityTicksPowerOfTwo;
            _mask = capacityTicksPowerOfTwo - 1;

            _stamp = new uint[capacityTicksPowerOfTwo];
            Array.Fill(_stamp, EmptyStamp);

            _values = new T[capacityTicksPowerOfTwo];
        }

        /// <summary>
        /// Stores value for the given tick. Overwrites any previous value in that slot.
        /// </summary>
        public void Set(uint tick, in T value)
        {
            var idx = (int)(tick & (uint)_mask);
            _stamp[idx] = tick;
            _values[idx] = value;
        }

        /// <summary>
        /// Returns true if we have a value stored exactly at this tick.
        /// </summary>
        public bool TryGetAt(uint tick, out T value)
        {
            var idx = (int)(tick & (uint)_mask);
            if (_stamp[idx] == tick)
            {
                value = _values[idx];
                return true;
            }

            value = default!;
            return false;
        }

        /// <summary>
        /// Finds a value at or before 'tick' by scanning backwards within the ring capacity.
        /// O(capacity) worst-case, but capacity is typically small (64..2048).
        /// </summary>
        public bool TryFindAtOrBefore(uint tick, out uint foundTick, out T value)
        {
            for (var i = 0; i < CapacityTicks; ++i)
            {
                var candidate = tick - (uint)i;
                if (TryGetAt(candidate, out value))
                {
                    foundTick = candidate;
                    return true;
                }
            }

            foundTick = 0;
            value = default!;
            return false;
        }

        /// <summary>
        /// Clears value if it currently stores exactly this tick.
        /// </summary>
        public void ClearTick(uint tick)
        {
            var idx = (int)(tick & (uint)_mask);
            if (_stamp[idx] == tick)
            {
                _stamp[idx] = EmptyStamp;
                _values[idx] = default!;
            }
        }

        /// <summary>
        /// Clears ticks in inclusive range [fromTick..toTick].
        /// Useful on rollback to invalidate derived cached values.
        /// </summary>
        public void ClearRange(uint fromTick, uint toTick)
        {
            if (toTick < fromTick)
            {
                return;
            }

            for (var t = fromTick; t <= toTick; ++t)
            {
                ClearTick(t);
            }
        }

        /// <summary>
        /// Clears everything.
        /// </summary>
        public void ClearAll()
        {
            Array.Fill(_stamp, EmptyStamp);
            Array.Clear(_values, 0, _values.Length);
        }
    }
}