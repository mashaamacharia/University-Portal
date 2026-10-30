
# C# and ASP.NET Core – Developer Notes

## 1. .NET SDK Commands

| Command                    | Description                                    |
| -------------------------- | ---------------------------------------------- |
| `dotnet --version`       | Display installed .NET SDK version             |
| `dotnet --info`          | Display detailed .NET installation information |
| `dotnet --list-sdks`     | List installed SDKs                            |
| `dotnet --list-runtimes` | List installed runtimes                        |

## 2. Project Creation

| Command                                        | Description                      |
| ---------------------------------------------- | -------------------------------- |
| `dotnet new list`                            | List available project templates |
| `dotnet new webapi -n UniversityPortal.API`  | Create ASP.NET Core Web API      |
| `dotnet new console -n MyApp`                | Create console application       |
| `dotnet new classlib -n MyLibrary`           | Create class library             |
| `dotnet new xunit -n UniversityPortal.Tests` | Create xUnit test project        |
| `dotnet sln add path/to/project.csproj`      | Add a project to a solution      |

## 3. Running the Application

| Command                       | Description                              |
| ----------------------------- | ---------------------------------------- |
| `dotnet run`                | Build and run the application            |
| `dotnet watch`              | Run application and monitor code changes |
| `dotnet build`              | Compile the project                      |
| `dotnet clean`              | Remove previous build outputs            |
| `dotnet publish -c Release` | Prepare application for deployment       |
| `Ctrl + C`                  | Stop the running application             |

**Useful:** `dotnet watch` is particularly useful during development because it detects code changes and can restart or reload the application.

## 4. NuGet Package Management

NuGet is the package manager used by .NET.

| Command                                       | Description                  |
| --------------------------------------------- | ---------------------------- |
| `dotnet add package PackageName`            | Install a NuGet package      |
| `dotnet remove package PackageName`         | Remove a package             |
| `dotnet list package`                       | List installed packages      |
| `dotnet restore`                            | Restore project dependencies |
| `dotnet add package Swashbuckle.AspNetCore` | Install Swagger UI support   |

## 5. Working with Solutions and Projects

A solution groups multiple related .NET projects.

| Command                                         | Description                                        |
| ----------------------------------------------- | -------------------------------------------------- |
| `dotnet new sln -n UniversityPortal`          | Create a solution                                  |
| `dotnet sln add Project.csproj`               | Add a project to solution                          |
| `dotnet sln list`                             | List projects in solution                          |
| `dotnet new classlib -n ProjectName`          | Create a class library                             |
| `dotnet reference add path/to/project.csproj` | Add a project reference (from the current project) |

## 6. Testing and Debugging

| Command                            | Description                 |
| ---------------------------------- | --------------------------- |
| `dotnet test`                    | Run automated tests         |
| `dotnet test --verbosity normal` | Run tests with more output  |
| `dotnet build`                   | Check whether code compiles |
| `dotnet run`                     | Run application locally     |

Useful VS Code shortcuts:

| Shortcut             | Description                       |
| -------------------- | --------------------------------- |
| `Ctrl + Shift + P` | Open Command Palette              |
| `Ctrl + Shift + X` | Open Extensions                   |
| `Ctrl + `` `       | Open integrated terminal          |
| `F5`               | Start debugging                   |
| `Shift + F5`       | Stop debugging                    |
| `Ctrl + .`         | Show code actions and quick fixes |

## 7. ASP.NET Core API Testing

When the application is running:

| URL                  | Description                              |
| -------------------- | ---------------------------------------- |
| `/swagger`         | Interactive Swagger UI (when configured) |
| `/openapi/v1.json` | Generated OpenAPI JSON document          |
| `/weatherforecast` | Default template example endpoint        |

HTTP methods:

| Method | Purpose                      |
| ------ | ---------------------------- |
| GET    | Retrieve information         |
| POST   | Create or submit information |
| PUT    | Replace an existing resource |
| PATCH  | Partially update a resource  |
| DELETE | Delete a resource            |

## 8. Common Project Files

| File                             | Purpose                                    |
| -------------------------------- | ------------------------------------------ |
| `Program.cs`                   | Application entry point and configuration  |
| `appsettings.json`             | Application configuration                  |
| `appsettings.Development.json` | Development configuration                  |
| `launchSettings.json`          | Local development launch settings          |
| `.csproj`                      | Project settings and dependencies          |
| `Properties/`                  | Project launch and debugging configuration |

## 9. Useful C# Concepts

| Concept               | Purpose                                   |
| --------------------- | ----------------------------------------- |
| Class                 | Blueprint for creating objects            |
| Interface             | Defines a contract that classes implement |
| Constructor           | Initializes an object                     |
| Dependency Injection  | Supplies required dependencies to classes |
| DTO                   | Defines data transferred between systems  |
| LINQ                  | Queries and transforms collections        |
| Async/Await           | Handles asynchronous operations           |
| Entity Framework Core | ORM for working with databases            |
| Middleware            | Processes HTTP requests and responses     |
| Controller            | Handles incoming API requests             |
| Service               | Contains application/business logic       |
| Repository            | Encapsulates data access logic            |

## 10. Important Development Practices

1. Run `dotnet build` before committing major changes.
2. Use `dotnet watch` during development.
3. Never commit passwords, API keys or connection strings containing secrets.
4. Use DTOs rather than exposing internal database entities directly.
5. Keep controllers focused on handling HTTP requests.
6. Put business logic in services.
7. Validate incoming API requests.
8. Test endpoints using Swagger before connecting the frontend.
9. Use Git to track changes.
10. Never access the Business Central SQL database directly; use its supported APIs.

---

**Project:** University Finance Portal
**Backend:** C# / ASP.NET Core Web API
**Frontend:** Next.js / TypeScript
**ERP Integration:** Microsoft Dynamics 365 Business Central
**API Documentation:** Swagger / OpenAPI
