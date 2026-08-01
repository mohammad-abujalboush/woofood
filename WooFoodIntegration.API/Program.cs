using Microsoft.EntityFrameworkCore;
using WooFoodIntegration.API.Data;
using WooFoodIntegration.API.Repositories;
using WooFoodIntegration.Application.Interfaces;
using WooFoodIntegration.Application.Services;
using WooFoodIntegration.Domain.Repositories;
using WooFoodIntegration.API.Middleware;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "WooFood Integration API", Version = "v1" });

    // Configure API Key authentication for Swagger UI
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "API Key for authentication. Example: `X-Api-Key: YOUR_API_KEY`",
        Name = "X-Api-Key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "ApiKey"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" }
            },
            new string[] { }
        }
    });
});

// Configure PostgreSQL DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Repositories
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<ITenantRepository, TenantRepository>();
builder.Services.AddScoped<ITenantCredentialRepository, TenantCredentialRepository>();
builder.Services.AddScoped<ISynchronizationLogRepository, SynchronizationLogRepository>();
builder.Services.AddScoped<IApiKeyRepository, ApiKeyRepository>();

// Register Application Services
builder.Services.AddScoped<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IApiKeyService, ApiKeyService>();
builder.Services.AddScoped<IWooCommerceOrderMappingService, WooCommerceOrderMappingService>();
builder.Services.AddScoped<IFoodicsOrderMappingService, FoodicsOrderMappingService>();
builder.Services.AddScoped<IWooCommerceService, WooCommerceService>();
builder.Services.AddScoped<IFoodicsService, FoodicsService>();
builder.Services.AddScoped<ISynchronizationService, SynchronizationService>();
builder.Services.AddScoped<WooCommerceSignatureFilter>();
builder.Services.AddHostedService<WooFoodIntegration.API.Workers.StockSyncBackgroundService>();

// Register HttpClient for external API calls
builder.Services.AddHttpClient();

// Register API Key Authentication Filter
builder.Services.AddScoped<ApiKeyAuthFilter>();

// Configure CORS - allow all for development, restrict in production
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

// Use CORS policy
app.UseCors();

app.UseDefaultFiles(); // Tells the server to look for index.html
app.UseStaticFiles();  // Enables serving HTML, CSS, and JS files

app.UseAuthorization();

app.MapControllers();

// --- AUTO MIGRATE DATABASE ON STARTUP ---
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}
// ----------------------------------------

app.Run();
