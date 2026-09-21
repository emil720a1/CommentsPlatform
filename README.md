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
