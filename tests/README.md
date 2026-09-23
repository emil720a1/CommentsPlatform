# Backend Testing

This directory contains the automated backend tests for CommentsPlatform.

## Test Projects

```text
tests/
├── CommentsPlatform.Domain.UnitTests
├── CommentsPlatform.Application.UnitTests
├── CommentsPlatform.Api.UnitTests
├── CommentsPlatform.Infrastructure.IntegrationTests
└── CommentsPlatform.Api.IntegrationTests
```

### Domain Unit Tests

`CommentsPlatform.Domain.UnitTests` verifies domain entities, aggregate behavior, invariants, and business rules.

These tests must:

- reference only the Domain project;
- avoid infrastructure and framework dependencies;
- verify observable domain behavior;
- avoid mocking domain entities.

### Application Unit Tests

`CommentsPlatform.Application.UnitTests` verifies commands, queries, handlers, validators, and use-case orchestration.

These tests must:

- isolate external dependencies through Application abstractions;
- use mocks only for dependencies outside the tested use case;
- verify returned results and important dependency interactions;
- avoid depending on Infrastructure implementations.

### API Unit Tests

`CommentsPlatform.Api.UnitTests` verifies controller behavior and HTTP mapping.

These tests must:

- mock `ISender`;
- verify the request sent to MediatR;
- verify HTTP status codes and response contracts;
- avoid testing Application business rules again.

### Infrastructure Integration Tests

`CommentsPlatform.Infrastructure.IntegrationTests` verifies persistence implementations against Microsoft SQL Server.

These tests must:

- use SQL Server through Testcontainers;
- apply the real EF Core migrations;
- verify database-specific behavior;
- avoid EF Core InMemory for relational query testing;
- isolate test data between scenarios.

### API Integration Tests

`CommentsPlatform.Api.IntegrationTests` verifies the application through its HTTP boundary.

These tests use:

- `WebApplicationFactory`;
- the real ASP.NET Core request pipeline;
- EF Core migrations;
- SQL Server through Testcontainers.

They must not depend on a manually started local database, `.env`, or `docker-compose.yml`.

## Test Framework and Libraries

The backend test suite uses:

- xUnit as the test framework;
- Moq for test doubles in unit tests;
- Testcontainers for disposable SQL Server instances;
- coverlet.collector for code coverage collection.

## Naming Convention

Test methods use the following format:

```text
Method_Scenario_ExpectedResult
```

Examples:

```text
Create_WithMissingEmail_ThrowsArgumentException
Handle_WithValidQuery_ReturnsPaginatedComments
GetComments_WithInvalidQueryParameters_ReturnsBadRequest
```

A test name must describe behavior rather than implementation details.

## Test Structure

Tests follow the Arrange–Act–Assert structure:

1. Arrange the required inputs and dependencies.
2. Act by executing the behavior under test.
3. Assert the observable result.

Explicit `Arrange`, `Act`, and `Assert` comments are optional when the structure is already clear.

Each test should verify one meaningful behavior. Multiple assertions are acceptable when they describe one result.

## Mocking Rules

Mocks are used only at architectural boundaries.

Use mocks to:

- isolate Application abstractions;
- isolate MediatR in controller unit tests;
- verify important collaboration with external dependencies.

Do not mock:

- domain entities;
- value objects;
- simple data models;
- EF Core queries in persistence integration tests.

Tests should verify behavior, not private methods or internal implementation details.

## Integration Test Database

Integration tests use disposable Microsoft SQL Server containers.

The test database approach follows these rules:

- each test run creates an isolated SQL Server environment;
- EF Core migrations prepare the schema;
- tests explicitly prepare their own data;
- tests must not depend on execution order;
- test data must be removed or isolated between scenarios;
- deterministic values are preferred over arbitrary delays;
- containers are disposed after the test run.

Docker must be available when running integration tests.

## Running Tests

Run the complete backend test suite:

```bash
dotnet test CommentsPlatform.slnx
```

Run Domain unit tests:

```bash
dotnet test tests/CommentsPlatform.Domain.UnitTests/CommentsPlatform.Domain.UnitTests.csproj
```

Run Application unit tests:

```bash
dotnet test tests/CommentsPlatform.Application.UnitTests/CommentsPlatform.Application.UnitTests.csproj
```

Run API unit tests:

```bash
dotnet test tests/CommentsPlatform.Api.UnitTests/CommentsPlatform.Api.UnitTests.csproj
```

Run Infrastructure integration tests:

```bash
dotnet test tests/CommentsPlatform.Infrastructure.IntegrationTests/CommentsPlatform.Infrastructure.IntegrationTests.csproj
```

Run API integration tests:

```bash
dotnet test tests/CommentsPlatform.Api.IntegrationTests/CommentsPlatform.Api.IntegrationTests.csproj
```

## Code Coverage

Generate backend coverage in Cobertura format:

```bash
dotnet test CommentsPlatform.slnx \
  --settings coverage.runsettings \
  --collect:"XPlat Code Coverage"
```

Coverage reports are generated under:

```text
TestResults/<run-id>/coverage.cobertura.xml
```

The `TestResults` directory is ignored by Git.

Coverage rules:

- production projects are included;
- test assemblies are excluded;
- EF Core migrations are excluded;
- generated source files are excluded;
- coverage supports code review but does not replace behavior-focused tests;
- no global percentage threshold is enforced by this task.
