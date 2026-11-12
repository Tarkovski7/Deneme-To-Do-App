# AGENTS.md

Guidance for coding agents (Claude Code, Codex, Cursor and the like) working in
this repository. It was delivered by Karqen together with the code. Any stack
sections that follow are the same conventions Karqen's own agents followed when
they wrote it, so changes made with them stay consistent with what is already
here.
This file belongs to you — edit it freely; Karqen never overwrites an existing
`AGENTS.md`.

## Working rules

- Keep the existing architecture and folder layout; extend it rather than
  reorganise it. Where this repository's existing code already differs from a
  stack section, the existing code wins.
- Match the conventions already in the code: naming, error handling, API
  shapes, test style.
- After every change run the project's build and tests; a change is done when
  they pass. A stack section, where present, names the exact commands.
- Configuration comes from environment variables; never commit secrets. When a
  new variable is needed, add it to `.env.example` if the project has one.
- Where a stack section mentions "the platform" or its build sandbox, that is
  the environment Karqen validated this code in; its pinned versions are why the
  sections pin theirs. Keep to them unless you deliberately move the whole
  project on.
- `RUN_LOCAL.md`, when present, explains how to start the whole application
  locally.
- `docs/karqen/runs/` holds one log per Karqen run (newest last by name): the
  task, the decisions taken and the follow-ups flagged. Read the latest ones
  before changing code — they explain why the code is the way it is.
- Keep changes small and say in the commit message what changed and why.

---

# .NET / C# — Technology Golden Path

## Target: .NET 10 for a new project

Every new project targets **`net10.0`** (the current LTS) unless the customer
named a version: a request for ".NET 8" targets `net8.0`, with the 8.x line of
the `Microsoft.*` packages. C# 14 features are fine on `net10.0`. The platform's
build sandbox carries the .NET SDKs 6, 8, 9 and 10 side by side, so `net6.0`
through `net10.0` targets all build and test there; the repository's own
`global.json` and `TargetFramework` pick the SDK.

A repository that already targets another framework (e.g. `net9.0`) keeps its
target: do not retarget it. Two things cannot build in the sandbox, and the
answer to both is the same — change nothing to work around it, and say in your
notes exactly why the build gate fails, so the customer sees the reason instead
of a silent change:
- a .NET Framework target (`net48` and older) — it needs Windows;
- a `global.json` that pins an SDK the sandbox does not carry (e.g.
  `"version": "8.0.100"` without a `rollForward`). Every command then fails with
  "A compatible .NET SDK was not found". Do not edit `global.json`.

## The fixed layout

One solution, four source projects and one test project. In a fullstack delivery
all of it lives under `backend/` (the fullstack layout rule); a backend-only
delivery puts the same tree at the root. With `MyApp` standing for the product
name:

- `backend/MyApp.sln` — references EVERY project below.
- `backend/src/MyApp.Domain/` — entities, enums, domain rules; no packages.
- `backend/src/MyApp.Application/` — services, DTOs, repository ports,
  `DependencyInjection.cs`.
- `backend/src/MyApp.Infrastructure/` — `Persistence/AppDbContext.cs`, entity
  configurations, repositories, `DependencyInjection.cs`. When the task asks
  for no database it holds only `DependencyInjection.cs` and in-memory services
  — no `Persistence/` (see `frameworks/aspnet_core.md`, "No persistence").
- `backend/src/MyApp.Api/` — `Program.cs`, `Controllers/`; the only startup
  project.
- `backend/tests/MyApp.Tests/` — xUnit: API integration tests + application unit
  tests.

- **References point inward only:** Api → Application + Infrastructure;
  Infrastructure → Application + Domain; Application → Domain; Domain → nothing.
  Tests → Api (+ whatever they unit-test). A reference the other way is a
  design error, not a shortcut.
- **One name everywhere** (`MyApp` stands for the product name in PascalCase).
  When revising after a failed build, keep the same paths and names and
  overwrite those files — never start a second, differently-named tree.
- Each layer registers its own services through a `DependencyInjection.cs`
  extension (`AddApplication()`, `AddInfrastructure(IConfiguration)`). A class
  library's implicit usings are `System.*` only, so that file starts with
  `using Microsoft.Extensions.Configuration;` and
  `using Microsoft.Extensions.DependencyInjection;`.

