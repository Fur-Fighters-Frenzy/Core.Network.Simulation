namespace Validosik.Core.Network.Simulation
{
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
