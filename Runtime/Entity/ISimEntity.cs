namespace Validosik.Core.Network.Simulation.Entity
{
    public interface ISimEntity<TInput>
    {
        void Simulate(TInput input);
    }

    public interface ISimEntity : IUsesNetEntity
    {
        INetEntity NetEntity { get; }
    }
}