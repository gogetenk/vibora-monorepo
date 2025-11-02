# 🏗️ Vibora Frontend Architecture

## 📐 Vue d'Ensemble du Système

```
┌─────────────────────────────────────────────────────────────────────┐
│                         VIBORA MVP FRONTEND                         │
│                        Next.js 15 + React 19                        │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                          USER INTERFACE                             │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐            │
│  │ /auth/login  │  │ /auth/signup │  │ /auth/callback│            │
│  │              │  │              │  │               │            │
│  │ • Email/Pass │  │ • Formulaire │  │ • OAuth redir │            │
│  │ • Magic Link │  │ • OAuth      │  │ • Magic Link  │            │
│  │ • OAuth      │  │ • Mode Invité│  │   validation  │            │
│  │ • Mode Invité│  └──────────────┘  └──────────────┘            │
│  └──────────────┘                                                  │
│                                                                     │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐            │
│  │ / (Home)     │  │ /my-games    │  │ /games/[id]  │            │
│  │              │  │              │  │               │            │
│  │ • Liste      │  │ • Mes parties│  │ • Détail      │            │
│  │   parties    │  │ • Host/Guest │  │ • Rejoindre   │            │
│  │ • Filtres    │  │ • Actions    │  │ • Quitter     │            │
│  └──────────────┘  └──────────────┘  └──────────────┘            │
│                                                                     │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐            │
│  │ /create-game │  │ /join/[token]│  │ /settings    │            │
│  │              │  │              │  │   /profile   │            │
│  │ • Google     │  │ • Magic Link │  │              │            │
│  │   Places     │  │ • Guest Form │  │ • Edit       │            │
│  │ • Formulaire │  │ • Join       │  │ • Photo      │            │
│  └──────────────┘  └──────────────┘  └──────────────┘            │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
                                  │
                                  │ Client-Side Routing
                                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│                         MIDDLEWARE LAYER                            │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  middleware.ts                                                      │
│  ├─ Check Auth Status (Supabase Session)                          │
│  ├─ Protected Routes: /my-games, /create-game, /settings          │
│  ├─ Redirect to /auth/login if not authenticated                  │
│  └─ Extract ExternalId from JWT (user.id)                         │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
                     │                            │
         ┌───────────┴────────┐      ┌───────────┴────────┐
         │ Authenticated      │      │ Unauthenticated    │
         │ (JWT Present)      │      │ (No JWT)           │
         ▼                    │      ▼                    │
┌─────────────────────────────▼───────────────────────────▼───────────┐
│                      AUTHENTICATION LAYER                           │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  lib/auth/supabase-auth.ts                                         │
│  ┌──────────────────────────────────────────────────────────────┐ │
│  │  Authentication Functions                                     │ │
│  │                                                              │ │
│  │  • signUp(email, password, firstName, lastName)             │ │
│  │  • signIn(email, password)                                  │ │
│  │  • signInWithMagicLink(email)                               │ │
│  │  • signInWithOAuth(provider: 'google' | 'apple')            │ │
│  │  • signOut()                                                │ │
│  │  • getSession() → { session, user }                         │ │
│  │  • getCurrentUserExternalId() → UUID                        │ │
│  │                                                              │ │
│  └──────────────────────────────────────────────────────────────┘ │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
                                  │
                    ┌─────────────┴─────────────┐
                    │                           │
                    ▼                           ▼
         ┌──────────────────┐        ┌──────────────────┐
         │  Supabase Auth   │        │  JWT Token       │
         │                  │        │                  │
         │  • User Session  │        │  • access_token  │
         │  • OAuth Flow    │        │  • sub = ExtId   │
         │  • Magic Links   │        │  • email         │
         │  • Refresh Token │        │  • exp           │
         └──────────────────┘        └──────────────────┘
                    │                           │
                    │                           │ Used in Headers
                    ▼                           ▼
┌─────────────────────────────────────────────────────────────────────┐
│                         API CLIENT LAYER                            │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  lib/api/vibora-client.ts                                          │
│  ┌──────────────────────────────────────────────────────────────┐ │
│  │  HTTP Client (Fetch Wrapper)                                 │ │
│  │                                                              │ │
│  │  • getViboraAuthHeaders() → JWT from Supabase              │ │
│  │  • fetchVibora<T>(endpoint, options) → ApiResponse<T>      │ │
│  │  • Error handling: { data?, error? }                       │ │
│  │                                                              │ │
│  └──────────────────────────────────────────────────────────────┘ │
│                                                                     │
│  ┌──────────────────────────────────────────────────────────────┐ │
│  │  viboraApi                                                   │ │
│  │                                                              │ │
│  │  games:                                                      │ │
│  │    • getAvailableGames(query?)                              │ │
│  │    • getMyGames()                                           │ │
│  │    • getGameDetails(id)                                     │ │
│  │    • createGame(request)                                    │ │
│  │    • joinGame(id)                                           │ │
│  │    • joinGameAsGuest(id, { name, phone, email })           │ │
│  │    • leaveGame(id)                                          │ │
│  │    • cancelGame(id)                                         │ │
│  │                                                              │ │
│  │  shares: (Magic Links)                                       │ │
│  │    • createGameShare(gameId)                                │ │
│  │    • getShareByToken(token)                                 │ │
│  │    • getShareMetadata(token)                                │ │
│  │                                                              │ │
│  │  users:                                                      │ │
│  │    • getCurrentUserProfile()                                │ │
│  │    • updateProfile(request)                                 │ │
│  │    • uploadProfilePhoto(file)                               │ │
│  │    • getUserPublicProfile(externalId)                       │ │
│  │    • claimGuestParticipations()                             │ │
│  │                                                              │ │
│  └──────────────────────────────────────────────────────────────┘ │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
                                  │
                                  │ HTTP/JSON
                                  │ Headers: Authorization: Bearer {JWT}
                                  │
                                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│                       VIBORA BACKEND (.NET)                         │
│                    http://localhost:5000 (dev)                      │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌──────────────────────────────────────────────────────────────┐ │
│  │  API Endpoints (25 endpoints)                                │ │
│  │                                                              │ │
│  │  Games Module (13):                                          │ │
│  │    • GET    /games                                           │ │
│  │    • GET    /games/me                                        │ │
│  │    • GET    /games/{id}                                      │ │
│  │    • POST   /games                                           │ │
│  │    • POST   /games/{id}/players                              │ │
│  │    • POST   /games/{id}/players/guest (PUBLIC)               │ │
│  │    • DELETE /games/{id}/players                              │ │
│  │    • POST   /games/{id}/cancel                               │ │
│  │    • POST   /games/{id}/shares                               │ │
│  │    • GET    /shares/{token} (PUBLIC)                         │ │
│  │    • GET    /shares/{token}/metadata (PUBLIC)                │ │
│  │    • POST   /games/guest-participations/by-contact           │ │
│  │    • POST   /games/guest-participations/convert              │ │
│  │                                                              │ │
│  │  Users Module (9):                                           │ │
│  │    • POST   /users/sync (Webhook Supabase)                   │ │
│  │    • POST   /users/guest                                     │ │
│  │    • GET    /users/me                                        │ │
│  │    • PUT    /users/me                                        │ │
│  │    • GET    /users/profile                                   │ │
│  │    • PUT    /users/profile (+ photo upload)                  │ │
│  │    • GET    /users/{externalId}/profile                      │ │
│  │    • GET    /users/{externalId}                              │ │
│  │    • POST   /users/claim-guest-participations                │ │
│  │                                                              │ │
│  └──────────────────────────────────────────────────────────────┘ │
│                                                                     │
│  ┌──────────────────────────────────────────────────────────────┐ │
│  │  Middleware Pipeline                                         │ │
│  │                                                              │ │
│  │  1. CORS              → AllowOrigin: localhost:3000         │ │
│  │  2. Authentication    → Validate JWT (RS256)                │ │
│  │  3. Authorization     → Check user permissions              │ │
│  │  4. MediatR Router    → CQRS Commands/Queries               │ │
│  │                                                              │ │
│  └──────────────────────────────────────────────────────────────┘ │
│                                                                     │
│  ┌──────────────────────────────────────────────────────────────┐ │
│  │  Architecture                                                │ │
│  │                                                              │ │
│  │  • MediatR (CQRS)                                            │ │
│  │  • MassTransit (Events)                                      │ │
│  │  • PostgreSQL + EF Core                                      │ │
│  │  • JWT Authentication (Supabase Issuer)                      │ │
│  │  • Cross-module Services (IGamesServiceClient)               │ │
│  │                                                              │ │
│  └──────────────────────────────────────────────────────────────┘ │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
                                  │
                                  │ EF Core
                                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│                       DATABASE (PostgreSQL)                         │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  Tables:                                                            │
│  • Users (ExternalId, FirstName, LastName, Email, PhoneNumber)     │
│  • Games (Id, DateTime, Location, SkillLevel, Status, HostId)      │
│  • Participations (UserId, GameId, IsHost, JoinedAt)               │
│  • GuestParticipants (Name, PhoneNumber, Email, GameId)            │
│  • GameShares (ShareToken, GameId, ViewCount, CreatedAt)           │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 🔄 Flow d'Authentification Complet

```
┌──────────┐
│  User    │
│  Browser │
└────┬─────┘
     │
     │ 1. Visit /auth/signup
     ▼
