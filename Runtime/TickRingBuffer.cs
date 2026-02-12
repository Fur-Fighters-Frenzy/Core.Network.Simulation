using System;
using System.Collections.Generic;

namespace Validosik.Core.Network.Simulation
{
    /// <summary>
    /// Per-tick ring buffer. Capacity is power-of-two.
    /// Stores a List&lt;T&gt; bucket for each tick slot, keyed by "extended tick" (uint).
    /// </summary>
    public sealed class TickRingBuffer<T>
    {
        private const uint EmptyStamp = uint.MaxValue;

        private readonly int _mask;
        private readonly uint[] _stamp;
        private readonly List<T>[] _buckets;

        private static readonly T[] EmptyArray = Array.Empty<T>();

        public int CapacityTicks { get; }

        public TickRingBuffer(int capacityTicksPowerOfTwo, int initialBucketCapacity = 4)
        {
            if (capacityTicksPowerOfTwo < 2 || (capacityTicksPowerOfTwo & (capacityTicksPowerOfTwo - 1)) != 0)
            {
                throw new ArgumentException("Capacity must be power of two and >= 2", nameof(capacityTicksPowerOfTwo));
            }

            if (initialBucketCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialBucketCapacity));
            }

            CapacityTicks = capacityTicksPowerOfTwo;
            _mask = capacityTicksPowerOfTwo - 1;

            _stamp = new uint[capacityTicksPowerOfTwo];
            Array.Fill(_stamp, EmptyStamp);

            _buckets = new List<T>[capacityTicksPowerOfTwo];
            for (var i = 0; i < _buckets.Length; ++i)
            {
                _buckets[i] = new List<T>(initialBucketCapacity);
            }
        }

        /// <summary>
        /// Adds item into bucket for the given tick. If this slot currently holds a different tick,
        /// the bucket is reset.
        /// </summary>
        public void Add(uint tick, in T item)
        {
            var idx = (int)(tick & (uint)_mask);

            if (_stamp[idx] != tick)
            {
                _stamp[idx] = tick;
                _buckets[idx].Clear();
            }

            _buckets[idx].Add(item);
        }

        /// <summary>
        /// Returns bucket for tick, or empty list view if absent.
        /// </summary>
        public IReadOnlyList<T> Get(uint tick)
        {
            var idx = (int)(tick & (uint)_mask);
            return _stamp[idx] == tick ? _buckets[idx] : EmptyArray;
        }

        /// <summary>
        /// True if bucket exists for tick and has the same stamp.
        /// </summary>
        public bool TryGet(uint tick, out IReadOnlyList<T> items)
        {
            var idx = (int)(tick & (uint)_mask);
            if (_stamp[idx] == tick)
            {
                items = _buckets[idx];
                return true;
            }

            items = EmptyArray;
            return false;
        }

        /// <summary>
        /// Ensures the bucket is prepared for this tick (clears if slot was used by another tick),
        /// then returns a mutable List for direct filling (AddRange etc).
        /// </summary>
        public List<T> GetOrCreateBucket(uint tick)
        {
            var idx = (int)(tick & (uint)_mask);

            if (_stamp[idx] != tick)
            {
                _stamp[idx] = tick;
                _buckets[idx].Clear();
            }

            return _buckets[idx];
        }

        /// <summary>
        /// Clears bucket if it currently stores exactly this tick.
        /// Also resets stamp to EmptyStamp so Get(tick) becomes empty unless re-added.
        /// </summary>
        public void ClearTick(uint tick)
        {
            var idx = (int)(tick & (uint)_mask);
            if (_stamp[idx] == tick)
            {
                _buckets[idx].Clear();
                _stamp[idx] = EmptyStamp;
            }
        }

        /// <summary>
        /// Clears ticks in inclusive range [fromTick..toTick].
        /// Use this on rollback to wipe derived buffers (events).
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
        /// Clears everything (keeps allocated lists).
        /// </summary>
        public void ClearAll()
        {
            for (var i = 0; i < _buckets.Length; ++i)
            {
                _buckets[i].Clear();
                _stamp[i] = EmptyStamp;
            }
        }
    }
}