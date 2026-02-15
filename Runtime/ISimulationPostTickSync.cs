namespace Validosik.Core.Network.Simulation
{
    public interface ISimulationPostTickSync
    {
        void OnPostSimulationTick(in SimulationFrame frame);
    }
}