┌─────────────────┐
│ Signup Page     │
│                 │
│ [Email]         │
│ [Password]      │
│ [First Name]    │
│ [Last Name]     │
│                 │
│ [Créer compte]  │
└────┬────────────┘
     │
     │ 2. signUp({ email, password, firstName, lastName })
     ▼
┌─────────────────────────┐
│  Supabase Auth          │
│                         │
│  • Create User Account  │
│  • Generate JWT         │
│  • Trigger Webhook      │
└────┬────────────────────┘
     │
     │ 3. Webhook: POST /users/sync
     │    Body: { externalId, email, firstName, lastName }
     ▼
┌─────────────────────────────────┐
│  Vibora Backend                 │
│                                 │
│  SyncUserFromAuthCommandHandler │
│  • Create User in DB            │
│  • Auto-claim guest parts       │
│  • Return success               │
└────┬────────────────────────────┘
     │
     │ 4. Success
     ▼
┌─────────────────┐
│ Frontend        │
│                 │
│ • Store session │
│ • Redirect /    │
│ • Show toast    │
└────┬────────────┘
     │
     │ 5. User authenticated
     ▼
┌─────────────────────────────┐
│ Subsequent API Calls        │
│                             │
│ Headers:                    │
│   Authorization: Bearer JWT │
│                             │
│ Backend validates:          │
│   • JWT signature (RS256)   │
│   • Expiry                  │
│   • Extract ExternalId      │
└─────────────────────────────┘
```

---

## 🔐 JWT Flow Detail

```
┌────────────────────────────────────────────────────────────────┐
│                         JWT TOKEN                              │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│  Header:                                                       │
│  {                                                             │
│    "alg": "RS256",           ← Supabase signature             │
│    "typ": "JWT"                                                │
│  }                                                             │
│                                                                │
│  Payload:                                                      │
│  {                                                             │
│    "sub": "550e8400-e29b-41d4-a716-446655440000",  ← ExternalId│
│    "email": "user@example.com",                                │
│    "aud": "authenticated",                                     │
│    "exp": 1234567890,                                          │
│    "iss": "https://xxx.supabase.co/auth/v1"                    │
│  }                                                             │
│                                                                │
│  Signature:                                                    │
│    RSASHA256(                                                  │
│      base64UrlEncode(header) + "." +                           │
│      base64UrlEncode(payload),                                 │
│      Supabase_Private_Key                                      │
│    )                                                           │
│                                                                │
└────────────────────────────────────────────────────────────────┘
              │                              │
              │ Frontend                     │ Backend
              ▼                              ▼
   ┌────────────────────┐       ┌────────────────────┐
   │ getSession()       │       │ Validate JWT       │
   │ → access_token     │       │ • Check signature  │
   │                    │       │ • Check expiry     │
   │ API Calls:         │       │ • Extract sub      │
   │ Authorization:     │       │                    │
   │   Bearer {token}   │       │ → ExternalId (Guid)│
   └────────────────────┘       └────────────────────┘
