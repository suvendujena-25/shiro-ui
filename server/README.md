# Shiro Server

This folder contains the backend projects for the Shiro application.

## Project Structure

```text
server/
  Shiro.Api/
  Shiro.Core/
  Shiro.Infrastructure/
```

## Projects

### Shiro.Api

`Shiro.Api` is the ASP.NET Core Web API project.

It is responsible for:

- Receiving HTTP requests from the frontend.
- Configuring authentication and authorization.
- Registering application services.
- Exposing OpenAPI documentation.
- Starting and hosting the backend application.

### Shiro.Core

`Shiro.Core` is the class library for core business logic.

It should usually contain:

- Domain entities.
- Business models.
- Interfaces.
- Shared business rules.

This project should stay clean and avoid unnecessary external dependencies.

### Shiro.Infrastructure

`Shiro.Infrastructure` is the class library for external systems and technical implementation details.

It is responsible for things like:

- Database access.
- SQL Server configuration.
- Keycloak service integrations.
- Redis or cache-related implementations.
- Repository and external API implementations.

## Package Details

### Shiro.Api Packages

#### Microsoft.AspNetCore.OpenApi

Used to generate OpenAPI documentation for the Web API.

Why we added it:

- Helps expose machine-readable API documentation.
- Supports endpoints like `/openapi/v1.json`.
- Makes API testing and client integration easier.

#### Microsoft.EntityFrameworkCore.Design

Used for Entity Framework Core design-time commands.

Why we added it:

- Required for commands like `dotnet ef migrations add` and `dotnet ef database update`.
- Allows the API project to work as the startup project for EF Core migrations.
- Helps EF Core discover configuration when generating database migrations.

#### Microsoft.AspNetCore.Authentication.JwtBearer

Used to validate JWT bearer tokens in ASP.NET Core.

Why we added it:

- The API needs to protect endpoints from unauthenticated users.
- Frontend requests can send access tokens in the `Authorization` header.
- ASP.NET Core can validate the token before allowing access to secured endpoints.

#### Keycloak.AuthServices.Authentication

Used to integrate Keycloak authentication with ASP.NET Core.

Why we added it:

- Simplifies Keycloak authentication setup.
- Reduces manual JWT configuration.
- Allows authentication settings to be configured cleanly from `appsettings.json`.

#### Keycloak.AuthServices.Sdk

Used to call Keycloak APIs from the .NET backend.

Why we added it:

- Allows backend services to communicate with Keycloak.
- Useful for reading users, roles, realms, or other identity data.
- Helpful when the application needs server-side identity management features.

#### StackExchange.Redis

Used to connect the .NET application to Redis.

Why we added it:

- Redis can be used for caching frequently accessed data.
- It can support temporary data, distributed sessions, OTPs, rate limiting, or fast lookups.
- Helps reduce repeated database or external service calls.

#### Serilog.AspNetCore

Used to integrate Serilog with ASP.NET Core logging.

Why we added it:

- Provides structured logging instead of only plain text logs.
- Helps capture useful request and error information.
- Makes logs easier to search and analyze.

#### Serilog.Enrichers.Thread

Used to add thread information to Serilog logs.

Why we added it:

- Helps identify which thread wrote a log entry.
- Useful when debugging async code, background tasks, or concurrent requests.
- Adds more context to application logs.

#### Serilog.Sinks.Console

Used to write Serilog logs to the console.

Why we added it:

- Shows logs directly in the terminal while running the API.
- Useful during local development and debugging.
- Helps quickly inspect requests, errors, and application startup logs.

#### Serilog.Sinks.File

Used to write Serilog logs to files.

Why we added it:

- Keeps log history after the application stops.
- Useful for debugging issues that happened earlier.
- Helps with reviewing server behavior over time.

### Shiro.Infrastructure Packages

#### Microsoft.EntityFrameworkCore.SqlServer

Used as the SQL Server provider for Entity Framework Core.

Why we added it:

- Allows EF Core to connect to SQL Server.
- Enables database queries, migrations, and table creation for SQL Server.
- Supports infrastructure-level database implementation.

#### Microsoft.AspNetCore.Authentication.JwtBearer

Used for JWT authentication-related types.

Why we added it:

- Usually this package belongs mainly in `Shiro.Api`.
- It is only needed in `Shiro.Infrastructure` if infrastructure services directly use JWT authentication classes.
- If infrastructure does not use JWT types directly, this dependency may not be required there.

#### Keycloak.AuthServices.Sdk

Used to communicate with Keycloak from infrastructure services.

Why we added it:

- Supports services that call the Keycloak Admin API.
- Useful for identity-related infrastructure code.
- Allows features like user lookup, role lookup, or identity synchronization.

## Dependency Flow

The intended dependency flow is:

```text
Shiro.Api -> Shiro.Infrastructure -> Shiro.Core
Shiro.Api -> Shiro.Core
```

`Shiro.Core` should not depend on `Shiro.Api` or `Shiro.Infrastructure`.
