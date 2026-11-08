namespace SistemaReservas.Application.Abstractions.Persistence
{
    public interface IDatabaseHealthCheck
    {
        Task<bool> CanConnectAsync();
    }
}
