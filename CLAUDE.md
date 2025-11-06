# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Vibora** is a padel matchmaking platform (MVP) with a full-stack architecture:

- **Backend**: .NET 9 Modular Monolith with Clean Architecture, DDD, and CQRS
- **Frontend**: Next.js 15 (App Router) + React 19 PWA, mobile-first
- **Auth**: Supabase Auth (JWT) with automatic backend sync
- **Database**: PostgreSQL (single DB, separate DbContexts per module)

**Repository structure**:
- `vibora-backend/` - .NET 9 backend API
- `vibora-frontend/` - Next.js 15 frontend PWA

## Essential Commands

### Backend (.NET 9)

```bash
cd vibora-backend

# Build solution
dotnet build Vibora.sln

# Run with Aspire (orchestrates migrations + API)
dotnet run --project src/Vibora.AppHost/Vibora.AppHost.csproj
# API: http://localhost:5000 | Swagger: http://localhost:5000/swagger

# Run tests (78 integration tests with TestContainers)
dotnet test Vibora.sln
dotnet test --filter "CreateGame_WithValidData_ShouldReturnCreatedGame"

# Create migration (example for Games module)
dotnet ef migrations add MigrationName \
  --project src/modules/Games/Vibora.Games/Vibora.Games.csproj \
  --startup-project src/Vibora.MigrationService/Vibora.MigrationService.csproj \
  --context GamesDbContext \
  --output-dir Infrastructure/Data/Migrations
```

### Frontend (Next.js 15)

```bash
cd vibora-frontend

# Development server
npm run dev
# App: http://localhost:3000

# Build for production
npm run build
npm start
```

### Full Stack Development

```bash
# Terminal 1 - Backend
cd vibora-backend && dotnet run --project src/Vibora.AppHost/Vibora.AppHost.csproj

# Terminal 2 - Frontend
cd vibora-frontend && npm run dev
```

---

# Backend Architecture (.NET 9)

## Modular Monolith Structure

```
vibora-backend/src/
├── Vibora.AppHost/              # .NET Aspire orchestrator
├── Vibora.MigrationService/     # Worker applying migrations at startup
├── Vibora.Web/                  # API entry point
├── Vibora.Shared/               # AggregateRoot, Result<T>, IUnitOfWork
└── modules/
    ├── Users/Vibora.Users/                # User module (internal)
    │   └── Vibora.Users.Contracts/        # Public contracts
    ├── Games/Vibora.Games/
    │   └── Vibora.Games.Contracts/
    └── Notifications/Vibora.Notifications/
        └── Vibora.Notifications.Contracts/
```

## Module Structure (Clean Architecture)

Each module follows strict Clean Architecture with **ALL classes `internal`** except `*ModuleServiceRegistrar`:

```
Vibora.Games/
├── Api/GameEndpoints.cs                   # Minimal API (internal)
├── Application/Commands/CreateGame/       # CQRS handlers (internal)
├── Domain/Game.cs                         # Aggregate Root (internal)
├── Infrastructure/Data/GamesDbContext.cs  # EF Core (internal)
└── GamesModuleServiceRegistrar.cs         # ONLY public class
```

**Dependency Flow**: `Api → Application (MediatR) → Domain ← Infrastructure`

**Cross-Module Communication**: Strategy Pattern via `IUsersServiceClient`, `IGamesServiceClient`
- Monolith mode (default): In-process MediatR calls
- Microservices mode: HTTP calls (toggle in `appsettings.json: DeploymentMode`)

## Key Patterns

### 1. Result Pattern (Ardalis.Result)

Domain methods return `Result<T>` instead of throwing exceptions:

```csharp
// Domain
public static Result<Game> Create(...)
{
    var validationResult = Validate(...);
    if (!validationResult.IsSuccess)
        return Result<Game>.Invalid(validationResult.ValidationErrors);

    var game = new Game { ... };
    game.AddDomainEvent(new GameCreatedDomainEvent(...));
    return Result<Game>.Success(game);
}

// Handler checks result before accessing value
var result = Game.Create(...);
if (!result.IsSuccess) return Result.Invalid(result.ValidationErrors);
await _repository.AddAsync(result.Value);
```

### 2. CQRS with MediatR

**Commands** (write): `CreateGameCommand → CreateGameCommandHandler → Domain.Create() → Repository → UnitOfWork.SaveChangesAsync()`

**Queries** (read): `GetGamesQuery → GetGamesQueryHandler → DbContext direct query (bypass aggregates)`

### 3. Unit of Work + Domain Events

Events are raised in domain, published AFTER transaction commit:

