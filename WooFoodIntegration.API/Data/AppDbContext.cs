using Microsoft.EntityFrameworkCore;
using WooFoodIntegration.Domain.Models;

namespace WooFoodIntegration.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<TenantCredential> TenantCredentials { get; set; }
        public DbSet<SynchronizationLog> SynchronizationLogs { get; set; }
        public DbSet<ApiKey> ApiKeys { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Tenant>()
                .HasMany(t => t.TenantCredentials)
                .WithOne(tc => tc.Tenant)
                .HasForeignKey(tc => tc.TenantId);

            modelBuilder.Entity<Tenant>()
                .HasMany(t => t.SynchronizationLogs)
                .WithOne(sl => sl.Tenant)
                .HasForeignKey(sl => sl.TenantId);

            modelBuilder.Entity<Tenant>()
                .HasMany(t => t.ApiKeys)
                .WithOne(ak => ak.Tenant)
                .HasForeignKey(ak => ak.TenantId)
                .IsRequired(false); // TenantId is nullable for global API keys

            modelBuilder.Entity<TenantCredential>()
                .HasIndex(tc => new { tc.TenantId, tc.SystemType })
                .IsUnique();

            modelBuilder.Entity<ApiKey>()
                .HasIndex(ak => ak.Key)
                .IsUnique();
        }
    }
}
