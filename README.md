# CommentsPlatform

A SPA application for creating, viewing and discussing comments with support for nested replies and file attachments.

The project name is currently provisional and may be changed later.

## Overview

CommentsPlatform is designed as a full-stack application based on Clean Architecture principles.

The backend is developed first as an ASP.NET Core Web API. The frontend will be implemented later using React.

## Technologies

- ASP.NET Core Web API
- Clean Architecture
- CQRS and MediatR
- Entity Framework Core
- Microsoft SQL Server
- React
- Docker
- xUnit
- SonarCloud

## Planned Features

- create comments;
- reply to existing comments;
- display nested replies;
- sort comments;
- paginate comments;
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
```

### Dependency Direction

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
- local SQL Server Docker Compose configuration.

### Planned

- initial database migration verification;
- repository implementations;
- API endpoints;
- integration tests;
- React frontend;
- CI and SonarCloud analysis.

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

Replace `<MSSQL_SA_PASSWORD>` with the local password from `.env`. Never commit a connection string containing a real password.

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
