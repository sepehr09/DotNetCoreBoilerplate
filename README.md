# MyApp

The project was generated using the [Clean.Architecture.Solution.Template](https://github.com/jasontaylordev/CleanArchitecture) version 9.0.12.

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
dotnet new install Clean.Architecture.Solution.Template::9.0.12
```

## Test

The solution contains unit, integration, and functional tests.

To run the tests:

```**bash**
dotnet test
```

## Help

To learn more about the template go to the [project website](https://github.com/jasontaylordev/CleanArchitecture). Here you can find additional guidance, request new features, report a bug, and discuss the template with other users.

## Extras

- Dockerized with postgresql and configured to pass `.env` file to appSettings.
- MultiTenancy using finbuckle package.
- FileStorage service using Minio (compatible with S3).
- redis
- configured Distributed caching (using redis)

## Multi tenancy instruction

1. Multi tenancy feature can be enabled/disabled in `app settings` with `IsMultiTenant` bool.
2. For Configure an entity type to be multi-tenant, use [IsMultiTenant()](https://www.finbuckle.com/MultiTenant/Docs/v9.4.0/EFCore#using-the-fluent-api) on EntityTypeBuilder.
