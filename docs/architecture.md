# Architecture

Browser -> Next.js same-origin /api proxy -> ASP.NET Core -> EF Core -> SQL Server LocalDB.

The browser never connects to SQL Server. Exhibition reads use IExhibitionCatalogue: explicitly selected Sample mode returns fictional local content; Sanity mode calls the published GraphQL endpoint. Studio manages the documents. Sanity mode has no silent sample fallback. Itinerary writes validate IDs against the selected provider before saving to SQL Server.

## Data model

Itinerary: GUID id, SHA-256 access-token hash, creation timestamp, expiry timestamp.
ItineraryStop: itinerary id, exhibition id, position.

The composite stop primary key prevents duplicate exhibitions within one itinerary. A unique itinerary/position index protects ordering. Deleting an itinerary cascades to its stops. Exhibition IDs refer to editorial content and have no SQL foreign key to the CMS.

## Access and failure handling

The server generates a cryptographically random 32-byte token on creation. Its SHA-256 hash is persisted; the original token is returned once and used in an Authorization header for reads. Responses carrying itinerary data are marked no-store. Token comparisons use a fixed-time comparison.

Application errors use Problem Details. Database readiness is separate from liveness and queries the actual itinerary table, so an unmigrated schema is not reported ready. The UI keeps the current selection when a save fails.

## Deferred functionality

Live Sanity project connection; CMS draft preview; feedback; updating existing itineraries; kiosk idle reset; scheduled deletion of expired records; AWS hosting and backups; accessibility audit; browser automation in CI.

## Deployment direction

Next.js on a compatible AWS hosting platform; containerised ASP.NET Core on an appropriate AWS compute service; SQL Server on RDS; secrets outside source control. Select and provision these services in a later milestone after estimating costs. Nothing has been deployed to a cloud account.
