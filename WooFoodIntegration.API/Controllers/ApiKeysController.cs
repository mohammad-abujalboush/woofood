using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WooFoodIntegration.Application.DTOs;
using WooFoodIntegration.Application.Interfaces;

namespace WooFoodIntegration.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // TODO: Implement API Key authentication for API Key management (e.g., admin API key)
    public class ApiKeysController : ControllerBase
    {
        private readonly IApiKeyService _apiKeyService;

        public ApiKeysController(IApiKeyService apiKeyService)
        {
            _apiKeyService = apiKeyService;
        }

        /// <summary>
        /// Generates a new API Key for the middleware. The raw key is returned only once.
        /// </summary>
        /// <param name="createApiKeyDto">API Key creation data.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The raw API key.</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GenerateApiKey([FromBody] CreateApiKeyDto createApiKeyDto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var newApiKey = await _apiKeyService.GenerateApiKeyAsync(createApiKeyDto, cancellationToken);
            return CreatedAtAction(nameof(GenerateApiKey), newApiKey); // Return the raw key here.
        }

        /// <summary>
        /// Revokes an existing API Key.
        /// </summary>
        /// <param name="key">The raw API key to revoke.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>No content if successful.</returns>
        [HttpDelete("{key}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RevokeApiKey(string key, CancellationToken cancellationToken)
        {
            var success = await _apiKeyService.RevokeApiKeyAsync(key, cancellationToken);
            if (!success)
            {
                return NotFound("API Key not found or already revoked.");
            }
            return NoContent();
        }
    }
}