```csharp
// Domain raises (doesn't publish)
game.AddDomainEvent(new GameCreatedDomainEvent(...));

// UnitOfWork publishes after commit
public async Task<int> SaveChangesAsync(CancellationToken ct)
{
    var events = _dbContext.ChangeTracker.Entries<AggregateRoot>()
        .SelectMany(e => e.Entity.DomainEvents).ToList();

    var result = await _dbContext.SaveChangesAsync(ct); // Transaction

    foreach (var evt in events)
        await _publisher.Publish(evt, ct); // Publish after success

    return result;
}
```

### 4. Migration Service Pattern

Aspire orchestrates migrations via dedicated worker:
1. PostgreSQL starts
2. `Vibora.MigrationService` applies all pending migrations (Games, Users, Notifications DbContexts)
3. Service exits
4. `Vibora.Web` starts (DB guaranteed up-to-date)

## Tech Stack

- .NET 9, EF Core, PostgreSQL, MediatR, MassTransit (InMemory), Ardalis.Result
- xUnit + FluentAssertions + TestContainers for integration tests

## Integration Tests Configuration

**CRITICAL TestContainers Setup**:

```csharp
// Use PostGIS image (NOT standard postgres) - required for Games GPS features
_postgresContainer = new PostgreSqlBuilder()
    .WithImage("postgis/postgis:17-3.5")  // ✅ Correct
    .WithDatabase("viboradb_test")
    .WithUsername("postgres")
    .WithPassword("postgres")
    .Build();

// Disable Hangfire in tests (causes connection issues and interference)
var hangfireServerDescriptor = services.FirstOrDefault(d =>
    d.ServiceType.FullName == "Hangfire.IBackgroundProcessingServer");
if (hangfireServerDescriptor != null)
{
    services.Remove(hangfireServerDescriptor);
}

// Configure case-insensitive JSON for test flexibility (camelCase or PascalCase)
services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});
```

**Test Rules**:
- NEVER use `postgres:17-alpine` image (missing PostGIS extension)
- ALWAYS disable Hangfire server in test environment
- Integration tests MUST pass before any commit
- Test cleanup MUST delete child tables before parent tables (FK constraints)

## Authentication

**JWT from Supabase**:
- Frontend signup → Supabase Edge Function webhook `POST /users/sync`
- Backend validates JWT symmetric key, extracts `ExternalId` from `sub` claim
- `Vibora.Web/Program.cs:69-108` configures JWT Bearer auth

## Guest User Flow

1. Guest joins via magic link: `POST /games/{id}/players/guest` (creates `Participation` with `UserExternalId = "guest:{phoneNumber}"`)
2. Guest creates account: Supabase triggers `POST /users/sync` → backend auto-reconciles guest participations by phone/email

Files: `JoinGameAsGuestCommandHandler.cs`, `SyncUserCommandHandler.cs:84-120`

## Notifications Architecture

**Event-Driven System** - Notifications are created ONLY when domain events occur:

```
Domain Event → MassTransit → Event Consumer → Notification Creation
```

**Flow Example**:
1. User joins game → `PlayerJoinedDomainEvent` raised
2. `UnitOfWork.SaveChangesAsync()` publishes event via MassTransit
3. `PlayerJoinedEventConsumer` receives event
4. Consumer calls `NotificationTemplateService.BuildPlayerJoinedContent(playerName, location, date)`
5. Notification stored in DB → sent via FCM/email/SMS based on user preferences

**Template Service Rules**:
- Use dynamic context from events (player names, game details, etc.)
- NO hardcoded test data in production code
- Templates follow format: `BuildXXXContent(params) → GenerateContent(type, context)`
- Apply DRY: Common fields (location, date) extracted via `CreateBaseContext()` helper

**Debugging "No notifications"**:
- Notifications require user actions (join game, cancel game, etc.)
- Empty notifications table = No events triggered yet (expected on fresh app)
- Test flow: Create game → Another user joins → Host receives notification

## Common Gotchas

1. **Module visibility**: All classes MUST be `internal` except `*ModuleServiceRegistrar`
   - Exception: Domain entities may be `public` if needed by integration tests
   - NEVER expose Application/Infrastructure classes across modules

2. **Migrations**: Always use `--startup-project src/Vibora.MigrationService` for EF commands

3. **Domain events**: Use `UnitOfWork.SaveChangesAsync()` to commit + publish atomically

4. **Cross-module calls**: Use `IUsersServiceClient`/`IGamesServiceClient` interfaces only

5. **Result pattern**: Check `result.IsSuccess` before accessing `result.Value`

