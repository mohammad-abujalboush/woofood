namespace WooFoodIntegration.Domain.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        ITenantRepository Tenants { get; }
        ITenantCredentialRepository TenantCredentials { get; }
        ISynchronizationLogRepository SynchronizationLogs { get; }
        IApiKeyRepository ApiKeys { get; }

        Task<int> CompleteAsync(CancellationToken cancellationToken);
    }
}
