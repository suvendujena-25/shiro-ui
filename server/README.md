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
- Exposing OpenAPI and Swagger documentation.
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

#### Swashbuckle.AspNetCore

Used to add Swagger support to the Web API.

Why we added it:

- Provides the Swagger UI page for testing API endpoints in the browser.
- Generates Swagger/OpenAPI metadata that tools can read.
- Makes local API development easier because endpoints can be viewed and tested from `/swagger`.

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

## API Documentation Endpoints

In the development environment, the API exposes documentation at:

```text
/openapi/v1.json
/swagger
```

`/openapi/v1.json` returns the machine-readable OpenAPI document.

`/swagger` opens the Swagger UI page for testing API endpoints in the browser.

## Current Shiro Agent Flow

The backend currently supports a safe starter agent flow:

```text
ChatController -> ChatService -> ToolRouter
```

`ToolRouter` separates actions into:

- Safe tools, such as `create_task`, which can run immediately.
- Risky tools, such as `send_email`, which require user approval first.

No real email, payment, file deletion, or external messaging is executed yet.
Risky tools are stored as approval requests and then passed through a fake executor only after approval.

For normal chat messages that are not routed to a local tool, `ChatService` calls the configured AI provider.
The default provider is Ollama, so Shiro can run with a free local model.

## Ollama Configuration

Install Ollama, then pull the configured model:

```powershell
& "C:\Users\suven\AppData\Local\Programs\Ollama\ollama.exe" pull llama3.2:3b
```

If `ollama` is available in your terminal PATH, this shorter command also works:

```powershell
ollama pull llama3.2:3b
```

The current local AI settings live in `appsettings.json`:

```json
{
  "AI": {
    "Provider": "Ollama"
  },
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "Model": "llama3.2:3b"
  }
}
```

After pulling the model, restart `Shiro.Api`.

## OpenAI Configuration

OpenAI is optional. Use this only if you want to switch from local Ollama to OpenAI.

Do not store the OpenAI API key in `appsettings.json`.

For local development, use either user secrets:

```powershell
cd Shiro.Api
dotnet user-secrets init
dotnet user-secrets set "OpenAI:ApiKey" "YOUR_API_KEY"
```

Or set an environment variable:

```powershell
$env:OPENAI_API_KEY = "YOUR_API_KEY"
```

The current model setting lives in `appsettings.json`:

```json
{
  "OpenAI": {
    "BaseUrl": "https://api.openai.com",
    "Model": "gpt-4.1-mini"
  }
}
```

## Current API Endpoints

```http
POST /api/chat
GET  /api/approvals/pending
GET  /api/approvals/{approvalId}
POST /api/approvals/{approvalId}/approve
POST /api/approvals/{approvalId}/reject
GET  /api/tasks
POST /api/tasks
GET  /api/audit
GET  /api/audit/approvals/{approvalId}
```

## Microservice Integration

`Shiro.Api` can be used as a standalone HTTP microservice from Angular, React, mobile, or any other client.

For React development, CORS currently allows:

```text
http://localhost:3000
http://127.0.0.1:3000
http://localhost:5173
http://127.0.0.1:5173
```

React can call the chat API like this:

```ts
const response = await fetch('http://localhost:5293/api/chat', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    conversationId,
    message: 'hello shiro'
  })
});

const data = await response.json();
```

The main contract is:

```text
POST /api/chat
```

Request:

```json
{
  "conversationId": "optional-existing-conversation-id",
  "message": "hello shiro"
}
```

Response:

```json
{
  "conversationId": "conversation-id",
  "responseType": 1,
  "message": "assistant reply",
  "requiresApproval": false,
  "toolName": null,
  "approvalId": null
}
```

## Manual Test Flow

Create a safe task through chat:

```http
POST /api/chat
```

```json
{
  "message": "create task buy milk"
}
```

This immediately creates a task and writes a safe-tool audit entry.

Create a risky approval request:

```http
POST /api/chat
```

```json
{
  "message": "send email to John saying I will be late"
}
```

This returns `responseType: ApprovalRequired` and an `approvalId`.

Approve the risky request:

```http
POST /api/approvals/{approvalId}/approve
```

This runs only the fake tool executor. It does not send a real email.

Review history:

```http
GET /api/audit
```
