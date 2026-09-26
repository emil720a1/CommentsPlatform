# CommentsPlatform

A full-stack application for creating, viewing, and discussing comments with support for nested replies and file attachments.

The project name is currently provisional and may be changed later.

## Overview

CommentsPlatform is designed using Clean Architecture principles.

The backend is implemented as an ASP.NET Core Web API. The frontend is implemented using Angular.

## Technologies

- ASP.NET Core Web API
- Clean Architecture
- CQRS and MediatR
- Entity Framework Core
- Microsoft SQL Server
- Angular
- Docker
- xUnit
- SonarCloud

## Planned Features

- create comments;
- reply to existing comments;
- display nested replies;
- sort and paginate comments;
- validate user input;
- protect against XSS and SQL injection;
- attach images and text files;
- store attachment metadata;
- provide automated unit and integration tests.

## Architecture

The backend follows the Clean Architecture approach:

```text
src/
├── CommentsPlatform.Domain
├── CommentsPlatform.Application
├── CommentsPlatform.Infrastructure
└── CommentsPlatform.Api

tests/
├── CommentsPlatform.Domain.UnitTests
├── CommentsPlatform.Application.UnitTests
├── CommentsPlatform.Api.UnitTests
├── CommentsPlatform.Infrastructure.UnitTests
├── CommentsPlatform.Infrastructure.IntegrationTests
└── CommentsPlatform.Api.IntegrationTests
```

### Layer Responsibilities

- `Domain` contains entities, aggregates, and business rules.
- `Application` contains use cases, CQRS commands, queries, handlers, and abstractions.
- `Infrastructure` contains persistence and other technical implementations.
- `Api` is the HTTP boundary and application composition root.

## Dependency Direction

Dependencies point inward:

```text
Api ──────────────→ Application ──────────────→ Domain
 │                       ↑
 └──→ Infrastructure ────┘
            │
            └────────────────────────────────→ Domain
```

The Domain layer does not depend on any other project.

The Application layer does not depend on API or Infrastructure implementations.

## Project Status

The project is currently under development.

### Completed

- initial .NET solution structure;
- Clean Architecture project boundaries;
- project references and dependency direction;
- Entity Framework Core and SQL Server foundation;
- Comment domain aggregate;
- Attachment domain entity;
- domain unit tests;
- initial CQRS comment creation use case;
- application unit tests;
- Entity Framework Core persistence mappings;
- local SQL Server Docker Compose configuration;
- backend unit and integration test projects;
- SQL Server Testcontainers integration testing.

### Planned

- initial database migration verification;
- repository implementations;
- API endpoints;
- Angular frontend;
- CI and SonarCloud analysis.

## Testing

The backend uses xUnit for unit and integration tests.

Run the complete test suite:

```bash
dotnet test CommentsPlatform.slnx
```

Generate code coverage in Cobertura format:

```bash
dotnet test CommentsPlatform.slnx \
  --settings coverage.runsettings \
  --collect:"XPlat Code Coverage"
```

Integration tests require Docker because they run Microsoft SQL Server through Testcontainers.

Detailed test project responsibilities, conventions, database isolation rules, and commands are documented in [tests/README.md](tests/README.md).

## Local Backend Secrets

The API requires a SQL Server connection string and a Cloudflare Turnstile secret at startup.

Sensitive values must not be stored in `appsettings.json` or committed to the repository. For local development, use ASP.NET Core User Secrets.

The API project is already initialized with a `UserSecretsId`.

Configure the local SQL Server connection string:

```bash
dotnet user-secrets set \
  "ConnectionStrings:DefaultConnection" \
  "Server=localhost,1435;Database=CommentsPlatform;User Id=sa;Password=<LOCAL_PASSWORD>;TrustServerCertificate=True" \
  --project src/CommentsPlatform.Api/CommentsPlatform.Api.csproj
```

Replace `<LOCAL_PASSWORD>` with the `MSSQL_SA_PASSWORD` value from the local ignored `.env` file.

Configure the Cloudflare Turnstile secret:

```bash
dotnet user-secrets set \
  "CloudflareTurnstile:SecretKey" \
  "<LOCAL_OR_TEST_SECRET>" \
  --project src/CommentsPlatform.Api/CommentsPlatform.Api.csproj
```

