namespace Validosik.Core.Network.Simulation.Entity
{
    public interface IRollbackEntity<TState> : ISimEntity
    {
        TState CaptureState();
        void RestoreState(in TState state);
    }
}