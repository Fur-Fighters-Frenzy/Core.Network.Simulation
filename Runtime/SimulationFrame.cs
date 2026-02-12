namespace Validosik.Core.Network.Simulation
{
    public readonly struct SimulationFrame
    {
        public readonly uint Tick;
        public readonly float Delta;

        public SimulationFrame(uint tick, float delta)
        {
            Tick = tick;
            Delta = delta;
        }
    }
}