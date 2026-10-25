# Museum Explorer

An independent museum portfolio project built with Next.js, React, TypeScript, C# / ASP.NET Core, Entity Framework Core and Microsoft SQL Server. All exhibitions are fictional; this project is not affiliated with Te Papa.

## Current progress

Implemented:
- Responsive exhibition browser, category filters and expandable exhibition details.
- Visit planner with add/remove actions and estimated duration.
- REST API with validation and SQL Server itinerary persistence.
- Anonymous itinerary access using a random bearer token. Only its hash is stored in SQL Server.
- Browser restoration of saved itineraries and a 30-day access expiry.
- EF Core initial migration and reviewable SQL in `docs/schema.sql`.
- 25 backend tests, GitHub Actions build/test workflow, PowerShell scripts.
- Sanity Studio exhibition schema, seed content and C# GraphQL adapter.
- Explicit Sample/Sanity modes, published-only content delivery and sanitized CMS failures.

Exhibition content defaults to the sample catalogue until you connect your Sanity account. Follow [Connect Sanity and GraphQL](docs/sanity-setup.md) to create the project, import content, deploy GraphQL and switch the backend provider. Kiosk mode, feedback, AWS deployment and production operations are future milestones.

## Requirements

- Node.js 22 and npm.
- .NET SDK 9 (matches the installed development environment).
- SQL Server LocalDB with the `MSSQLLocalDB` instance.
- PowerShell.

Plan an upgrade to .NET 10 LTS before production deployment. This initial development target was selected to use the SDK already installed.

## Install

Run from this folder:

```powershell
dotnet restore backend/MuseumExplorer.Api.Tests
dotnet tool restore
Push-Location frontend
npm ci
Pop-Location
```

## Initialise SQL Server LocalDB

In a normal PowerShell terminal under your Windows account:

```powershell
SqlLocalDB info
# Only if MSSQLLocalDB is not listed:
SqlLocalDB create MSSQLLocalDB
SqlLocalDB start MSSQLLocalDB
./scripts/Initialize-Database.ps1
```

The migration creates a dedicated `MuseumExplorer` database and itinerary tables. It does not modify other application databases. Normal API startup does not run migrations.

The development connection string uses Windows authentication:

```text
Server=(localdb)\MSSQLLocalDB;Database=MuseumExplorer;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=5
```

LocalDB is a Windows development database. For AWS deployment, use a hosted SQL Server instance such as RDS for SQL Server and configure its connection via a secret. Do not use development certificate-trust settings in production.

If you choose a different instance later, override `ConnectionStrings__Museum` in the API terminal or use `dotnet user-secrets`. Do not commit passwords.

## Run

Terminal 1:

```powershell
./scripts/Start-Api.ps1
```

Terminal 2:

```powershell
./scripts/Start-Web.ps1
```

Open http://127.0.0.1:3000. The API listens on http://127.0.0.1:5080.

Next.js forwards `/api/*` to .NET, so the browser uses one origin. An optional `frontend/.env.local` can override `API_BASE_URL`; restart/rebuild the frontend after changing it.

## Check your work

```powershell
dotnet test backend/MuseumExplorer.Api.Tests --configuration Release
Push-Location frontend
npm run typecheck
npm run build
Pop-Location
./scripts/Smoke-Test.ps1
```

The smoke test requires the running API and a migrated SQL Server database. It creates one itinerary, retrieves it with its token and verifies stop order. It never prints the token. Unit/API tests do not claim to verify SQL Server persistence.

## API

| Method | Route | Purpose |
|---|---|---|
| GET | /api/exhibitions | Fictional exhibition catalogue |
| GET | /api/exhibitions/{slug} | Exhibition details |
| POST | /api/itineraries | Create itinerary; returns id, accessToken and expiry |
| GET | /api/itineraries/{id} | Read itinerary with Authorization: Bearer token |
| GET | /health | Process liveness |
| GET | /health/ready | SQL Server/schema readiness; returns 503 when unavailable |

Create body: `{"exhibitionIds":["ocean","earth"]}`.

Missing/empty, duplicate and unknown exhibition IDs are rejected. Unauthenticated reads return 401; a wrong token or expired/missing itinerary returns 404. Saving creates a new snapshot rather than updating the previous record.

## Project layout

- `frontend/app/`: visitor interface and styling.
- `backend/MuseumExplorer.Api/Features/`: catalogue and itinerary validation.
- `backend/MuseumExplorer.Api/Data/`: database entities and EF mapping.
- `backend/MuseumExplorer.Api/Migrations/`: versioned SQL Server schema.
- `backend/MuseumExplorer.Api.Tests/`: API and token validation tests.
- `scripts/`: startup, database migration and SQL round-trip check.
- `cms/`: Sanity Studio, exhibition schema and importable seed documents.
- `backend/MuseumExplorer.Api/Integrations/Sanity/`: GraphQL client, configuration and failure handling.
- `docs/`: architecture, schema and verification record.

## Development boundaries

Tokens are stored in browser local storage on personal devices. Anyone with that browser profile can access its saved itinerary. Kiosk mode must avoid persistent visitor tokens and clear state on idle. The 30-day expiry prevents access; physical cleanup of expired database rows is a future scheduled job.

The local HTTP binding, host allowlist and basic per-IP write limiter are development defaults. Before hosting: configure HTTPS, allowed hosts, trusted proxy handling, secret management, distributed rate limits as needed, database backups, cleanup jobs and monitoring.

## Next milestone

SQL Server and Sanity end-to-end verification have been reported successful. Follow [GitHub and CI setup](docs/github-setup.md) to publish this repository and verify the hosted checks. Draft preview, kiosk mode and cloud deployment remain future work.
