# MyApp

The project was generated using the [Clean.Architecture.Solution.Template](https://github.com/jasontaylordev/CleanArchitecture) version 10.0.0-preview.

## Build

Run `dotnet build -tl` to build the solution.

## Run

To run the web application:

```bash
cd .\src\Web\
dotnet watch run
```

Navigate to https://localhost:5001. The application will automatically reload if you change any of the source files.

## Code Styles & Formatting

The template includes [EditorConfig](https://editorconfig.org/) support to help maintain consistent coding styles for multiple developers working on the same project across various editors and IDEs. The **.editorconfig** file defines the coding styles applicable to this solution.

## Code Scaffolding

The template includes support to scaffold new commands and queries.

Start in the `.\src\Application\` folder.

Create a new command:

```
dotnet new ca-usecase --name CreateTodoList --feature-name TodoLists --usecase-type command --return-type int
```

Create a new query:

```
dotnet new ca-usecase -n GetTodos -fn TodoLists -ut query -rt TodosVm
```

If you encounter the error _"No templates or subcommands found matching: 'ca-usecase'."_, install the template and try again:

```bash
dotnet new install Clean.Architecture.Solution.Template::10.0.0-preview
```

## Test

The solution contains unit, integration, and functional tests.

To run the tests:

```bash
dotnet test
```

## Help

To learn more about the template go to the [project website](https://github.com/jasontaylordev/CleanArchitecture). Here you can find additional guidance, request new features, report a bug, and discuss the template with other users.

## Core Framework

- .NET 10.0 - Latest version of the .NET framework
- ASP.NET Core - Web framework for building APIs and web applications
- Clean Architecture - Following Domain-Driven Design (DDD) principles

## Architecture Pattern

- Clean Architecture with 3-layered structure:
  - Domain - Core business logic and entities
  - Application - Use cases, CQRS (Commands/Queries), and application services
  - Infrastructure - External implementations (data access, storage, etc.)
  - Web - API presentation layer

## Endpoint Rules

Endpoints must follow the single-line mediator convention. Example (C#):

sample:

```cs
app.MapPost("/users", async (CreateUserCommand command, ISender sender)
=> await sender.Send(command));
```

- No logic or conditionals in endpoints.
- Endpoints only map to commands or queries.
- Return results directly from await sender.Send().

## API Responses

- Use `Result<T>` (`src/Application/Common/Models/Result.cs`) for all responses.

- For paginated data:
  - Use `PaginatedList<T>` (`src/Application/Common/Models/PaginatedList.cs`).
  - Use `.PaginatedListAsync()` extension for pagination queries.

Never return raw entities or unwrapped collections from handlers.

## Exceptions

Use only predefined exception types handled in: `src/Web/Infrastructure/CustomExceptionHandler.cs`
No custom or inline exceptions outside this handler. Always rely on centralized exception mapping for consistency.

sample:

```cs
  throw new NotFoundException(nameof(TodoItem), request.Id.ToString());
```

## current user

you can use IUser and IIdentityService in application.

## Validation Rules

- Validation framework: FluentValidation
- No DataAnnotations
- Every Command and Query must have a corresponding Validator.
- Validators must be placed alongside their respective command/query folders.

## Migrations & Database

### Add Migration

```bash
SkipNSwag=true dotnet ef migrations add "MigrationName" -p src/Infrastructure -s src/Web -o Data/Migrations
```

### Update Database

```bash
dotnet ef database update -p src/Infrastructure -s src/Web
```

- Never modify migration folders manually.
- Always generate migrations from Infrastructure using the above command.

## File Storage

All file operations must use the StorageService (based on MinIO). Never write files manually or use raw I/O in any handler or controller. All uploads, downloads, and deletions go through this service.
