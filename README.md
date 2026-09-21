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

## Dependency Direction

Dependencies point inward:

```text
Api → Infrastructure → Application → Domain
                 ↘ Application → Domain
The Domain layer does not depend on any other project.
The Application layer does not depend on API or Infrastructure implementations.
Project Status
The project is currently under development.
Completed:
- initial .NET solution structure;
- Clean Architecture project boundaries;
- project references and dependency direction;
- Entity Framework Core and SQL Server foundation;
- Comment domain aggregate;
- Attachment domain entity;
- domain unit tests.
Planned:
- application use cases with CQRS and MediatR;
- API endpoints;
- database mappings and migrations;
- React frontend;
- Docker configuration;
- CI and SonarCloud analysis.
Development Workflow
The project uses the following Git workflow:
feature branch → dev → main
- main contains stable code;
- dev contains integrated development changes;
- feature branches are created from dev;
- pull requests are opened into dev.
