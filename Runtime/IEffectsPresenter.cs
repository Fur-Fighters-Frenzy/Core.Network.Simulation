using System;

namespace Validosik.Core.Network.Simulation
{
    public interface IPresenterObserver
    {
        SimulationState CurrentState { get; }
        event Action<SimulationState> OnSimulationStateChange;
    }

    public sealed class SimulationStateBus : IPresenterObserver
    {
        public SimulationState CurrentState
        {
            get => CurrentState;

            set
            {
                if (CurrentState == value)
                {
                    return;
                }

                CurrentState = value;
                OnSimulationStateChange?.Invoke(value);
            }
        }

        public event Action<SimulationState> OnSimulationStateChange;
    }

    public enum SimulationState : byte
    {
        LocalPrediction,
        Resim,
    }

    public abstract class EffectsPresenterBase
    {
        protected SimulationState currentState;
        protected bool AllowPresentation => currentState != SimulationState.Resim;

        public virtual void Bind(IPresenterObserver observer)
        {
            currentState = observer.CurrentState;
            observer.OnSimulationStateChange += OnSimulationStateChange;
        }

        public virtual void Unbind(IPresenterObserver observer)
        {
            observer.OnSimulationStateChange -= OnSimulationStateChange;
        }

        protected virtual void OnSimulationStateChange(SimulationState state)
        {
            currentState = state;
        }
    }
}