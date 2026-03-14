# Copilot Instructions for Claido

## Build, test, and lint commands

### Backend (`backend/`, ASP.NET Core)

- Restore/build:
  - `dotnet restore backend\Claido.csproj`
  - `dotnet build backend\Claido.csproj`
- Run API:
  - `dotnet run --project backend\Claido.csproj`

### Frontend (`frontend/`, Vue 3 + Vite)

- Install deps: `npm --prefix frontend install`
- Dev server: `npm --prefix frontend run dev`
- Production build: `npm --prefix frontend run build`
- Preview build: `npm --prefix frontend run preview`

### Tests and linting status

- There are currently no test projects/suites in this repo (`dotnet test` has no test project, and `frontend/package.json` has no `test` script).
- There are currently no configured lint scripts (`npm run lint` is not defined, and no backend lint target is configured).
- Because no test framework is configured yet, there is no single-test command available at this time.

## High-level architecture

- This is a two-app project:
  - `frontend/`: SPA (Vue + Pinia + Vue Router) that renders rooms and clues.
  - `backend/`: ASP.NET Core API that creates/holds game sessions and generates room/NPC content.
- Session lifecycle:
  - Frontend starts at `LandingView.vue`, calls `POST /api/session/create` via `gameStore.createSession()`.
  - Backend `SessionController.CreateSession()` calls `AiService.GenerateSessionAsync()`, normalizes values, and stores `SessionState` in an in-memory `ConcurrentDictionary<Guid, SessionState>`.
- Room flow:
  - Frontend room views call `gameStore.enterRoom(roomName)` -> `POST /api/session/{sessionId}/room/{roomName}/enter`.
  - Backend `RoomController.EnterRoom()` returns:
    - generated AI JSON for `shell/mail/wiki/search/onion`,
    - synthetic SQLite DB (base64) for `database`,
    - static hint payload for `vault`.
- Validation flow:
  - Frontend posts answers through `gameStore.validateAnswer()`.
  - Backend `RoomController.Validate()` compares answer against vault words in `SessionState`; vault expects exactly 4 space-separated words.
- NPC chat flow:
  - Frontend `NpcChat.vue` sends message history + prompt to `POST /api/session/{sessionId}/npc/chat`.
  - Backend `NpcController` builds NPC-specific system prompt via `NpcService.GetSystemPrompt(...)` and sends conversation to `AiService.ChatAsync(...)`.

## Key codebase conventions

- Keep room IDs consistent across all layers (`shell`, `database`, `mail`, `wiki`, `search`, `onion`, `vault`):
  - frontend routes (`frontend/src/router/index.js`),
  - API calls in store (`frontend/src/stores/gameStore.js`),
  - backend room switch logic (`backend/Controllers/RoomController.cs` and `backend/Services/AiService.cs`).
- Clues are deduplicated by stable clue IDs in `gameStore.addClue(id, room, text)`. Reuse existing IDs or add deterministic new IDs when introducing clues.
- Room completion is explicitly tracked from the frontend with `store.markRoomComplete(room)` when clue conditions are met in each room view.
- `AiService` expects strict JSON from model output and strips fenced code blocks before deserialization. Preserve this behavior when changing prompts/response parsing.
- Backend secret loading convention:
  - API key comes from `API_KEY` env var.
  - `builder.LoadApiKey()` reads `.env` from repo root (or backend folder) only when `API_KEY` is not already set.
- Frontend API base URL comes from `import.meta.env.VITE_API_BASE_URL`; keep client API calls centralized in `gameStore`.
- `DatabaseView.vue` consumes a base64 SQLite payload from backend and initializes `sql.js` dynamically in-browser; backend schema/content for that room is generated in `DatabaseService.GenerateSqliteBase64(...)`.