Use a provider-approved test secret for local development. Never use or commit a production secret.

List the configured secrets:

```bash
dotnet user-secrets list \
  --project src/CommentsPlatform.Api/CommentsPlatform.Api.csproj
```

> **Warning:** This command prints secret values to the terminal. Do not copy its output into logs, issues, pull requests, or screenshots.

Remove one configured secret:

```bash
dotnet user-secrets remove \
  "CloudflareTurnstile:SecretKey" \
  --project src/CommentsPlatform.Api/CommentsPlatform.Api.csproj
```

Clear all local secrets for the API project:

```bash
dotnet user-secrets clear \
  --project src/CommentsPlatform.Api/CommentsPlatform.Api.csproj
```

User Secrets are stored outside the repository and are loaded automatically when the API runs in the `Development` environment.

Environment variables remain supported for CI, containers, and other environments:

```text
ConnectionStrings__DefaultConnection
CloudflareTurnstile__SecretKey
```

Start the API after configuring the secrets:

```bash
dotnet run \
  --project src/CommentsPlatform.Api/CommentsPlatform.Api.csproj
```

## CAPTCHA Configuration

Comment creation is protected by Cloudflare Turnstile.

The backend receives a provider-neutral CAPTCHA token from the client and validates it through the Application `ICaptchaValidator` abstraction. The Cloudflare-specific HTTP implementation is located in the Infrastructure layer.

The following configuration keys are supported:

| Key | Required | Description |
|---|---|---|
| `CloudflareTurnstile__SecretKey` | Yes | Private Cloudflare Turnstile server-side secret |
| `CloudflareTurnstile__VerificationUrl` | Yes | Cloudflare token verification endpoint |
| `CloudflareTurnstile__ExpectedHostname` | No | Expected hostname returned by Cloudflare |
| `CloudflareTurnstile__ExpectedAction` | No | Expected CAPTCHA action |
| `CloudflareTurnstile__TimeoutSeconds` | Yes | Maximum provider response time in seconds |

