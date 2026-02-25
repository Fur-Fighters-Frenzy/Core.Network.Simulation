using System;

namespace Validosik.Core.Network.Simulation
{
    public sealed class SimulationStateBus : IPresenterObserver
    {
        private SimulationState _currentState = SimulationState.LocalPrediction;

        public SimulationState CurrentState
        {
            get => _currentState;

            set
            {
                if (_currentState == value)
                {
                    return;
                }

                _currentState = value;
                OnSimulationStateChange?.Invoke(value);
            }
        }

        public event Action<SimulationState> OnSimulationStateChange;
    }
}
