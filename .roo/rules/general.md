# back

using clean arch and ddd with template https://github.com/jasontaylordev/CleanArchitecture. documentation about boilerpate is available in context7 mcp.

## Core Framework

- .NET 9.0 - Latest version of the .NET framework
- ASP.NET Core - Web framework for building APIs and web applications
- Clean Architecture - Following Domain-Driven Design (DDD) principles

## Architecture Pattern

- Clean Architecture with 3-layered structure:
  - Domain - Core business logic and entities
  - Application - Use cases, CQRS (Commands/Queries), and application services
  - Infrastructure - External implementations (data access, storage, etc.)
  - Web - API presentation layer

## Exceptions

anytime wanna throw exception, just use one of defined exceptions here:

`src/Web/Infrastructure/CustomExceptionHandler.cs`

## get current user

you can use ICurrentUserService and IIdentityService in application.

## multi tenancy

we've used Finbuckle for multi tenancy. documentation is available in context7 mcp.

## file storage

we have storage service (that uses minio) for files.
