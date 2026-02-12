namespace Validosik.Core.Network.Simulation
{
    public interface ISimulationSystem
    {
        void Step(in SimulationFrame frame);
    }
}