## The `.csproj` of each project

Every project: `<TargetFramework>net10.0</TargetFramework>`,
`<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`. Then:

- **Domain** — `Sdk="Microsoft.NET.Sdk"`, no references at all.
- **Application** — `Sdk="Microsoft.NET.Sdk"` + `<FrameworkReference
  Include="Microsoft.AspNetCore.App" />` + a `<ProjectReference>` to Domain. The
  framework reference is what gives a class library `IServiceCollection`,
  `IConfiguration` and DataAnnotations — without it `DependencyInjection.cs`
  fails with CS0246, and it needs no version pin. With it in place, a CS0246 on
  those names is a missing `using` in the file, not a missing reference.
- **Infrastructure** — same `FrameworkReference`, the EF Core + Npgsql packages
  from the table only when the app persists data (none when the task asks for
  no database), `<ProjectReference>`s to Application and Domain.
- **Api** — `Sdk="Microsoft.NET.Sdk.Web"` (the web SDK brings ASP.NET Core
  itself) + `<ProjectReference>`s to Application and Infrastructure.
- **Tests** — `Sdk="Microsoft.NET.Sdk"`, `<IsPackable>false</IsPackable>`, the
  test packages from the table — `Microsoft.EntityFrameworkCore.InMemory` only
  when the app persists data (with no database the test project has five, and
  no EF package at all) — a `<ProjectReference>` to Api, and
  `<ItemGroup><Using Include="Xunit" /></ItemGroup>` so `[Fact]` needs no using.

## C# conventions

- `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`,
  file-scoped namespaces matching the folder (`MyApp.Application.Orders`).
- DTOs are `record`s; entities are classes with private setters and a factory or
  constructor that enforces their rules.
- `async`/`await` end to end for I/O, returning `Task`/`Task<T>`; pass a
  `CancellationToken` through every public async method; never `.Result` or
  `.Wait()`.
- Constructor injection of interfaces; no service locator, no `static` state.
- Configuration through `IConfiguration`/options; secrets only from the
  environment — never in `appsettings.json` or source.
- **No MediatR, no AutoMapper**: plain services and hand-written mapping
  (`record` → `record`). They add packages, licences and failure modes a
  delivery does not need.
- Timestamps are UTC (`DateTime.UtcNow`); money is `decimal`.

## Package versions — the known-good table

Use exactly these (all verified to exist on NuGet and to restore together;
the EF packages are pinned to the version the PostgreSQL provider itself
depends on, so ORM and provider never disagree):

| Package | Version | Project |
|---|---|---|
| `Microsoft.EntityFrameworkCore` | `10.0.12` | Infrastructure |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | `10.0.3` | Infrastructure |
| `Microsoft.EntityFrameworkCore.InMemory` | `10.0.12` | Tests only (only when the app persists data) |
| `Microsoft.AspNetCore.Mvc.Testing` | `10.0.12` | Tests only |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | `10.0.12` | Api (only if auth is asked for) |
| `Microsoft.AspNetCore.OpenApi` | `10.0.12` | Api (only if an OpenAPI document is asked for) |
| `Swashbuckle.AspNetCore` | `10.2.3` | Api (only if a Swagger UI page is asked for) |
| `xunit` | `2.9.3` | Tests |
| `xunit.runner.visualstudio` | `3.1.4` | Tests |
| `Microsoft.NET.Test.Sdk` | `17.14.1` | Tests |
| `coverlet.collector` | `6.0.4` | Tests |

- The Npgsql provider has its own patch numbers: `10.0.3` requires EF Core
  `10.0.4` or newer, which `10.0.12` satisfies. Keep EF Core and InMemory on the
  same version and all three on the `10.0` line.
- The test packages are the set the .NET 10 SDK's own `xunit` template uses.
- A package not in the table: a released (GA) version on the `10.x` line for any
  `Microsoft.*` package — never a `-preview`/`-rc`, never a version you have not
  seen exist. `coverlet.collector` is what makes
  `dotnet test --collect "XPlat Code Coverage"` report coverage; it belongs in
  the test project only.
