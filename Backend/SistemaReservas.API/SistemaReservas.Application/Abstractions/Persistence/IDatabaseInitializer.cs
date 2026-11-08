namespace SistemaReservas.Application.Abstractions.Persistence
{
    public interface IDatabaseInitializer
    {
        Task InitializeAsync();
    }
}
