using System;

namespace Validosik.Core.Network.Simulation
{
    /// <summary>
    /// Snapshot producer/consumer for a specific system kind.
    /// SimWorld calls GetByteCount/TryWriteSnapshot for packing,
    /// and ApplySnapshot for dispatching received slices.
    /// </summary>
    public interface ISnapshotSystem<TKind>
        where TKind : unmanaged, Enum
    {
        TKind Kind { get; }

        int GetSnapshotByteCount(in SimulationFrame frame);
        bool TryWriteSnapshot(in SimulationFrame frame, Span<byte> destination, out int written);

        void ApplySnapshot(in SimulationFrame frame, ReadOnlySpan<byte> blob);
    }
}