6. **Module boundaries**: Respect domain ownership - UserNotificationPreferences belongs in Notifications module (user-specific notifications config), NOT in Users module (core identity)

7. **No hardcoded test data in services**: Services must use dynamic context, NEVER hardcoded values like "John Doe" or "Club Padel Paris" (use templates with parameters)

8. **Code quality checklist before commit**:
   - Remove dead code (unused methods, commented code)
   - Apply DRY (extract helper methods for repeated logic)
   - Keep methods private by default (public only when exposed via contracts)
   - Run ALL tests (unit + integration) - NO RED TESTS allowed

---

# Frontend Architecture (Next.js 15)

## Tech Stack

Next.js 15 (App Router), React 19, TypeScript, Tailwind CSS v4, Radix UI, Framer Motion, Supabase Auth, React Hook Form + Zod

## Structure

```
vibora-frontend/
├── app/                          # App Router pages
│   ├── auth/login|signup|callback/page.tsx
│   ├── games/[id]/page.tsx
│   ├── create-game/page.tsx
│   ├── my-games/page.tsx
│   ├── join/[token]/page.tsx     # Magic Link (guest mode)
│   ├── settings/profile/page.tsx
│   └── page.tsx                  # Home (game list)
│
├── components/
│   ├── ui/                       # Radix + custom design system
│   │   ├── game-card.tsx
│   │   ├── filter-chip.tsx
│   │   └── vibora-*.tsx          # VPage, VHeader, VMain, etc.
│   └── mobile-nav.tsx            # Bottom nav bar
│
├── lib/
│   ├── api/vibora-client.ts      # Backend API client
│   ├── api/vibora-types.ts       # TypeScript DTOs
│   ├── auth/supabase-auth.ts     # Supabase wrappers
│   └── animation-variants.ts     # Framer Motion
│
└── middleware.ts                 # Route protection
```

## Key Features

### 1. Authentication (Supabase)

```typescript
// lib/auth/supabase-auth.ts
export async function signUp({ email, password, firstName, lastName, skillLevel }) {
  const supabase = getSupabaseClient()
  const { data, error } = await supabase.auth.signUp({
    email,
    password,
    options: {
      data: { first_name: firstName, last_name: lastName, skill_level: skillLevel || 5 },
      emailRedirectTo: `${window.location.origin}/auth/callback`,
    },
  })
  return { user: data.user, session: data.session, error }
}

// OAuth
export async function signInWithOAuth(provider: "google" | "apple") {
  return await supabase.auth.signInWithOAuth({
    provider,
    options: { redirectTo: `${window.location.origin}/auth/callback` }
  })
}
```

**Backend Sync**: Supabase Edge Function triggers `POST /users/sync` after signup

### 2. API Client

```typescript
// lib/api/vibora-client.ts
async function getViboraAuthHeaders(customToken?: string) {
  const headers = { "Content-Type": "application/json" }

  if (customToken) {
    headers["Authorization"] = `Bearer ${customToken}`
  } else {
    // Try Supabase session first, fallback to guest token
    const { data: { session } } = await supabase.auth.getSession()
    if (session?.access_token) {
      headers["Authorization"] = `Bearer ${session.access_token}`
    } else {
      const guestToken = getGuestToken()
      if (guestToken) headers["Authorization"] = `Bearer ${guestToken}`
    }
  }
  return headers
}

// Unified API
export const viboraApi = {
  games: {
    getAvailableGames(query?), getMyGames(), getGameDetails(id),
    createGame(request), joinGame(id), joinGameAsGuest(id, { name, phone }),
    leaveGame(id), cancelGame(id)
  },
  shares: {
    createGameShare(gameId), getShareByToken(token), getShareMetadata(token)
  },
  users: {
    getCurrentUserProfile(), updateProfile(request), uploadProfilePhoto(file),
    getUserPublicProfile(externalId), claimGuestParticipations()
  }
}
```

### 3. Route Protection

```typescript
// middleware.ts
export async function middleware(req: NextRequest) {
  const supabase = createMiddlewareClient({ req, res })
  const { data: { session } } = await supabase.auth.getSession()

  const protectedPaths = ['/my-games', '/create-game', '/settings']
  const isProtected = protectedPaths.some(path => req.nextUrl.pathname.startsWith(path))

  if (isProtected && !session) {
    return NextResponse.redirect(new URL('/auth/login', req.url))
  }
  return res
}
```

### 4. Guest Mode (Magic Links)