```

---

## 🎯 Data Flow: Create Game Example

```
User Action                Frontend                  Backend                Database
────────────────────────────────────────────────────────────────────────────────

[Click FAB "+"]
     │
     ▼
[Open /create-game]
     │
     ▼
[Fill Form]
  • Date/Time ───────────┐
  • Location (Places) ───┤
  • Skill Level ─────────┤
  • Max Players ─────────┘
     │
     ▼
[Submit]
     │
     │ viboraApi.games.createGame({
     │   dateTime: "2025-10-25T19:00:00Z",
     │   location: "Casa Padel Paris",
     │   skillLevel: 5,
     │   maxPlayers: 4
     │ })
     │
     │ POST /games
     │ Headers:
     │   Authorization: Bearer {JWT}
     │   Content-Type: application/json
     │
     ├──────────────────────▶  Middleware
                                   │
                                   ▼
                              Validate JWT
                                   │
                                   ▼
                              Extract ExternalId
                              (550e8400-...)
                                   │
                                   ▼
                              CreateGameCommandHandler
                                   │
                                   ├─▶ Validate User exists
                                   │
                                   ├─▶ Create Game
                                   │     • DateTime
                                   │     • Location
                                   │     • SkillLevel
                                   │     • MaxPlayers
                                   │     • Status = Open
                                   │     • HostExternalId
                                   │
                                   ├─▶ Add Host as Participant
                                   │     • IsHost = true
                                   │     • JoinedAt = Now
                                   │
                                   └─▶ Save to DB ──▶  INSERT INTO Games
                                                       INSERT INTO Participations
                                   │
                                   ▼
                              Return GameDto
     ◀──────────────────────  {
                                gameId: "abc-123",
                                message: "Game created"
                              }
     │
     ▼
