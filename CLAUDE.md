# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and Run Commands

```bash
# Build the entire solution
dotnet build OpenERP.slnx

# Build a specific project
dotnet build OpenERP.Web/OpenERP.Web.csproj

# Run the web application
dotnet run --project OpenERP.Web/OpenERP.Web.csproj

# Run with hot reload (development)
dotnet watch --project OpenERP.Web/OpenERP.Web.csproj

# Publish for deployment
dotnet publish OpenERP.Web/OpenERP.Web.csproj -c Release -o ./output
```

### Frontend Build (Vue 3 Login Page)

```bash
cd OpenERP.Frontend
npm install
npm run build    # Output goes to ../OpenERP.Web/wwwroot/vue-login/
npm run dev      # Vite dev server with hot reload
```

### Database Migrations (EF Core)

```bash
# Create a new migration
dotnet ef migrations add MigrationName --project OpenERP.Sales --startup-project OpenERP.Web

# Apply migrations to database
dotnet ef database update --project OpenERP.Sales --startup-project OpenERP.Web

# Rollback to specific migration
dotnet ef database update PreviousMigrationName --project OpenERP.Sales --startup-project OpenERP.Web
```

## Architecture Overview

Modular ERP system built on ASP.NET Core MVC (.NET 10.0) with SQL Server (LocalDB for development).

**Authentication**: Cookie-based auth (8-hour sliding session), demo account `admin/123456`. Auth claims: `NameIdentifier`, `Name`, `erp:account`, `erp:company`, `erp:fiscalYear`.

**Localization**: Three languages — Simplified Chinese (zh-Hans, default), Traditional Chinese (zh-Hant), English (en). Resolved via QueryString > Cookie > Accept-Language. Resources in `OpenERP.Web/Resources/Localization/`.

**Frontend**: Vue 3 + Element Plus login page at `/vue-login/`, built from separate `OpenERP.Frontend` project via Vite. Main UI uses server-rendered Razor views with Bootstrap.

### No Shared Project

There is no `OpenERP.Shared` or `OpenERP.Common` project. `BaseEntity` is duplicated identically across all EF Core modules (`OpenERP.{Module}/Models/Entities/BaseEntity.cs`). All module DbContext classes are also identically named `ApplicationDbContext`, which requires `using` aliases in `Program.cs` (e.g., `using SalesDbContext = OpenERP.Sales.Data.ApplicationDbContext;`).

### Data Access Patterns

Two patterns coexist:

1. **Entity Framework Core** (preferred for new modules): Each module has its own `ApplicationDbContext` with `DbSet<T>` properties. All registered in `Program.cs` sharing one `DefaultConnection`.

2. **Raw SQL with ADO.NET** (used in HR, BasicData): Repository interfaces defined in module, SQL implementations either in `OpenERP.Web/Data/HR/` (HR) or within the module itself (BasicData). Uses `SqlConnection` with parameterized queries.

### Module Structure

Business modules follow this pattern:
```
OpenERP.{Module}/
├── Models/Entities/    # Domain entities (inherit from BaseEntity)
├── Data/               # DbContext and repositories
└── Migrations/         # EF Core migrations (EF Core modules only)
```

The web application uses ASP.NET Core Areas to organize controllers and views:
```
OpenERP.Web/
├── Areas/{Module}/Controllers/   # Module controllers
├── Areas/{Module}/Views/         # Razor views
├── Data/HR/                      # ADO.NET repositories for HR module
├── Controllers/                  # Root controllers (Home, Account, AuthApi)
├── Resources/Localization/       # .resx localization files
└── wwwroot/
    ├── vue-login/                # Built Vue 3 login (from OpenERP.Frontend)
    ├── css/                      # Custom styles per module page
    └── js/                       # Custom scripts per module page
```

### Routing

- Area routes: `{area:exists}/{controller=Home}/{action=Index}/{id?}`
- Default route: `{controller=Account}/{action=Login}/{id?}`
- `/` redirects to `/login`, which redirects to `/Account/Login`

### API Controllers (for Vue Frontend)

Place API controllers in `OpenERP.Web/Controllers/` with `[ApiController]` and `[Route("api/[controller]")]`. Reference pattern: [AuthApiController.cs](OpenERP.Web/Controllers/AuthApiController.cs). Return `ActionResult<T>`. Auth API uses raw ADO.NET for authentication queries against `HR_Employee` table.

## Naming Conventions (from AGENTS.md)

### Code Comments
- All class/property/field/method names must have Chinese comments explaining business meaning
- Comments go above the member being documented
- For foreign key `Id` fields, specify which dictionary/entity they reference
- For abbreviations, provide Chinese explanation

### Database Table Naming
All business tables use format: `<ModulePrefix>_<EntityName>` (PascalCase, singular)

| Module | Prefix | Example Table |
|--------|--------|---------------|
| Asset | AS | `AS_Asset` |
| BasicData | BD | `BD_BasicDataType` |
| CRM | CRM | `CRM_Lead` |
| Finance | FIN | `FIN_Account` |
| HR | HR | `HR_Employee` |
| Logistics | LOG | `LOG_Shipment` |
| Office | OF | `OF_Meeting` |
| Production | PRD | `PRD_ProductionOrder` |
| Purchasing | PO | `PO_PurchaseOrder` |
| Sales | SA | `SA_SalesOrder` |
| Service | SV | `SV_ServiceContract` |
| Transport | TR | `TR_Vehicle` |

**Note**: Some older EF Core modules (Sales, Purchasing, Finance, Logistics) don't yet use prefixed table names. New modules should always configure table names in `OnModelCreating` using `modelBuilder.Entity<T>().ToTable("Prefix_EntityName")`.

### Entity Base Class
All entities inherit from `BaseEntity` which provides: `Id` (int, PK), `CreatedAt`, `UpdatedAt` (timestamps), `CreatedBy`, `UpdatedBy` (string, optional), `IsDeleted` (soft delete flag).

## Adding a New Module

1. Create project: `dotnet new classlib -n OpenERP.{Module} -f net10.0`
2. Add to solution: `dotnet sln add OpenERP.{Module}`
3. Add reference in `OpenERP.Web/OpenERP.Web.csproj`
4. Create entity classes in `Models/Entities/` (inherit from `BaseEntity`)
5. Create `Data/ApplicationDbContext.cs` with `DbSet<T>` properties and `OnModelCreating` for prefixed table names
6. Register DbContext in `OpenERP.Web/Program.cs` (add `using` alias to avoid name conflicts)
7. Create Area in `OpenERP.Web/Areas/{Module}/` with Controllers and Views
8. Run initial migration: `dotnet ef migrations add InitialCreate --project OpenERP.{Module} --startup-project OpenERP.Web`

## Connection String

Default: LocalDB (`(localdb)\mssqllocaldb`), database name `OpenERP`.
Configured in `OpenERP.Web/appsettings.json`.

## Key Files

- [OpenERP.slnx](OpenERP.slnx) - Solution file listing all projects
- [OpenERP.Web/Program.cs](OpenERP.Web/Program.cs) - Application entry point, DI registration, middleware pipeline
- [AGENTS.md](AGENTS.md) - Chinese naming and table convention rules
