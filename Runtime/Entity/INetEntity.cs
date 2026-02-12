using Validosik.Core.Network.Types;

namespace Validosik.Core.Network.Simulation.Entity
{
    public interface INetEntity
    {
        uint Uid { get; }
        PlayerId OwnerPid { get; }
    }
}