Toast: "Partie créée !"
     │
     ▼
Redirect: /games/abc-123
     │
     ▼
[Game Detail Page]
```

---

## 🌐 Magic Links Flow

```
User 1 (Host)              User 2 (Guest)              Backend
──────────────────────────────────────────────────────────────────

[Create Game]
     │
     ▼
[Click "Partager"]
     │
     │ POST /games/{id}/shares
     ├────────────────────────▶ Create GameShare
     │                              • ShareToken (8 chars)
     │                              • GameId
     │                              • CreatedByExternalId
     │                              • ViewCount = 0
     │
     ◀────────────────────────  { shareToken, shareUrl }
     │
     │ shareUrl = "vibora.app/join/abc12345"
     │
     ▼
[Copy Link / Share WhatsApp]
     │
     │ Send to User 2 ────────▶
                                    │
                                    │ [Click Link]
                                    │
                                    ▼
                               [Open /join/abc12345]
                                    │
                                    │ GET /shares/abc12345
                                    ├──────────────▶ GetShareByToken
                                    │                    • Find GameShare
                                    │                    • Increment ViewCount
                                    │                    • Return Game details
                                    │
                                    ◀──────────────  GameDto
                                    │
                                    ▼
                               [Display Game Card]
                                    • Date/Time
                                    • Location
                                    • Players: 1/4
                                    
                               [Guest Form]
                                    • Prénom: "Marc"
                                    • Phone: "0612345678"
                                    
                                    │
                                    │ [Click "Participer"]
                                    │
                                    │ POST /games/{id}/players/guest
                                    │ Body: { name, phoneNumber }
                                    │
                                    ├──────────────▶ JoinGameAsGuest
                                    │                    • Validate
                                    │                    • Create GuestParticipant
                                    │                    • Increment CurrentPlayers
                                    │
                                    ◀──────────────  Success
                                    │
                                    ▼
                               [Redirect /join/success]
                                    
                               [Modal after 3s]
                               "🎉 Créez un compte
                                pour suivre vos parties"
                                    
                               [Create Account]  [Plus tard]
