# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

**API (from repo root or `src/StratusFabTracker.Api`):**
```bash
dotnet restore          # install dependencies
dotnet run              # starts API on http://localhost:5072
dotnet test             # run all tests
```

**Web (from `src/stratus-fab-tracker-web`):**
```bash
npm install
npm run dev             # Vite dev server, proxies /api → http://localhost:5072
npm run build
```

**Run a single test class:**
```bash
dotnet test --filter "FullyQualifiedName~ClassName"
```

**Regenerate seed data:**
```bash
node scripts/generate-seed.mjs
```

## Architecture

**Two projects, one solution (`StratusFabTracker.sln`):**

- `src/StratusFabTracker.Api` — .NET 8 minimal API
- `src/stratus-fab-tracker-web` — Vue 3 + TypeScript SPA (single file: `src/App.vue`)
- `src/Data/spools.seed.json` — ~200 spools loaded into memory at startup; no database

**API layer structure:**
- `Domain/` — `Spool`, `Station` enum (`Detailing→Cut→Weld→QC→Shipped→Installed`), `StationExtensions.Next()`
- `Application/` — `SpoolWorkflowService` (advance logic), `DashboardService`, `ThroughputService`, `ISpoolRepository`/`IClock` interfaces
- `Infrastructure/` — `InMemorySpoolRepository` (singleton, thread-unsafe), `SeedData`
- `Program.cs` — minimal API wiring; all endpoints defined here

**Key domain rules:**
- A spool's `CurrentStation` is derived from its `StatusHistory` (latest by `ChangedAt`), defaulting to `Detailing` if history is empty
- `Station.Next()` returns `null` for `Installed` (terminal state)
- `POST /api/spools/{id}/advance` returns `204 No Content` on success, `400` for invalid transitions, `404` if not found

**Existing endpoints:**
- `GET /api/dashboard` → `DashboardDto` (WIP counts by station + past-due count)
- `GET /api/throughput` → throughput data
- `GET /api/spools/{id}` → single spool
- `POST /api/spools/{id}/advance` → advance spool to next station

**Tests** live in `src/StratusFabTracker.Tests/`. The `IClock` interface exists to enable deterministic time in tests.
