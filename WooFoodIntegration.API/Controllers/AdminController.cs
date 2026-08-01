using Microsoft.AspNetCore.Mvc;
using WooFoodIntegration.Application.DTOs;
using WooFoodIntegration.Application.Interfaces;

namespace WooFoodIntegration.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // TODO: Implement API Key authentication for admin endpoints
    public class AdminController : ControllerBase
    {
        private readonly ITenantService _tenantService;

        public AdminController(ITenantService tenantService)
        {
            _tenantService = tenantService;
        }

        /// <summary>
        /// Creates a new tenant.
        /// </summary>
        /// <param name="createTenantDto">Tenant creation data.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The created tenant details.</returns>
        [HttpPost("tenants")]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(TenantDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateTenant([FromBody] CreateTenantDto createTenantDto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var tenant = await _tenantService.CreateTenantAsync(createTenantDto, cancellationToken);
            return CreatedAtAction(nameof(GetTenantById), new { tenantId = tenant.Id }, tenant);
        }

        /// <summary>
        /// Retrieves a tenant by ID.
        /// </summary>
        /// <param name="tenantId">The ID of the tenant.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The tenant details.</returns>
        [HttpGet("tenants/{tenantId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TenantDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTenantById(Guid tenantId, CancellationToken cancellationToken)
        {
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId, cancellationToken);
            if (tenant == null)
            {
                return NotFound($"Tenant with ID {tenantId} not found.");
            }
            return Ok(tenant);
        }

        /// <summary>
        /// Sets or updates WooCommerce and Foodics API credentials for a tenant.
        /// </summary>
        /// <param name="tenantId">The ID of the tenant.</param>
        /// <param name="credentialsDto">The credentials to set/update.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>A confirmation of the update.</returns>
        [HttpPost("tenantcredentials/{tenantId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateTenantCredentials(Guid tenantId, [FromBody] CreateTenantCredentialDto credentialsDto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (tenantId != credentialsDto.TenantId)
            {
                return BadRequest("Tenant ID in route must match Tenant ID in body.");
            }

            try
            {
                await _tenantService.UpdateTenantCredentialsAsync(tenantId, credentialsDto, cancellationToken);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}