- Most-forgotten references: `UseNpgsql` → the Npgsql provider;
  `UseInMemoryDatabase` → the InMemory provider; `ToTable`/`HasColumnType` →
  arrive with the relational provider; `AddJwtBearer` → JwtBearer; Swagger calls
  → Swashbuckle. A build error ending in *"are you missing a using directive or
  an assembly reference?"* is a missing `<PackageReference>`, not a code bug.

## Tests

- xUnit, one test project, arrange/act/assert, test names that state the
  behaviour (`Create_WithEmptyName_Returns400`).
- API behaviour through `WebApplicationFactory<Program>` with the in-memory
  database (see `frameworks/aspnet_core.md`); application services unit-tested
  with hand-written fakes of their ports — mock the edges, never the domain.
- Tests must not need a network, a running database or an environment variable.

## Validation — the one command set

From the solution folder (`backend/` in a fullstack delivery): `dotnet build`, then
`dotnet test`. Both must pass before the work is proposed. The platform runs
exactly these in its .NET build sandbox — a warning-free build is the goal, an
error-free one is the gate.

## When the task is unclear

Build the smallest thing that satisfies the stated requirement, keep the layout
above, and state the assumption in the output's `notes` — never invent
entities, endpoints, roles or integrations the task did not ask for.

---

# ASP.NET Core — Framework Golden Path

The ONE way a Karqen backend is built on ASP.NET Core. In a new codebase follow it
exactly; where the task is silent, this document decides. It is written against
what the platform's build sandbox, deploy package and frontend contract actually
require — every rule below exists because breaking it failed a real delivery.
In an existing codebase its own choices (versions, layout, libraries) win over
this document, which then decides only what the repository has not decided.

## The shape of the API

- **Controllers, not minimal APIs**, for resource endpoints: `[ApiController]` +
  an explicit, **plural** route — `[Route("api/orders")]`. Every HTTP route lives
  under `/api` (the frontend and the deploy proxy only forward `/api/...`).
  Never rely on `[Route("api/[controller]")]`: a `OrderController` yields the
  singular `/api/order` and the frontend's call misses.
- **Thin controllers.** A controller maps HTTP to one Application-service call
  and back; no EF Core, no business rules, no `DbContext` in a controller.
- **Status codes, one convention:** `GET` → `200` with the body, or `404` via
  `NotFound()`; `POST` → `201` via `CreatedAtAction(...)` with the created
  resource; `PUT`/`PATCH` → `200` **with the updated resource**; `DELETE` →
  `204 NoContent()`. The frontend relies on exactly this (a `204` has no body).
- **Validation** at the boundary with DataAnnotations on the request DTOs
  (`[Required]`, `[StringLength]`, `[Range]`). `[ApiController]` returns the
  RFC 7807 `400` automatically — do not hand-roll a validation response.
- **JSON** is the ASP.NET default (camelCase). Never rename properties per field;
  the frontend types mirror the DTO names in camelCase. Serialize enums as
  strings: `AddControllers().AddJsonOptions(o =>
  o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))`.
- **Errors:** `builder.Services.AddProblemDetails()` + `app.UseExceptionHandler()`
  — an unhandled exception becomes a problem-details `500` with no stack trace.
  Domain "not found"/"conflict" is expressed with return values the controller
  maps to `404`/`409`, not by throwing.

## `Program.cs` — the canonical skeleton

Top-level statements (usings for `System.Text.Json.Serialization` and the
Application/Infrastructure namespaces at the top), in exactly this order:

1. `var builder = WebApplication.CreateBuilder(args);`
2. `builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));`
3. `builder.Services.AddProblemDetails();`
4. `builder.Services.AddHealthChecks();`
5. `builder.Services.AddApplication();`
6. `builder.Services.AddInfrastructure(builder.Configuration);`
7. `var app = builder.Build();`
8. `app.UseExceptionHandler();`
9. `app.MapControllers();`
10. `app.MapHealthChecks("/api/health");`
11. `await app.Services.EnsureDatabaseCreatedAsync();` — **only when the app
    persists data**; with no persistence (see "No persistence" below) skip it.
