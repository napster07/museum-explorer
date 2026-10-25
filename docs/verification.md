# Sanity milestone verification — 25 October 2026

Passed:
- 25 backend tests, including mocked GraphQL transport, mapping, pagination, published filtering, cancellation, malformed responses and sanitized REST errors.
- Next.js production build with TypeScript.
- Sanity Studio TypeScript check and production build.
- Sanity schema extraction.
- Seed document IDs preserve ocean, light and earth for existing itineraries.

Not yet verified:
- Real Sanity project connection, GraphQL deployment and live publishing round trip: the user has not created a Sanity project yet.
- Studio build/schema extraction use the explicit build-only project ID buildcheck; no remote project was created or modified.
- No authenticated CMS browser test or live Sanity/SQL end-to-end test was performed.

SQL Server:
- User reports SQL Server now works. This milestone preserves its configuration and makes no database migration changes. The earlier LocalDB blocker below is historical.

Dependency audit:
- Initial Studio install reported 13 advisories including three high.
- Compatible same-major overrides for js-yaml, smol-toml and undici removed high advisories.
- Final Studio audit: six moderate advisories, stemming from typeid-js/uuid and their dependency parents. A forced fix proposes a major Sanity downgrade and was not applied.
- Recheck upstream releases before hosting the Studio.

See sanity-setup.md for the remaining account/project steps.

---


# Verification — 24 October 2026

Passed:
- Next.js production build (Next.js 16.4.3); TypeScript compilation passed.
- npm audit during installation: zero reported vulnerabilities.
- .NET API/tests built successfully; eight tests passed.
- Generated EF Core SQL Server migration and compiled migration SQL script.
- Frontend HTTP 200; Next.js proxy returned three exhibition records from .NET.
- Browser interaction checks: category filter, add exhibition, updated duration, expanded details, and explicit database-save failure state.
- Visually inspected narrow/mobile-width page in the app browser.

Blocked:
- SqlLocalDB info cannot read instance registry configuration in this execution session.
- SqlLocalDB start MSSQLLocalDB reports that it cannot create an automatic instance.
- Applying the migration fails at the LocalDB connection.
- /health/ready returns 503 as intended.
- SQL persistence and reload restoration have NOT been verified.
- No full accessibility audit, desktop visual inspection or cloud deployment was performed.

Next action:
Run the LocalDB commands and Initialize-Database.ps1 in a normal PowerShell session as the Windows user, then start the API and run scripts/Smoke-Test.ps1. If LocalDB fails there too, inspect its Windows Application event log and repair the LocalDB installation before retrying.
