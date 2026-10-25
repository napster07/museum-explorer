# Connect Sanity and GraphQL

## What is ready

The C# API has two explicitly selected content providers: Sample (default) and Sanity. Sanity mode queries the published GraphQL perspective. It never falls back silently to sample content.

The Studio schema mirrors the existing REST exhibition model. SQL Server remains the store for visitor itineraries. No database schema change is needed for this milestone.

## 1. Create your project

Sign in or create your account at https://www.sanity.io/manage and create a project named Museum Explorer. Create a dataset named production.

For this fictional portfolio content, a public dataset avoids needing a read token. A public dataset allows anyone to query its published content; only authorised editors can write. Do not put confidential material in it. If you choose private, follow the token instructions below.

Keep your project ID (not the project name) and dataset name. These identifiers are not secrets. You must finish any account/terms steps yourself.

## 2. Configure and install Studio

From the museum-explorer root:

```powershell
Push-Location cms
Copy-Item .env.example .env.local
# Edit .env.local: replace your-project-id with the real project ID.
npm ci
npx sanity login
Pop-Location
```

The Studio uses Node.js 22.12 or newer. The .env.local file should contain:

```dotenv
SANITY_STUDIO_PROJECT_ID=your-real-project-id
SANITY_STUDIO_DATASET=production
```

These values identify the CMS project. Never put API tokens into SANITY_STUDIO_* variables: they are exposed in the browser bundle.

## 3. Import the existing exhibitions

From the project root:

```powershell
Push-Location cms
npx sanity datasets import seed/exhibitions.ndjson --dataset production --missing
Pop-Location
```

This intentionally publishes the three fictional sample documents with IDs ocean, light and earth. Keeping those IDs preserves existing SQL itinerary references. The --missing option skips existing IDs rather than overwriting editorial changes. If an existing ID belongs to unrelated content, resolve that conflict before import.

## 4. Deploy the GraphQL schema

```powershell
Push-Location cms
npm run graphql:deploy
npm run dev
```

Deploying GraphQL makes the exhibition query available. It is separate from importing documents and separate from hosting the Studio. Re-run graphql:deploy whenever you change schema fields; ordinary content edits do not require redeploying GraphQL.

Open the Studio URL printed by the command. If a CORS-origin prompt appears, add only the exact local Studio origin to your project's allowed origins. Sign in, edit an exhibition and click Publish. Draft changes are not shown in the visitor app.

## 5. Connect .NET

In a new PowerShell terminal at the project root:

```powershell
dotnet user-secrets set "Content:Provider" "Sanity" --project backend/MuseumExplorer.Api
dotnet user-secrets set "Sanity:ProjectId" "your-real-project-id" --project backend/MuseumExplorer.Api
dotnet user-secrets set "Sanity:Dataset" "production" --project backend/MuseumExplorer.Api
./scripts/Start-Api.ps1
```

The default GraphQL tag is default. User secrets are loaded in Development and are outside the repository. If an API instance is already running, stop and restart it after configuration changes.

For a private dataset, create a dedicated token with read/viewer permission in Sanity. Configure it locally as Sanity:ReadToken using .NET user secrets or the Sanity__ReadToken environment variable. Do not paste it into chat or put it into the frontend/Studio environment. The API sends it server-to-server. Even with a token, the adapter explicitly requests only published content.

To return to the local samples:

```powershell
dotnet user-secrets set "Content:Provider" "Sample" --project backend/MuseumExplorer.Api
```

Restart the API after switching.

## 6. Verify live content

Start the frontend in another terminal with ./scripts/Start-Web.ps1.

1. Open the visitor website and confirm the three imported exhibitions appear.
2. In Studio, change an exhibition summary but do not publish: refreshing the visitor page should still show the published summary.
3. Publish the edit, then refresh the visitor page: the updated summary should appear.
4. Add an exhibition to your visit, save, reload and confirm SQL Server restoration still works.
5. Run ./scripts/Smoke-Test.ps1 for the seeded ocean/earth itinerary round trip.

A published dataset can legitimately be empty; the UI then shows an empty-state message. A missing GraphQL deployment, failed request or invalid document returns a sanitized 503 from the API instead of displaying samples.

## Query path

Browser -> Next.js /api proxy -> ASP.NET Core REST endpoint -> Sanity GraphQL -> published exhibition documents.

Sanity endpoint:
https://PROJECT_ID.api.sanity.io/v2025-02-19/graphql/production/default?perspective=published

The adapter uses POST with GraphQL variables for limit/offset and fetches pages of 100. It orders by document ID, preserves _id as the REST id, and maps slug.current to the REST slug. It requests fresh content directly from the API without application caching. Each request has a 10-second timeout.

Studio validation guides editors. The C# adapter also validates required fields and enum values because imports can bypass Studio validation. Failed or malformed upstream responses never expose the read token or upstream response body.

## Dependency maintenance

The Studio pins Sanity 6.20.0. Compatible overrides update js-yaml 3.x, smol-toml 1.x and undici 7.x to patched releases. The current npm audit still reports moderate advisories in upstream tooling. A forced audit fix proposes a major Sanity downgrade, so it was not applied. Revisit the lockfile as upstream fixes become available; do not assume the dependency audit is clean.

## Current limits

Draft preview, images, related stories, content webhooks, caching and hosted Studio deployment are not included in this milestone. Category and illustration choices match the current frontend. Avoid deleting and recreating an exhibition merely to change its title: editing the existing document preserves its stable ID.

Live Sanity verification requires your project to exist and its GraphQL schema to be deployed. Automated backend tests use mocked HTTP responses; they do not claim to connect to your account.

## Official references

- GraphQL and published perspectives: https://www.sanity.io/docs/content-lake/graphql
- GraphQL deployment: https://www.sanity.io/docs/cli-reference/cli-graphql
- Dataset import: https://www.sanity.io/docs/cli-reference/cli-datasets