12. `app.Run();`
13. and, as the file's LAST line, `public partial class Program;` — it makes
    `Program` visible to `WebApplicationFactory<Program>` in the tests. Nothing
    else follows the statements.

- **`/api/health`** is mandatory (`AddHealthChecks` is built in — no package). The
  deploy package and the platform's smoke checks probe it.
- **No `UseHttpsRedirection()`**: TLS ends at the reverse proxy; inside the
  container there is no HTTPS port and the redirect breaks every request.
- **Never set ports in code** (`UseUrls`, `ConfigureKestrel` listen calls, a
  `launchSettings.json` the container relies on): the container sets
  `ASPNETCORE_URLS`; a hard-coded port makes the app unreachable.
- **No CORS** unless the task names a cross-origin client: the frontend reaches
  the API same-origin through `/api` (dev proxy locally, reverse proxy deployed).
- `AddApplication()` / `AddInfrastructure(configuration)` are extension methods
  that live in their own layers (below) — the composition root only calls them.

## Data access — EF Core, one `DbContext`, schema at startup

### No persistence — when the task asks for no database

When the task says there is no database, or nothing it describes has to survive
a restart, the app has **no persistence layer at all** — this whole section
does not apply:

- no `Persistence/` folder, no `AppDbContext`, no `IEntityTypeConfiguration`;
- no EF Core, `Npgsql.EntityFrameworkCore.PostgreSQL` or
  `Microsoft.EntityFrameworkCore.InMemory` package in any project — the test
  project included: with no database there is nothing for an in-memory
  provider to stand in for;
- no `AddDbContext`, no `DefaultConnection`, no `EnsureDatabaseCreatedAsync`
  helper — and `Program.cs` skips step 11: the call is not written at all, not
  kept alive by an empty or no-op `EnsureDatabaseCreatedAsync` extension;
- state the API must hold while it runs lives in an in-memory implementation of
  the Application port (in Infrastructure, registered as a singleton in
  `AddInfrastructure`); a stateless API has no repository at all.

Write none of it "for later": a half-written persistence layer (a `using` of a
`Persistence` namespace, an `AppDbContext` nobody declares) does not build.

### With persistence

- **One** `AppDbContext` in `Infrastructure/Persistence/`. Entity configuration
  with `IEntityTypeConfiguration<T>` classes in the same folder (Domain entities
  carry no EF attributes).
- **The connection string is named `DefaultConnection`** — read it with
  `configuration.GetConnectionString("DefaultConnection")`. The deploy package
  sets `ConnectionStrings__DefaultConnection`; any other name means no database.
- **PostgreSQL** via `Npgsql.EntityFrameworkCore.PostgreSQL` (the platform's
  database). In `AddInfrastructure`:
  `services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connectionString))`.
- **Create the schema at startup with `EnsureCreated`**, not `Migrate()`, unless
  you emit a *complete* migration set (see pitfalls). Infrastructure's
  `DependencyInjection.cs` carries the helper `Program.cs` calls:
  `public static async Task EnsureDatabaseCreatedAsync(this IServiceProvider services)`
  — create a scope (`services.CreateScope()`), resolve `AppDbContext` from
  `scope.ServiceProvider`, and `await db.Database.EnsureCreatedAsync()`. A scope is
  required: `AppDbContext` is scoped and the root provider refuses to hand it out.

- Application defines the persistence port (`IOrderRepository` with async,
  `CancellationToken`-taking methods); Infrastructure implements it with the
  `DbContext`. Queries that only read use `AsNoTracking()`.
- IDs: `Guid` generated in the domain (`Guid.NewGuid()` in the factory or
  constructor), timestamps `DateTime.UtcNow` — never local time.

## Auth (only when the task asks for it)

- JWT bearer via `Microsoft.AspNetCore.Authentication.JwtBearer`; the signing key
  comes from configuration/environment (`Jwt__Key`), never from source. Protect
  endpoints with `[Authorize]`; keep `/api/health` anonymous.
- Passwords are hashed with `Microsoft.AspNetCore.Identity`'s `PasswordHasher<T>`
  (in the shared framework — no package); never store or log plain text.
