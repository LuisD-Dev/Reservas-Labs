namespace SistemaReservas.Infrastructure.Interfaces
{
    public interface IDatabaseHealthCheck
    {
        Task<bool> CanConnectAsync();
    }
}
