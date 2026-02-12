using Validosik.Core.Network.Simulation.Snapshots;

namespace Validosik.Core.Network.Simulation
{
    public interface ISnapshotStore<TSnapshot> where TSnapshot : class, ISnapshot
    {
        void Save(TSnapshot snapshot);
        bool TryGetAtOrBefore(uint tick, out TSnapshot snapshot);
        bool TryGetAt(uint tick, out TSnapshot snapshot);
    }
}