- No auth requirement in the task → no auth code. Do not add login screens or
  user tables nobody asked for.

## Testing the API

- **No persistence:** `ApiFactory` derives from `WebApplicationFactory<Program>`
  and overrides nothing (there is no database to replace); the test project
  references no EF Core package. Everything below about `AppDbContext` applies
  only when the app persists data.
- Integration tests use `WebApplicationFactory<Program>` (package
  `Microsoft.AspNetCore.Mvc.Testing`) through one `ApiFactory` class that derives
  from it and overrides `ConfigureWebHost`: inside
  `builder.ConfigureServices(...)`, call
  `services.RemoveAll<DbContextOptions<AppDbContext>>()`,
  `services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>()` and then
  `services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("tests"))`.
  Tests therefore never need a running PostgreSQL; test classes take the factory
  through `IClassFixture<ApiFactory>` and call `factory.CreateClient()`.
- `RemoveAll` is in `Microsoft.Extensions.DependencyInjection.Extensions`;
  `IDbContextOptionsConfiguration<>` is in `Microsoft.EntityFrameworkCore.Infrastructure`. The
  in-memory provider needs `Microsoft.EntityFrameworkCore.InMemory` in the TEST
  project only. `EnsureCreatedAsync` works on it unchanged.
- One test class per controller: happy path per endpoint, `404` for an unknown
  id, `400` for an invalid body. Assert status codes and the JSON body shape the
  frontend relies on.

## Must compile — pitfalls measured on real deliveries

- **`Program.cs` shape:** top-level statements only (no `namespace`, no `Main`)
  and no type declarations after them except `public partial class Program;`.
  Mixing top-level statements with a `Program` class/`Main` fails with CS8803,
  CS0116, CS8956, CS8954. A file-scoped namespace, when used, is the first
  declaration and there is at most one per file.
- **Two database providers at test start:** since EF Core 9, `AddDbContext` also
  registers an `IDbContextOptionsConfiguration<AppDbContext>`. A factory that
  removes only `DbContextOptions<AppDbContext>` keeps the Npgsql configuration,
  and every test fails with "Services for database providers
  'Npgsql.EntityFrameworkCore.PostgreSQL', 'Microsoft.EntityFrameworkCore.InMemory'
  have been registered". Remove both (see Testing the API).
- **OpenAPI and Swagger need their packages, and only when asked for:** an
  OpenAPI document → `Microsoft.AspNetCore.OpenApi` (`AddOpenApi()` +
  `app.MapOpenApi()`, the .NET 10 default); a Swagger UI page →
  `Swashbuckle.AspNetCore` (`AddSwaggerGen`/`UseSwagger`/`UseSwaggerUI`). Neither
  asked for → reference neither.
- **Every namespace you use comes from a referenced package or project.** Add the
  `<PackageReference>` in the same output as the code that needs it; reference
  another layer's types only through a `<ProjectReference>`.
- **Nullable:** non-nullable reference properties get an initializer
  (`= string.Empty;`), `required`, or a constructor assignment (CS8618).
- **GA versions only, aligned** (the table in `technologies/dotnet.md`): a
  pre-release or non-existent version warns `NU1603` and restores a different
  build; EF Core and its provider on different lines fail with `NU1605`.
- **`Migrate()` needs a complete migration set** — the `[DbContext]` +
  `[Migration("<id>")]`-attributed class **and** `<Context>ModelSnapshot.cs`. A
  hand-written `class X : Migration { Up/Down }` has neither: `Migrate()` applies
  nothing, no tables exist, and the app fails at runtime with "relation does not
  exist" although it built and tested green. Use `EnsureCreated` instead.
- **`WebApplicationFactory<Program>` cannot see `Program`** unless `Program.cs`
  ends with `public partial class Program;` (top-level statements make the class
  internal) — the test project then fails to compile with CS0122.
- **`Program.cs` ends with `app.Run();`** — never `await app.WaitForShutdownAsync()`
  in its place. That call waits for a host nobody started: `WebApplicationFactory`
  hangs, `dotnet test` never returns, and the platform kills the run at its time
  limit (a live delivery hung 62 minutes this way). The same holds when the API
  is a single project rather than the four-project layout.
