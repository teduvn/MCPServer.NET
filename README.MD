# Order Management System

A clean architecture .NET solution for order management with separation of concerns across multiple layers.

## Project Structure

```
OrderManagement/
├── src/
│   ├── OrderManagement.Domain/              # Domain layer (entities, value objects, domain events)
│   ├── OrderManagement.Application/         # Application layer (use cases, DTOs, mappings)
│   ├── OrderManagement.Infrastructure/      # Infrastructure layer (database, external services)
│   └── OrderManagement.WebAPI/              # Web API layer (controllers, endpoints)
├── tests/
│   └── OrderManagement.Tests/               # Unit and integration tests
└── docs/                                    # Documentation
```

### Project Descriptions

- **OrderManagement.Domain**: Contains core business logic, entities, value objects, and domain events
- **OrderManagement.Application**: Application services, DTOs, mappings, and use case handlers
- **OrderManagement.Infrastructure**: Database context, repositories, external service implementations, and persistence migrations
- **OrderManagement.WebAPI**: REST API controllers, middleware, and configuration
- **OrderManagement.Tests**: Comprehensive unit and integration tests

## Prerequisites

- .NET 8.0 or later
- SQL Server (or configured database provider)
- Entity Framework Core CLI tools

## Getting Started

### Installation

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build
```

### Configuration

#### User Secrets (Development)

```bash
dotnet user-secrets set "SendGrid:ApiKey" "SG.xxxxx"
```

## Database Migrations

All migration commands target the `OrderManagement.Infrastructure` project (contains DbContext) with `OrderManagement.WebAPI` as the startup project.

### Creating a New Migration

```bash
dotnet ef migrations add <MigrationName> `
  --project src/OrderManagement.Infrastructure `
  --startup-project src/OrderManagement.WebAPI `
  --output-dir Persistence/Migrations
```

**Example:**
```bash
dotnet ef migrations add AddIdentityTables `
  --project src/OrderManagement.Infrastructure `
  --startup-project src/OrderManagement.WebAPI `
  --output-dir Persistence/Migrations
```

### Listing Migrations

View all created migrations:

```bash
dotnet ef migrations list `
  --project src/OrderManagement.Infrastructure `
  --startup-project src/OrderManagement.WebAPI
```

### Applying Migrations

#### Development/Test Environment Only

Apply migrations to update the database:

```bash
dotnet ef database update `
  --project src/OrderManagement.Infrastructure `
  --startup-project src/OrderManagement.WebAPI
```

#### Production Deployment (SQL Script Approach)

**Step 1: Generate SQL script**

```bash
dotnet ef migrations script --idempotent `
  --output src/OrderManagement.Infrastructure/Persistence/Scripts/migration_$(Get-Date -Format 'yyyyMMdd').sql `
  --project src/OrderManagement.Infrastructure `
  --startup-project src/OrderManagement.WebAPI
```

**Step 2: Review the SQL script**

- Check for any `DROP TABLE/COLUMN` statements
- Verify new indexes won't negatively impact production performance
- Validate data migrations (`UPDATE` statements) are correct

**Step 3: Apply on staging environment**

```bash
sqlcmd -S <staging-server> -U <username> -P <password> -d OrderManagement `
  -i .\src\OrderManagement.Infrastructure\Persistence\Scripts\migration_yyyymmdd.sql
```

**Step 4: Verify staging environment**

- Test all related functionality
- Monitor performance metrics

**Step 5: Apply on production**

During a maintenance window:

```bash
sqlcmd -S <prod-server> -U <username> -P <password> -d OrderManagement `
  -i .\src\OrderManagement.Infrastructure\Persistence\Scripts\migration_yyyymmdd.sql
```

### Removing a Migration

Remove the last unapplied migration (use with caution):

```bash
dotnet ef migrations remove `
  --project src/OrderManagement.Infrastructure `
  --startup-project src/OrderManagement.WebAPI
```

## Building and Running

```bash
# Build solution
dotnet build

# Run tests
dotnet test

# Run the Web API
dotnet run --project src/OrderManagement.WebAPI
```