using System;

namespace Validosik.Core.Network.Simulation
{
    public interface IPresenterObserver
    {
        SimulationState CurrentState { get; }
        event Action<SimulationState> OnSimulationStateChange;
    }
}
