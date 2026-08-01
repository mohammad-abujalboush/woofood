using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WooFoodIntegration.Application.Interfaces;

namespace WooFoodIntegration.API.Middleware
{
    public class ApiKeyAuthFilter : IAsyncActionFilter
    {
        private const string APIKEYNAME = "X-Api-Key";
        private readonly IApiKeyService _apiKeyService;

        public ApiKeyAuthFilter(IApiKeyService apiKeyService)
        {
            _apiKeyService = apiKeyService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!context.HttpContext.Request.Headers.TryGetValue(APIKEYNAME, out var extractedApiKey))
            {
                context.Result = new UnauthorizedObjectResult("API Key missing.");
                return;
            }

            // Ensure extractedApiKey is not null before passing it to GetApiKeyAsync
            if (string.IsNullOrWhiteSpace(extractedApiKey))
            {
                context.Result = new UnauthorizedObjectResult("API Key is empty.");
                return;
            }
            var apiKey = await _apiKeyService.GetApiKeyAsync(extractedApiKey!, CancellationToken.None); // Use null-forgiving operator after null if confident it's not null here

            if (apiKey == null)
            {
                context.Result = new UnauthorizedObjectResult("Invalid API Key.");
                return;
            }

            await next();
        }
    }
}