Configure the secret through ASP.NET Core User Secrets as described in [Local Backend Secrets](#local-backend-secrets).

Environment variables may be used outside local development. Real secret values must not be committed to the repository.

The default verification URL and non-sensitive settings are defined in `src/CommentsPlatform.Api/appsettings.json`.

API integration tests replace the real `ICaptchaValidator` implementation with a deterministic fake. Tests never call the external Cloudflare service.

The client-side CAPTCHA widget will be implemented separately as part of the frontend scope.

## Local SQL Server

Docker with Docker Compose support is required to run SQL Server locally.

### Configuration

Create a local environment file from the provided template:

```bash
cp .env.example .env
```

Open `.env` and provide a strong local password for `MSSQL_SA_PASSWORD`.

The `.env` file contains local secrets and must not be committed to Git.

Available environment variables:

| Variable | Description |
|---|---|
| `MSSQL_SA_PASSWORD` | Local password for the SQL Server `sa` account |
| `MSSQL_DB` | Name of the application database |
| `SQLSERVER_HOST_PORT` | SQL Server port exposed on the host machine |
| `MSSQL_PID` | SQL Server edition used by the container |

### Starting SQL Server

Start the SQL Server container:

```bash
docker compose up -d sqlserver
```

Check the container status:

```bash
docker compose ps
```

SQL Server is ready to accept connections when the container status is `healthy`.

### Connection Settings

The default local connection settings are:

- server: `localhost`;
- port: `1435`;
- database: `CommentsPlatform`;
- user: `sa`;
- password: the value of `MSSQL_SA_PASSWORD` from `.env`.

Example connection string:

```text
Server=localhost,1435;Database=CommentsPlatform;User Id=sa;Password=<MSSQL_SA_PASSWORD>;TrustServerCertificate=True;
```

Replace `<MSSQL_SA_PASSWORD>` with the local password from `.env`. Never commit the resulting connection string if it contains a real password.

Configure this connection string through ASP.NET Core User Secrets as described in [Local Backend Secrets](#local-backend-secrets).

### Stopping SQL Server

Stop and remove the container while preserving database data:

```bash
docker compose down
```

Stop the container and permanently remove its local database volume:

```bash
docker compose down -v
```

> **Warning:** `docker compose down -v` permanently deletes the local SQL Server data stored in the Docker volume.

## Development Workflow

The project uses the following Git workflow:

```text
feature branch → dev → main
```

- `main` contains stable code;
- `dev` contains integrated development changes;
- feature branches are created from `dev`;
- pull requests are opened from feature branches into `dev`.

## Continuous Integration

The repository uses GitHub Actions to validate backend changes.

The backend CI workflow runs automatically:

- for pull requests targeting the `dev` branch;
- for pushes to the `dev` branch.

The workflow performs the following checks:

- restores .NET dependencies;
- builds the complete solution in the `Release` configuration;
- runs Domain unit tests;
- runs Application unit tests;
- runs API unit tests;
- runs Infrastructure integration tests;
- runs API integration tests.

Integration tests use SQL Server Testcontainers and do not depend on the local `docker-compose.yml` file or local `.env` configuration.

When tests fail, their TRX result files are uploaded as GitHub Actions artifacts for troubleshooting.

The workflow uses the .NET SDK version configured in `global.json`.

## SonarCloud Analysis

The backend CI workflow performs SonarQube Cloud analysis for:

- pull requests targeting the `dev` branch;
- pushes to the `dev` branch.

The analysis covers the backend source and relevant test projects. It reports:

- bugs;
- vulnerabilities;
- security hotspots;
- code smells;
- duplicated code;
- maintainability issues;
- unit test results;
- code coverage.

Generated files, Entity Framework Core migrations, build output, and temporary test results are excluded where appropriate.

Unit test coverage is generated in the OpenCover format and imported into SonarQube Cloud.

### Configuration

The SonarQube Cloud project uses:

```text
Organization key: emil720a1
Project key: emil720a1_CommentsPlatform
```

GitHub Actions requires the following repository secret:

```text
SONAR_TOKEN
```

The token must be stored in GitHub repository secrets and must never be committed to the repository or written directly into the workflow.

Automatic analysis must remain disabled because the repository uses CI-based analysis through GitHub Actions.

### Quality Gate

The project uses the built-in `Sonar way` Quality Gate.

The current SonarQube Cloud plan does not support custom Quality Gates, so the built-in gate is used with its standard conditions for new code:

- no new issues are introduced;
- reliability rating is `A`;
- security rating is `A`;
- maintainability rating is `A`;
- all new Security Hotspots are reviewed;
- coverage on new code is at least 80%;
- duplicated lines on new code do not exceed 3%.

SonarQube Cloud ignores the coverage and duplication conditions until a change contains at least 20 new lines.

The project uses a 30-day new code definition for long-lived branch analysis. Pull request analysis evaluates code changed relative to the target branch.

The CI workflow waits up to 300 seconds for the Quality Gate result. A failed or unavailable Quality Gate causes the `Build, test, and analyze` job to fail.

The `dev` branch is protected by an active GitHub ruleset. Pull requests targeting `dev` require the following checks to pass:

- `Build, test, and analyze`;
- `Infrastructure integration tests`;
- `API integration tests`;
- `SonarCloud Code Analysis`.

The protected branch must be up to date before merging. Direct changes, force pushes, branch deletion, and merging with unresolved conversations are not allowed.

### Local Verification

Restore the repository's local .NET tools:

```bash
dotnet tool restore
```

Build the solution:

```bash
dotnet build CommentsPlatform.slnx --configuration Release
```

Generate a local OpenCover report:

```bash
dotnet test \
  tests/CommentsPlatform.Domain.UnitTests/CommentsPlatform.Domain.UnitTests.csproj \
  --configuration Release \
  --no-build \
  --collect "XPlat Code Coverage" \
  --settings coverage.runsettings \
  --results-directory TestResults/Unit
```

Generated coverage reports are stored under `TestResults` and must not be committed.

Uploading a local analysis to SonarQube Cloud requires a valid personal token. The token must be supplied through an environment variable and must not be stored in source-controlled files.

Quality gate configuration and pull request blocking rules are handled in a separate task.
