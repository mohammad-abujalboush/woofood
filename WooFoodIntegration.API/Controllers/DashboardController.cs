using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using WooFoodIntegration.API.Data;
using WooFoodIntegration.Domain.Models;
using WooFoodIntegration.Application.Interfaces; // <-- Injects your exact service namespace

namespace WooFoodIntegration.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. ADMIN ENDPOINTS (For index.html)
        // ==========================================

        [HttpGet("logs")]
        public async Task<IActionResult> GetLiveLogs()
        {
            var logs = await _context.SynchronizationLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(50)
                .Select(l => new 
                { 
                    l.Timestamp, 
                    l.EventType, 
                    l.Status, 
                    l.Message,
                    l.TargetSystem
                })
                .ToListAsync();

            return Ok(logs);
        }

        [HttpGet("tenants")]
        public async Task<IActionResult> GetTenants()
        {
            var tenants = await _context.Tenants
                .Select(t => new 
                { 
                    t.Id, 
                    t.Name, 
                    t.WooCommerceBaseUrl, 
                    t.FoodicsBaseUrl 
                })
                .ToListAsync();

            return Ok(tenants);
        }

        [HttpPost("tenants")]
        public async Task<IActionResult> CreateTenant([FromBody] TenantConfigDto config)
        {
            var tenant = new Tenant 
            { 
                Id = Guid.NewGuid(), 
                Name = config.Name ?? "Unnamed Client",
                WooCommerceBaseUrl = config.WooCommerceBaseUrl ?? "",
                FoodicsBaseUrl = config.FoodicsBaseUrl ?? "https://api-sandbox.foodics.com",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Tenant created successfully", TenantId = tenant.Id });
        }

        // ==========================================
        // 2. CLIENT PORTAL ENDPOINTS (For client.html)
        // ==========================================

        [HttpGet("logs/{tenantId}")]
        public async Task<IActionResult> GetLogsByTenant(Guid tenantId)
        {
            var logs = await _context.SynchronizationLogs
                .Where(l => l.TenantId == tenantId)
                .OrderByDescending(l => l.Timestamp)
                .Take(50)
                .Select(l => new 
                { 
                    l.Timestamp, 
                    l.EventType, 
                    l.Status, 
                    l.Message,
                    l.TargetSystem
                })
                .ToListAsync();

            return Ok(logs);
        }

        [HttpGet("tenants/{tenantId}")]
        public async Task<IActionResult> GetTenant(Guid tenantId)
        {
            var tenant = await _context.Tenants
                .Where(t => t.Id == tenantId)
                .Select(t => new { 
                    t.Id, 
                    t.Name, 
                    t.WooCommerceBaseUrl, 
                    t.FoodicsBaseUrl 
                })
                .FirstOrDefaultAsync();

            if (tenant == null) return NotFound("Client not found.");
            return Ok(tenant);
        }

        [HttpPut("tenants/{tenantId}")]
        public async Task<IActionResult> UpdateTenantConfig(
            Guid tenantId, 
            [FromBody] TenantConfigDto config,
            [FromServices] IEncryptionService encryptionService) // <-- Uses your built-in app service
        {
            var tenant = await _context.Tenants.FindAsync(tenantId);
            if (tenant == null) return NotFound("Client not found.");

            if (!string.IsNullOrEmpty(config.Name)) tenant.Name = config.Name;
            if (!string.IsNullOrEmpty(config.WooCommerceBaseUrl)) tenant.WooCommerceBaseUrl = config.WooCommerceBaseUrl;
            if (!string.IsNullOrEmpty(config.FoodicsBaseUrl)) tenant.FoodicsBaseUrl = config.FoodicsBaseUrl;
            
            tenant.UpdatedAt = DateTime.UtcNow;

            // 2. Encrypt and Save Foodics API Key
            if (!string.IsNullOrEmpty(config.FoodicsApiKey))
            {
                var foodicsCred = await _context.TenantCredentials
                    .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.SystemType == "Foodics");

                var encrypted = encryptionService.Encrypt(config.FoodicsApiKey);

                if (foodicsCred == null)
                {
                    _context.TenantCredentials.Add(new TenantCredential 
                    { 
                        Id = Guid.NewGuid(),
                        TenantId = tenantId, 
                        SystemType = "Foodics", 
                        EncryptedApiKey = encrypted.encryptedData, 
                        Iv = encrypted.iv,
                        Salt = encrypted.salt
                    });
                }
                else
                {
                    foodicsCred.EncryptedApiKey = encrypted.encryptedData;
                    foodicsCred.Iv = encrypted.iv;
                    foodicsCred.Salt = encrypted.salt;
                }
            }

            // 3. Encrypt and Save WooCommerce Webhook Secret
            if (!string.IsNullOrEmpty(config.WebhookSecret))
            {
                var wooCred = await _context.TenantCredentials
                    .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.SystemType == "WooCommerce");

                var encrypted = encryptionService.Encrypt(config.WebhookSecret);

                if (wooCred == null)
                {
                    _context.TenantCredentials.Add(new TenantCredential 
                    { 
                        Id = Guid.NewGuid(),
                        TenantId = tenantId, 
                        SystemType = "WooCommerce", 
                        EncryptedApiKey = encrypted.encryptedData, 
                        Iv = encrypted.iv,
                        Salt = encrypted.salt
                    });
                }
                else
                {
                    wooCred.EncryptedApiKey = encrypted.encryptedData;
                    wooCred.Iv = encrypted.iv;
                    wooCred.Salt = encrypted.salt;
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Configuration and credentials securely encrypted and saved." });
        }
    }

    public class TenantConfigDto
    {
        public string? Name { get; set; }
        public string? WooCommerceBaseUrl { get; set; }
        public string? FoodicsBaseUrl { get; set; }
        public string? WebhookSecret { get; set; }
        public string? FoodicsApiKey { get; set; }
    }
}