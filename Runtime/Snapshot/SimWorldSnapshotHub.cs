using System;
using System.Collections.Generic;
using System.Buffers;
using System.Buffers.Binary;
using Validosik.Core.Network.Events;

namespace Validosik.Core.Network.Simulation.Snapshots
{
    /// <summary>
    /// Snapshot hub: builds one snapshot batch per tick and dispatches received batches by system kind.
    /// Payload format: [u32 tick][f32 delta][u16 count]{ [u16 kind][u16 len][blob] }*
    /// </summary>
    public sealed class SimWorldSnapshotHub<TKind, TCodec> : IDisposable
        where TKind : unmanaged, Enum
        where TCodec : struct, IKindCodec<TKind>
    {
        private const int HeaderSize = 4 + 4 + 2;

        private readonly Dictionary<TKind, ISnapshotSystem<TKind>> _systemsByKind = new();
        private readonly List<ISnapshotSystem<TKind>> _orderedSystems = new();

        private readonly EventsBuilder<TKind, TCodec, NoEnvelope> _tlv;
        private byte[] _payload;
        private int _payloadWritten;

        public SimWorldSnapshotHub(int initialCapacity = 512)
        {
            _tlv = new EventsBuilder<TKind, TCodec, NoEnvelope>(initialCapacity);
            _payload = ArrayPool<byte>.Shared.Rent(Math.Max(initialCapacity + HeaderSize, 64));
            _payloadWritten = 0;
        }

        public int RegisteredSystemsCount => _orderedSystems.Count;

        public ISnapshotSystem<TKind> GetRegisteredSystemAt(int index) => _orderedSystems[index];

        public bool TryGetRegisteredSystem(TKind kind, out ISnapshotSystem<TKind> system) =>
            _systemsByKind.TryGetValue(kind, out system);

        public void Register(ISnapshotSystem<TKind> system)
        {
            if (_systemsByKind.TryGetValue(system.Kind, out var current))
            {
                if (ReferenceEquals(current, system))
                {
                    return;
                }

                _systemsByKind[system.Kind] = system;
                var index = IndexOfOrderedSystem(current);
                if (index >= 0)
                {
                    _orderedSystems[index] = system;
                    return;
                }
            }
            else
            {
                _systemsByKind.Add(system.Kind, system);
            }

            _orderedSystems.Add(system);
        }

        public void Unregister(ISnapshotSystem<TKind> system)
        {
            if (_systemsByKind.TryGetValue(system.Kind, out var s) && ReferenceEquals(s, system))
            {
                _systemsByKind.Remove(system.Kind);
                RemoveOrderedSystem(s);
            }
        }

        /// <summary>
        /// Builds snapshot payload for the given tick using registered systems order.
        /// </summary>
        public ReadOnlySpan<byte> BuildPayload(in SimulationFrame frame) =>
            BuildPayload(frame, _orderedSystems);

        /// <summary>
        /// Builds snapshot payload for the given tick.
        /// </summary>
        public ReadOnlySpan<byte> BuildPayload(in SimulationFrame frame,
            IReadOnlyList<ISnapshotSystem<TKind>> orderedSystems)
        {
            _tlv.Reset();

            for (int i = 0, ilen = orderedSystems.Count; i < ilen; ++i)
            {
                var sys = orderedSystems[i];

                var size = sys.GetSnapshotByteCount(frame);
                if (size <= 0)
                {
                    continue;
                }

                if (size <= 64)
                {
                    Span<byte> tmp = stackalloc byte[size];
                    if (!sys.TryWriteSnapshot(frame, tmp, out var w) || w != size)
                    {
                        continue;
                    }

                    _tlv.AddRaw(sys.Kind, tmp);
                    continue;
                }

                var buf = ArrayPool<byte>.Shared.Rent(size);
                try
                {
                    var span = buf.AsSpan(0, size);
                    if (!sys.TryWriteSnapshot(frame, span, out var w) || w != size)
                    {
                        continue;
                    }

                    _tlv.AddRaw(sys.Kind, span);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buf);
                }
            }

            var tlv = _tlv.BuildPayload();

            EnsurePayload(HeaderSize + tlv.Length);

            BinaryPrimitives.WriteUInt32LittleEndian(_payload.AsSpan(0, 4), frame.Tick);

            var deltaBits = BitConverter.SingleToInt32Bits(frame.Delta);
            BinaryPrimitives.WriteInt32LittleEndian(_payload.AsSpan(4, 4), deltaBits);

            tlv.CopyTo(_payload.AsSpan(HeaderSize));

            _payloadWritten = HeaderSize + tlv.Length;
            return new ReadOnlySpan<byte>(_payload, 0, _payloadWritten);
        }

        /// <summary>
        /// Dispatches snapshot payload to registered systems by kind.
        /// </summary>
        public bool TryApplyPayload(ReadOnlySpan<byte> payload, out SimulationFrame frame)
        {
            frame = default;

            if (payload.Length < HeaderSize)
            {
                return false;
            }

            var tick = BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(0, 4));

            var deltaBits = BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(4, 4));
            var delta = BitConverter.Int32BitsToSingle(deltaBits);

            frame = new SimulationFrame(tick, delta);

            var tlv = payload.Slice(HeaderSize);
            var r = new EventsReader<TKind, TCodec>(tlv);

            while (r.TryRead(out var kind, out var blob))
            {
                if (_systemsByKind.TryGetValue(kind, out var sys))
                {
                    sys.ApplySnapshot(frame, blob);
                }
            }

            return true;
        }

        public void Dispose()
        {
            _tlv.Dispose();
            if (_payload != null)
            {
                ArrayPool<byte>.Shared.Return(_payload);
                _payload = null;
            }
        }

        private void EnsurePayload(int need)
        {
            if (need <= _payload.Length)
            {
                return;
            }

            var newSize = Math.Max(_payload.Length * 2, need);
            var newBuf = ArrayPool<byte>.Shared.Rent(newSize);
            System.Buffer.BlockCopy(_payload, 0, newBuf, 0, _payloadWritten);
            ArrayPool<byte>.Shared.Return(_payload);
            _payload = newBuf;
        }

        private int IndexOfOrderedSystem(ISnapshotSystem<TKind> target)
        {
            for (int i = 0, ilen = _orderedSystems.Count; i < ilen; ++i)
            {
                if (ReferenceEquals(_orderedSystems[i], target))
                {
                    return i;
                }
            }

            return -1;
        }

        private void RemoveOrderedSystem(ISnapshotSystem<TKind> target)
        {
            for (int i = 0, ilen = _orderedSystems.Count; i < ilen; ++i)
            {
                if (ReferenceEquals(_orderedSystems[i], target))
                {
                    _orderedSystems.RemoveAt(i);
                    return;
                }
            }
        }

        /// <summary>
        /// No-op envelope for EventsBuilder when we only want TLV payload.
        /// </summary>
        private readonly struct NoEnvelope : IEnvelopeFactory
        {
            public int GetByteCount(int payloadLen) => payloadLen;

            public bool TryWrite(ReadOnlySpan<byte> payload, Span<byte> destination, out int written)
            {
                written = 0;
                if (destination.Length < payload.Length) return false;
                payload.CopyTo(destination);
                written = payload.Length;
                return true;
            }
        }
    }
}
