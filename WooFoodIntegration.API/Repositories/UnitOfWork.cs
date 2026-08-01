using WooFoodIntegration.API.Data;
using WooFoodIntegration.Domain.Repositories;

namespace WooFoodIntegration.API.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private ITenantRepository? _tenants;
        private ITenantCredentialRepository? _tenantCredentials;
        private ISynchronizationLogRepository? _synchronizationLogs;
        private IApiKeyRepository? _apiKeys;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        public ITenantRepository Tenants => _tenants ??= new TenantRepository(_context);
        public ITenantCredentialRepository TenantCredentials => _tenantCredentials ??= new TenantCredentialRepository(_context);
        public ISynchronizationLogRepository SynchronizationLogs => _synchronizationLogs ??= new SynchronizationLogRepository(_context);
        public IApiKeyRepository ApiKeys => _apiKeys ??= new ApiKeyRepository(_context);

        public async Task<int> CompleteAsync(CancellationToken cancellationToken)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
