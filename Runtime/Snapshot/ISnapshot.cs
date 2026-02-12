using System;

namespace Validosik.Core.Network.Simulation.Snapshots
{
    public interface ISnapshot
    {
        int GetByteCount();
        bool TryWrite(Span<byte> destination, out int written);
    }
}