# SE-Project

Software Engineering Project at the VU — QuattroLingo (language learning app).

## Running the project

**First time / after pulling new backend or frontend changes:**
```bash
docker compose up --build
```

**Day-to-day (no dependency changes):**
```bash
docker compose up
```

**Only changed backend code:**
```bash
docker compose up --build server
```

**Stop everything (keeps your Postgres data):**
```bash
docker compose down
```

**Stop and wipe the database too:**
```bash
docker compose down -v
```

App runs at:
- Frontend: `http://localhost:5173`
- Backend: `http://localhost:5000`

## Environment variables

Create `.env` and fill in real values before running anything. `.env` is gitignored — never commit it, it should hold the Postgres credentials.

## Naming conventions

- `server/`: PascalCase folders and files (`Controllers/`, `DTOs/`, `AuthService.cs`) — standard C#/.NET convention
- `client/`: lowercase folders (`pages/`, `components/`, `hooks/`) — standard JS/TS convention
- Match the convention of the language inside the folder, not one global rule

## Folder structure

**`server/`**

```text
Controllers/    → API endpoints, thin — delegate to Services
DTOs/           → request/response shapes (never expose entities directly)
Entities/       → domain models / DB tables
Services/       → actual business logic
Exceptions/     → custom exceptions, handled by middleware
Hubs/           → SignalR real-time hubs
Data/           → AppDbContext, migrations
```

**`client/src/`**

```text
api/            → all backend calls live here (no fetch() calls inside components)
components/     → reusable UI pieces
pages/          → route-level views
hooks/          → custom hooks
types/          → shared TS types
context/        → shared state (auth, etc.)
```


## Tests

- Every new feature needs tests.
- Backend tests live in `server.Tests/`, mirroring `server/`'s folder structure (e.g. `server.Tests/Controllers/` tests `server/Controllers/`)
- Run backend tests: `dotnet test server.Tests/server.Tests.csproj`

## Why two client Dockerfiles

- `client/Dockerfile` — production build (nginx-served static files). Not used day-to-day.
- `client/Dockerfile.dev` — what `docker compose up` actually runs. Live-reloads on file changes, no rebuild needed for frontend edits.

## Nginx / reverse proxy

Not set up yet. Revisit later — a reverse proxy in front of both services would remove the need for CORS config entirely, useful for deployment.