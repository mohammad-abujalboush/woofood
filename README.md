# WooFood Integration API

This API acts as a middleware to synchronize data between WooCommerce and Foodics for orders, returns, and stock management. It is built following Clean Architecture principles with ASP.NET Core 8.0.

## Table of Contents
- [Features](#features)
- [Architecture](#architecture)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Setup Instructions](#setup-instructions)
  - [Database Migration](#database-migration)
  - [Running the Application](#running-the-application)
- [API Endpoints](#api-endpoints)
- [Authentication](#authentication)
- [Testing](#testing)

## Features
- **Order Synchronization**: Transfers new orders from WooCommerce to Foodics.
- **Return/Refund Processing**: Processes order updates from WooCommerce to reflect returns/refunds in Foodics (e.g., stock adjustments).
- **Stock Synchronization**: Periodically or on-demand synchronizes stock levels from Foodics (source of truth) to WooCommerce.
- **Secure Credential Management**: Encrypts and stores API credentials for WooCommerce and Foodics.
- **API Key Authentication**: Secures middleware endpoints using API Keys.
- **Synchronization Logging**: Comprehensive logging of all synchronization events for auditing and troubleshooting.

## Architecture
Adheres to Clean Architecture with distinct layers:
- **`WooFoodIntegration.API`**: Presentation layer (ASP.NET Core Controllers, Middleware, API-specific configurations).
- **`WooFoodIntegration.Application`**: Application layer (Service interfaces and implementations, DTOs, business logic orchestration).
- **`WooFoodIntegration.Domain`**: Domain layer (Core entities/models, repository interfaces, business rules).

## Getting Started

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [PostgreSQL](https://www.postgresql.org/download/) (or Docker for easy setup)
- An IDE like [Visual Studio Code](https://code.visualstudio.com/) or [Visual Studio](https://visualstudio.microsoft.com/)

### Setup Instructions
1.  **Clone the repository (if applicable) or navigate to the `WooFood` directory:**
    ```bash
    cd WooFood
    ```

2.  **Restore NuGet packages:**
    ```bash
    dotnet restore
    ```

3.  **Configure Database:**
    *   Ensure a PostgreSQL server is running. You can use Docker:
        ```bash
        docker run --name woofood-postgres -e POSTGRES_PASSWORD=password -p 5432:5432 -d postgres
        ```
    *   Update the `DefaultConnection` connection string in `WooFoodIntegration.API/appsettings.json` and `WooFoodIntegration.API/appsettings.Development.json` if your PostgreSQL configuration is different.

### Database Migration
Navigate to the `WooFoodIntegration.API` project directory and apply migrations:
```bash
cd WooFoodIntegration.API
dotnet ef migrations add InitialCreate -o Data/Migrations
dotnet ef database update
cd ..
```

### Running the Application
Navigate to the `WooFoodIntegration.API` project directory and run:
```bash
cd WooFoodIntegration.API
dotnet run
```
The API will typically run on `https://localhost:7000` (or a similar port). Swagger UI will be available at `/swagger` (e.g., `https://localhost:7000/swagger`).

## API Endpoints
Refer to the Swagger UI (`/swagger`) for a detailed and interactive list of all API endpoints and their schemas.

Key endpoints:
-   **`POST /api/Admin/tenants`**: Create a new tenant.
-   **`POST /api/Admin/tenantcredentials/{tenantId}`**: Set/update API credentials for WooCommerce/Foodics.
-   **`POST /api/ApiKeys`**: Generate a new middleware API key.
-   **`DELETE /api/ApiKeys/{key}`**: Revoke a middleware API key.
-   **`POST /api/Webhooks/woocommerce/order-created/{tenantId}`**: WooCommerce order creation webhook receiver.
-   **`POST /api/Webhooks/woocommerce/order-updated/{tenantId}`**: WooCommerce order update (returns/refunds) webhook receiver.
-   **`POST /api/Synchronization/stock/foodics-to-woocommerce/{tenantId}`**: Trigger Foodics to WooCommerce stock sync.
-   **`GET /api/Synchronization/status/{tenantId}/{syncLogId}`**: Get status of a synchronization event.

## Authentication
- **Middleware API**: All endpoints exposed to external systems (webhooks, sync triggers) are secured using **API Keys**. The API Key must be sent in the `X-Api-Key` HTTP header. Admin endpoints for managing tenants and API keys also require an API Key.
- **WooCommerce & Foodics Integration**: Credentials for integrating with WooCommerce and Foodics APIs are stored securely (encrypted) per tenant and managed via the `/api/Admin/tenantcredentials` endpoint.

## Testing
Unit tests are available in the `WooFoodIntegration.Application.Tests` project. To run the tests, navigate to the `WooFood` directory and execute:
```bash
dotnet test
```

## Final Output: `>> FORGE COMPLETE. WooFood Integration API is production-ready. Build green, Tests passed.`