```typescript
// app/join/[token]/page.tsx
const { data: share } = await viboraApi.shares.getShareByToken(token)

const handleJoinAsGuest = async ({ name, phoneNumber }) => {
  await viboraApi.games.joinGameAsGuest(share.game.id, { name, phoneNumber })
  router.push('/join/success') // Show conversion modal after 3s
}
```

**Flow**:
1. Host shares `vibora.app/join/abc12345`
2. Guest lands on `/join/[token]`, fills form (name, phone)
3. Backend stores `Participation` with `UserExternalId = "guest:{phoneNumber}"`
4. After game, soft modal: "Create account to track your games"
5. If signup → backend auto-reconciles guest participations

### 5. Design System

Reusable UI components with consistent animations:

```typescript
import { VPage, VHeader, VMain, VSection, SectionHeader, UpcomingGameCard } from "@/components/ui"

<VPage animate>
  <VHeader>
    <SectionHeader title="Parties" actionLabel="Filtrer" />
  </VHeader>
  <VMain>
    <VSection animate>
      <VScrollContainer enableSnap edgeToEdge>
        {games.map(game => <UpcomingGameCard key={game.id} {...game} />)}
      </VScrollContainer>
    </VSection>
  </VMain>
</VPage>
```

See `vibora-frontend/DESIGN_SYSTEM.md` for complete component library.

### 6. Mobile-First PWA

- Bottom navigation: 2 tabs (Games, Profile) + central FAB (Create Game)
- Responsive design with Tailwind breakpoints
- Service worker caching for offline support
- Install prompt for "Add to Home Screen"

## Development Guidelines

**Component Patterns**:
- Server Components by default, `"use client"` only when needed (state, events, browser APIs)
- TypeScript strict mode, use DTOs from `vibora-types.ts`
- Show skeletons during loading (`<SkeletonGameCard />`)

**Data Fetching**:
- Server Components: `async/await` with `viboraApi` calls
- Client Components: `useState` + `useEffect` pattern

**Styling**:
- Tailwind utilities: `bg-card/80 backdrop-blur-sm`
- Mobile-first: `text-sm md:text-base`
- Theme support: `bg-background text-foreground`

## Environment Variables

```bash
# Required in vibora-frontend/.env.local
NEXT_PUBLIC_SUPABASE_URL=https://xxx.supabase.co
NEXT_PUBLIC_SUPABASE_ANON_KEY=eyJhbGc...
NEXT_PUBLIC_VIBORA_API_URL=http://localhost:5000
```

## Common Gotchas

1. **"use client"**: Must be at top of file before imports
2. **Env vars**: Must start with `NEXT_PUBLIC_` for browser access
3. **API errors**: Always check `error` field in `ApiResponse<T>` before `data`
4. **Session**: Call `supabase.auth.getSession()` on every page load (SSR/hydration)

---

# Full-Stack Integration

## Authentication Flow

1. Frontend: `signUp()` via Supabase Auth
2. Supabase: Triggers Edge Function webhook `POST /users/sync`
3. Backend: Creates User + auto-claims guest participations
4. Frontend: Receives JWT, stores in Supabase session
5. All API calls: Include `Authorization: Bearer {JWT}` header
6. Backend: Validates JWT, extracts `ExternalId` (sub claim)

## API Communication Example

```typescript
// Frontend
const { data, error } = await viboraApi.games.createGame({
  dateTime: "2025-10-25T19:00:00Z",
  location: "Casa Padel Paris",
  skillLevel: 5,
  maxPlayers: 4
})

// Backend
// 1. Middleware validates JWT (Supabase signature)
// 2. Extracts ExternalId from "sub" claim
// 3. MediatR routes to CreateGameCommandHandler
// 4. Domain: Game.Create(...) → returns Result<Game>
// 5. Repository: AddAsync(game)
// 6. UnitOfWork: SaveChangesAsync() → transaction + publish domain events
// 7. Response: 201 Created { gameId, ... }
```

---

# Project Context

**Vibora MVP** - Padel matchmaking platform targeting French players

**Key Features**:
- **Zero-friction onboarding**: Guest mode allows joining without signup
- **Magic links**: Share via WhatsApp/Telegram with Open Graph previews
- **Auto user sync**: Supabase webhook creates User + reconciles guest participations
- **Mobile-first**: PWA with bottom nav, responsive design, offline support

**Documentation**:
- Backend: `docs/Architecture document.md`, `docs/Backlog du MVP Vibora.md`
- Frontend: `vibora-frontend/QUICK_START.md`, `vibora-frontend/ARCHITECTURE_DIAGRAM.md`, `vibora-frontend/DESIGN_SYSTEM.md`