```

---

## 📊 File Structure

```
vibora-next/
├── app/
│   ├── auth/
│   │   ├── login/page.tsx          ✅ Email/Password + OAuth + Magic Link
│   │   ├── signup/page.tsx         ✅ Inscription + OAuth
│   │   └── callback/page.tsx       ✅ OAuth/Magic Link callback
│   │
│   ├── games/
│   │   └── [id]/page.tsx           🔜 Détail partie (mock data)
│   │
│   ├── create-game/page.tsx        🔜 Création (mock data)
│   ├── my-games/page.tsx           🔜 Mes parties (mock data)
│   ├── join/
│   │   ├── [token]/page.tsx        🔜 Magic Link page
│   │   └── success/page.tsx        🔜 Success + modal conversion
│   │
│   ├── settings/
│   │   └── profile/page.tsx        🔜 Édition profil
│   │
│   └── page.tsx                    🔜 Accueil (mock data)
│
├── lib/
│   ├── api/
│   │   ├── vibora-types.ts         ✅ Types DTO (156 lignes)
│   │   └── vibora-client.ts        ✅ Client HTTP (273 lignes)
│   │
│   ├── auth/
│   │   └── supabase-auth.ts        ✅ Auth wrappers (181 lignes)
│   │
│   ├── animation-variants.ts       ✅ Framer Motion variants
│   └── supabase-client.ts          ✅ Supabase client
│
├── components/
│   └── ui/                          ✅ Design system complet
│
├── middleware.ts                    ✅ Route protection (108 lignes)
│
├── IMPLEMENTATION_PROGRESS.md       ✅ Suivi technique
├── FRONTEND_DEVELOPER_GUIDE.md      ✅ Guide développeur
├── EXECUTIVE_SUMMARY.md             ✅ Vue exécutive
├── PO_CHECKLIST.md                  ✅ Tests & validation
├── ENV_TEMPLATE.md                  ✅ Configuration
├── README_SESSION.md                ✅ Vue d'ensemble
└── ARCHITECTURE_DIAGRAM.md          ✅ Ce document

Legend:
✅ = Implemented (Phase 1 & 2)
🔜 = To do (Phase 3, 4, 5)
```

---

## 🎯 Key Design Decisions

### ✅ Decision 1: Google Places Frontend Only
- **PO Approved**
- No backend Clubs module for MVP
- Direct Google Places API in frontend
- Store: Plain text in `Game.Location` (string)
- **Rationale:** 1 day vs 5 days, "Less is More"

### ✅ Decision 2: Guest Mode Zero Friction
- **PO Approved**
- No auth required to view parties
- Magic Links allow joining without account
- Soft conversion modal post-party
- **Rationale:** Reduce adoption friction, compete with WhatsApp

### ✅ Decision 3: Supabase Auth
- **PO Approved**
- OAuth (Google/Apple) for easy signup
- Magic Links for passwordless login
- Auto-sync backend via webhook
- **Rationale:** Industry standard, reliable, scalable

---

**🏗️ Architecture solide, extensible, et conforme "Less is More" ! 🚀**
