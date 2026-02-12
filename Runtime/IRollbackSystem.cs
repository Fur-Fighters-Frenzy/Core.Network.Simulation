namespace Validosik.Core.Network.Simulation
{
    public interface IRollbackSystem : ISimulationSystem
    {
        void CaptureStates(in SimulationFrame frame);
        void RestoreStates(in SimulationFrame frame);
    }
}