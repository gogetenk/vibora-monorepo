## 1. Aperçu

Application **PWA** (Progressive Web App) construite avec **Next.js 15 + React 19**, Tailwind CSS et **Supabase**. Architecture MVP minimaliste focalisée sur la validation rapide du concept de matchmaking padel.

**Principe** : Simplicité maximale pour itérer vite, complexité ajoutée uniquement après validation PMF.

## 2. Stack Technologique MVP

| Couche            | Technologie                                    | Justification MVP                                           |
| ----------------- | ---------------------------------------------- | ----------------------------------------------------------- |
| **Frontend**      | Next.js 15 (App Router) + React 19            | Déploiement Vercel gratuit, PWA intégré, Server Components |
| **UI**            | Tailwind CSS + composants custom              | Pas de dépendance externe, styling rapide                  |
| **State**         | React built-in (useState, useEffect)          | Éviter complexité TanStack Query, debugging simple         |
| **Database**      | Supabase (PostgreSQL + Auth)                  | Free tier généreux, auth gratuit, admin UI                 |
| **Real-time**     | Polling simple (30s refresh)                  | Éviter websockets, suffisant pour matchmaking              |
| **Hosting**       | Vercel (hobby plan)                           | CI/CD gratuit, edge functions incluses                     |
| **Monitoring**    | Console.log + Vercel Analytics                | MVP = pas de Sentry, ajout post-traction                   |

## 3. Architecture Simplifiée

```
Browser PWA ⟷ Next.js App Router ⟷ Supabase PostgreSQL
     ↓              ↓                       ↓
Service Worker  Server Actions       Auth + 3 Tables
     ↓              ↓                       ↓  
  Cache static   API mutations         Basic RLS
```

**Principe** : Chaque couche a UN rôle, pas de redondance.

## 4. Structure Projet MVP

```
src/
├── app/                    # Next.js App Router
│   ├── (auth)/            # Auth pages
│   ├── feed/              # Match feed  
│   ├── create/            # Create match
│   ├── match/[id]/        # Match details
│   └── api/               # Server Actions
├── components/            # UI components (< 10 composants)
│   ├── ui/               # Primitives (Button, Card, Input)
│   └── match/            # Match-specific (MatchCard, LevelChip)
├── lib/                  # Utils
│   ├── supabase.ts       # Client config
│   ├── types.ts          # TypeScript types
│   └── utils.ts          # Helpers
└── styles/               # Tailwind CSS
```

**Règle** : Maximum 50 fichiers pour rester maintenable en solo.

## 5. Base de Données Ultra-Simplifiée

### 5.1 Schéma 3 Tables

```sql
-- 1. USERS (guests + auth)
CREATE TABLE users (
  id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  email         TEXT UNIQUE,                               -- NULL autorisé
  name          TEXT NOT NULL,
  phone         TEXT,
  level         DECIMAL(3,1) CHECK (level >= 1 AND level <= 10),
  is_guest      BOOLEAN DEFAULT true,
  visibility    TEXT DEFAULT 'public' CHECK (visibility IN ('public','private')),
  created_at    TIMESTAMPTZ DEFAULT NOW()
);

-- 2. MATCHES
CREATE TABLE matches (
  id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  creator_id    UUID REFERENCES users(id),
  title         TEXT NOT NULL,
  zone          TEXT NOT NULL,
  datetime      TIMESTAMPTZ NOT NULL,
  level_min     INT CHECK (level_min BETWEEN 1 AND 10),
  level_max     INT CHECK (level_max BETWEEN 1 AND 10),
  status        TEXT DEFAULT 'open' CHECK (status IN ('open','full','cancelled')),
  magic_token   TEXT UNIQUE DEFAULT gen_random_uuid()::TEXT,
  expires_at    TIMESTAMPTZ DEFAULT (NOW() + INTERVAL '24 hours'),
  created_at    TIMESTAMPTZ DEFAULT NOW()
);

-- 3. PARTICIPANTS
CREATE TABLE match_participants (
  id         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  match_id   UUID REFERENCES matches(id) ON DELETE CASCADE,
  user_id    UUID REFERENCES users(id)   ON DELETE CASCADE,
  joined_at  TIMESTAMPTZ DEFAULT NOW()
);

-- 4. FEEDBACK (👍 / 👎)
CREATE TABLE match_feedbacks (
  id         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  match_id   UUID REFERENCES matches(id) ON DELETE CASCADE,
  rater_id   UUID REFERENCES users(id),
  target_id  UUID REFERENCES users(id),
  is_good    BOOLEAN,                      -- TRUE = à mon niveau
  comment    TEXT,
  created_at TIMESTAMPTZ DEFAULT NOW()
);

```

### 5.2 RLS Optimisé

```sql
-- USERS : lecture publique restreinte
ALTER TABLE users ENABLE ROW LEVEL SECURITY;
CREATE POLICY public_profiles
  ON users FOR SELECT
  USING (visibility = 'public');

CREATE POLICY owner_update
  ON users FOR UPDATE
  USING (auth.uid() = id);

-- MATCHES : visibles si 'open' ET non expirés
ALTER TABLE matches ENABLE ROW LEVEL SECURITY;
CREATE POLICY read_open
  ON matches FOR SELECT
  USING (status = 'open' AND expires_at > NOW());

CREATE POLICY creator_write
  ON matches FOR ALL
  USING (auth.uid() = creator_id);


-- Participants: lecture publique, insertion contrôlée
ALTER TABLE match_participants ENABLE ROW LEVEL SECURITY;
CREATE POLICY "public_read" ON match_participants FOR SELECT USING (true);
CREATE POLICY "join_valid_match" ON match_participants FOR INSERT WITH CHECK (
  EXISTS (
    SELECT 1 FROM matches 
    WHERE id = match_id 
    AND status = 'open' 
    AND magic_expires_at > NOW()
  )
);

-- Feedbacks: lecture publique, insertion par participants seulement
ALTER TABLE match_feedbacks ENABLE ROW LEVEL SECURITY;
CREATE POLICY "public_read" ON match_feedbacks FOR SELECT USING (true);
CREATE POLICY "participants_rate" ON match_feedbacks FOR INSERT WITH CHECK (
  EXISTS (
    SELECT 1 FROM match_participants 
    WHERE match_id = match_feedbacks.match_id 
    AND user_id = auth.uid()
  )
);
```

### 5.3 Pas de Triggers/Jobs MVP

**Décision** : Logique métier dans l'application Next.js, pas en base.
- Status "full" calculé côté app quand 4ème joueur rejoint
- Pas de cleanup automatique → manuel si nécessaire
- Messages = feature P1, pas MVP

## 6. API Design (Server Actions)

```typescript
// app/actions/matches.ts
'use server'

import { log } from '@/lib/logger'

export async function createMatch(data: CreateMatchData) {
  try {
    const user = await getCurrentUser()
    const match = await supabase
      .from('matches')
      .insert({
        creator_id: user.id,
        ...data
      })
      .select()
      .single()
    
    log.info('match_created', { userId: user.id, matchId: match.id })
    revalidatePath('/feed')
    return match
  } catch (error) {
    log.error('create_match_failed', error, { userId: user?.id })
    throw error
  }
}

export async function joinMatchAsUser(matchId: string) {
  try {
    const user = await getCurrentUser()
    
    await supabase
      .from('match_participants')
      .insert({
        match_id: matchId,
        user_id: user.id
      })
      
    await updateMatchStatusIfFull(matchId)
    log.info('user_joined_match', { userId: user.id, matchId })
    revalidatePath(`/match/${matchId}`)
  } catch (error) {
    log.error('join_match_failed', error, { userId: user?.id, matchId })
    throw error
  }
}

export async function joinMatchAsGuest(matchId: string, guestData: GuestData) {
  try {
    // Vérifier que le magic link est valide
    const { data: match } = await supabase
      .from('matches')
      .select('magic_expires_at')
      .eq('id', matchId)
      .single()
    
    if (!match || new Date(match.magic_expires_at) < new Date()) {
      throw new Error('Magic link expired')
    }
    
    // Créer utilisateur guest
    const { data: guestUser } = await supabase
      .from('users')
      .insert({
        name: guestData.name,
        phone: guestData.phone,
        level: guestData.level,
        is_guest: true
      })
      .select()
      .single()
    
    // Rejoindre le match
    await supabase
      .from('match_participants')
      .insert({
        match_id: matchId,
        user_id: guestUser.id
      })
      
    await updateMatchStatusIfFull(matchId)
    log.info('guest_joined_match', { guestId: guestUser.id, matchId })
    revalidatePath(`/match/${matchId}`)
    
    return guestUser
  } catch (error) {
    log.error('guest_join_failed', error, { matchId })
    throw error
  }
}

export async function submitMatchFeedback(
  matchId: string, 
  ratings: Array<{ targetId: string; score: number; comment?: string }>
) {
  try {
    const user = await getCurrentUser()
    
    // Insérer tous les feedbacks en batch
    const feedbacks = ratings.map(rating => ({
      match_id: matchId,
      rater_id: user.id,
      target_id: rating.targetId,
      score: rating.score,
      comment: rating.comment
    }))
    
    await supabase
      .from('match_feedbacks')
      .insert(feedbacks)
    
    // Mettre à jour les niveaux des joueurs notés (logique simple)
    await updatePlayerLevelsFromFeedback(ratings)
    
    log.info('feedback_submitted', { userId: user.id, matchId, ratingsCount: ratings.length })
  } catch (error) {
    log.error('feedback_failed', error, { userId: user?.id, matchId })
    throw error
  }
}

async function updateMatchStatusIfFull(matchId: string) {
  const { count } = await supabase
    .from('match_participants')
    .select('*', { count: 'exact', head: true })
    .eq('match_id', matchId)
    
  if (count >= 4) {
    await supabase
      .from('matches')
      .update({ status: 'full' })
      .eq('id', matchId)
  }
}

async function updatePlayerLevelsFromFeedback(ratings: Array<{ targetId: string; score: number }>) {
  // Logique simple: score 4-5 = +0.1 niveau, score 1-2 = -0.1 niveau
  for (const rating of ratings) {
    const adjustment = rating.score >= 4 ? 0.1 : rating.score <= 2 ? -0.1 : 0
    
    if (adjustment !== 0) {
      await supabase.rpc('adjust_user_level', {
        user_id: rating.targetId,
        adjustment
      })
    }
  }
}
```

**Avantage** : Pas d'API routes, Server Actions intégrés, cache Next.js automatique.

## 7. Composants UI Essentiels

```typescript
// components/match/MatchCard.tsx
interface MatchCardProps {
  match: Match
  participants: User[]
}

export function MatchCard({ match, participants }: MatchCardProps) {
  return (
    <div className="border rounded-lg p-4">
      <h3>{match.title}</h3>
      <p>{match.zone} • {formatDate(match.datetime)}</p>
      <div className="flex gap-2">
        <LevelChip level={match.level_min} />
        <ParticipantsList users={participants} max={match.max_players} />
      </div>
      <JoinButton matchId={match.id} />
    </div>
  )
}
```

**Liste complète** : MatchCard, LevelChip, JoinButton, CreateMatchForm, UserAvatar, LoadingSpinner (6 composants core).

## 8. State Management Simplifié

```typescript
// components/MatchList.tsx (Client Component avec smart polling)
'use client'
import { useEffect, useState } from 'react'
import { log } from '@/lib/logger'

export function MatchList({ matches: initialMatches }) {
  const [matches, setMatches] = useState(initialMatches)
  const [isLoading, setIsLoading] = useState(false)
  
  // Smart polling: seulement si onglet actif
  useEffect(() => {
    const refreshMatches = async () => {
      if (document.visibilityState !== 'visible') return
      
      try {
        setIsLoading(true)
        const response = await fetch('/api/matches')
        const freshMatches = await response.json()
        setMatches(freshMatches)
        log.info('matches_refreshed', { count: freshMatches.length })
      } catch (error) {
        log.error('matches_refresh_failed', error)
      } finally {
        setIsLoading(false)
      }
    }
    
    // Polling toutes les 30s, mais seulement si onglet visible
    const interval = setInterval(refreshMatches, 30000)
    
    // Refresh immédiat quand l'onglet redevient visible
    document.addEventListener('visibilitychange', () => {
      if (document.visibilityState === 'visible') {
        refreshMatches()
      }
    })
    
    return () => {
      clearInterval(interval)
      document.removeEventListener('visibilitychange', refreshMatches)
    }
  }, [])
  
  return (
    <div>
      {isLoading && (
        <div className="text-center text-gray-500 mb-4">
          Actualisation...
        </div>
      )}
      {matches.map(match => (
        <MatchCard key={match.id} match={match} />
      ))}
    </div>
  )
}
```

## 9. Magic Links Implementation

```typescript
// app/join/[token]/page.tsx
interface Props {
  params: { token: string }
}

export default async function MagicJoinPage({ params }: Props) {
  const match = await supabase
    .from('matches')
    .select(`
      *,
      match_participants (
        id,
        user_id,
        users (name, level, is_guest)
      )
    `)
    .eq('magic_token', params.token)
    .single()
  
  if (!match) {
    return <div>Match introuvable</div>
  }
  
  // Vérifier expiration magic link
  if (new Date(match.magic_expires_at) < new Date()) {
    return <div>Ce lien a expiré</div>
  }
  
  const participants = match.match_participants.map(p => ({
    id: p.user_id,
    name: p.users.name,
    level: p.users.level,
    isGuest: p.users.is_guest
  }))
  
  return (
    <div>
      <MatchDetails match={match} participants={participants} />
      <GuestJoinForm matchId={match.id} />
      <div className="mt-4 text-center">
        <p>Vous avez déjà un compte ?</p>
        <LoginButton />
      </div>
    </div>
  )
}

// components/GuestJoinForm.tsx
'use client'
export function GuestJoinForm({ matchId }: { matchId: string }) {
  const [formData, setFormData] = useState({ name: '', phone: '', level: 5 })
  const [isSubmitting, setIsSubmitting] = useState(false)
  
  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setIsSubmitting(true)
    
    try {
      await joinMatchAsGuest(matchId, formData)
      // Redirect vers page de confirmation ou match details
    } catch (error) {
      console.error('Erreur lors de la participation:', error)
      // Show error toast
    } finally {
      setIsSubmitting(false)
    }
  }
  
  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <input 
        placeholder="Votre nom"
        value={formData.name}
        onChange={(e) => setFormData(prev => ({ ...prev, name: e.target.value }))}
        required
        disabled={isSubmitting}
      />
      <input 
        placeholder="Téléphone (optionnel)"
        value={formData.phone}
        onChange={(e) => setFormData(prev => ({ ...prev, phone: e.target.value }))}
        disabled={isSubmitting}
      />
      <LevelSelector 
        value={formData.level}
        onChange={(level) => setFormData(prev => ({ ...prev, level }))}
        disabled={isSubmitting}
      />
      <button 
        type="submit" 
        className="w-full bg-green-600 text-white py-2 rounded disabled:opacity-50"
        disabled={isSubmitting}
      >
        {isSubmitting ? 'Participation...' : 'Rejoindre le match'}
      </button>
    </form>
  )
}

// components/FeedbackForm.tsx (NOUVEAU)
'use client'
export function FeedbackForm({ matchId, participants }: { 
  matchId: string
  participants: Array<{ id: string; name: string }>
}) {
  const [ratings, setRatings] = useState<Record<string, { score: number; comment: string }>>({})
  
  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    
    const feedbackData = Object.entries(ratings).map(([targetId, data]) => ({
      targetId,
      score: data.score,
      comment: data.comment
    }))
    
    await submitMatchFeedback(matchId, feedbackData)
    // Redirect ou fermer modal
  }
  
  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <h3>Comment étaient vos partenaires ?</h3>
      {participants.map(player => (
        <div key={player.id} className="border rounded p-4">
          <h4>{player.name}</h4>
          <div className="flex gap-2 my-2">
            {[1,2,3,4,5].map(score => (
              <button
                key={score}
                type="button"
                className={`px-3 py-1 rounded ${
                  ratings[player.id]?.score === score ? 'bg-green-600 text-white' : 'bg-gray-200'
                }`}
                onClick={() => setRatings(prev => ({
                  ...prev,
                  [player.id]: { ...prev[player.id], score }
                }))}
              >
                {score}
              </button>
            ))}
          </div>
          <textarea
            placeholder="Commentaire (optionnel)"
            value={ratings[player.id]?.comment || ''}
            onChange={(e) => setRatings(prev => ({
              ...prev,
              [player.id]: { ...prev[player.id], comment: e.target.value }
            }))}
            className="w-full p-2 border rounded"
          />
        </div>
      ))}
      <button type="submit" className="w-full bg-blue-600 text-white py-2 rounded">
        Envoyer les évaluations
      </button>
    </form>
  )
}

// Génération URL magic link avec expiration
const magicUrl = `https://app.vibora.io/join/${match.magic_token}`
```

## 10. PWA Configuration Minimale

```typescript
// next.config.js
const withPWA = require('next-pwa')({
  dest: 'public',
  disable: process.env.NODE_ENV === 'development',
  runtimeCaching: [
    {
      urlPattern: /^https:\/\/app\.vibora\.io\/feed/,
      handler: 'StaleWhileRevalidate',
      options: {
        cacheName: 'matches-cache',
        expiration: {
          maxEntries: 50,
          maxAgeSeconds: 5 * 60 // 5 minutes
        }
      }
    }
  ]
})

module.exports = withPWA({
  // Next.js config
})
```

```json
// public/manifest.json
{
  "name": "Vibora - Padel Matchmaking",
  "short_name": "Vibora",
  "description": "Trouvez des partenaires de padel instantanément",
  "start_url": "/",
  "display": "standalone",
  "background_color": "#ffffff",
  "theme_color": "#10b981",
  "icons": [
    {
      "src": "/icon-192.png",
      "sizes": "192x192",
      "type": "image/png"
    }
  ]
}
```

## 11. Géolocalisation Simplifiée

**MVP Decision** : Pas de coordonnées GPS, zones textuelles prédéfinies.

```typescript
// lib/logger.ts - Logging structuré minimal
interface LogContext {
  userId?: string
  matchId?: string
  guestId?: string
  [key: string]: any
}

export const log = {
  info: (action: string, context?: LogContext) => {
    const logData = {
      level: 'info',
      action,
      timestamp: new Date().toISOString(),
      ...context
    }
    
    console.log(`[INFO] ${action}`, logData)
    
    // En production: envoyer vers Supabase ou service de logs
    if (process.env.NODE_ENV === 'production') {
      // TODO: Implementer envoi vers logs centralisés
    }
  },
  
  error: (action: string, error: Error, context?: LogContext) => {
    const logData = {
      level: 'error',
      action,
      error: error.message,
      stack: error.stack,
      timestamp: new Date().toISOString(),
      ...context
    }
    
    console.error(`[ERROR] ${action}`, logData)
    
    // En production: Sentry ou autre service d'erreurs
    if (process.env.NODE_ENV === 'production') {
      // Sentry.captureException(error, { extra: logData })
    }
  },
  
  warn: (action: string, context?: LogContext) => {
    const logData = {
      level: 'warn',
      action,
      timestamp: new Date().toISOString(),
      ...context
    }
    
    console.warn(`[WARN] ${action}`, logData)
  }
}

// Exemples d'usage:
// log.info('user_joined_match', { userId: '123', matchId: 'abc' })
// log.error('database_connection_failed', error, { query: 'SELECT...' })
```

**Post-MVP** : Migration vers coordonnées GPS si nécessaire.

## 12. Notifications Strategy

| Platform | MVP Solution                    | Post-MVP                      |
| -------- | ------------------------------- | ----------------------------- |
| Android  | PWA Push Notifications          | React Native push             |
| iOS      | Email notifications uniquement  | React Native push obligatoire |
| Desktop  | Browser notifications           | N/A                           |

```typescript
// lib/notifications.ts (MVP)
export async function sendEmailNotification(userId: string, message: string) {
  // Supabase Edge Function qui envoie email via Resend
  await supabase.functions.invoke('send-email', {
    body: { userId, message }
  })
}
```

## 13. Testing Strategy MVP

```typescript
// tests/basic.test.ts - Tests critiques uniquement
import { render, screen } from '@testing-library/react'
import { MatchCard } from '@/components/match/MatchCard'

test('affiche les infos du match', () => {
  const match = { title: 'Match test', zone: 'Paris 15e' }
  render(<MatchCard match={match} participants={[]} />)
  expect(screen.getByText('Match test')).toBeInTheDocument()
})

// 5-10 tests max, focus sur les user flows critiques
```

## 14. Déploiement & Environnements

| Env   | URL                           | Config                      |
| ----- | ----------------------------- | --------------------------- |
| Dev   | localhost:3000                | Local Supabase              |
| Prod  | https://app.vibora.io         | Supabase prod + Vercel prod |

**Pas de staging** pour MVP → déploiement direct en prod avec feature flags si nécessaire.

## 15. Performance Targets MVP

| Métrique              | Cible MVP | Mesure                    |
| --------------------- | --------- | ------------------------- |
| First Load Time       | < 2s      | Vercel Analytics          |
| PWA Install prompt    | > 80%     | Manual testing            |
| Offline functionality | Basic     | Service worker cache      |
| Mobile responsiveness | 100%      | Manual testing + Devtools |

## 16. Migration Path Post-MVP

**Si PMF validé** → Roadmap technique :

1. **v1.1** : TanStack Query + Supabase Realtime
2. **v1.2** : Géolocalisation GPS + PostGIS  
3. **v2.0** : React Native app + notifications push
4. **v2.1** : Microservices + monitoring avancé

## 17. Risques Techniques Identifiés

| Risque                    | Impact | Mitigation                        |
| ------------------------- | ------ | --------------------------------- |
| PWA adoption iOS          | Medium | Email notifications + React Native si traction |
| Supabase free tier limit | High   | Monitoring usage + upgrade à 25€/mois         |
| Polling performance       | Low    | Migration vers Realtime si > 1000 users       |
| Magic links sécurité     | Medium | UUID cryptographiquement sûr + expiration     |

## 18. Conventions Code MVP

```typescript
// Naming
- Composants: PascalCase (MatchCard)  
- Fichiers: kebab-case (match-card.tsx)
- Variables: camelCase (matchId)
- Base: snake_case (match_participants)

// Architecture
- 1 feature = 1 dossier max
- Server Components par défaut
- Client Components explicites ('use client')
- Server Actions pour mutations
```

## 19. Checklist MVP Ready

- [ ] 4 tables Supabase créées + RLS optimisé (users unifiés, matches, match_participants, match_feedbacks)
- [ ] Magic links avec expiration 24h implémentée
- [ ] Auth email magic link Supabase  
- [ ] Pages : /feed, /create, /match/[id], /join/[token], /feedback/[matchId]
- [ ] Composants : MatchCard, CreateForm, GuestJoinForm, FeedbackForm, LoginButton
- [ ] Smart polling# Vibora – Architecture Technique MVP Simplifiée


**Temps estimé développement solo** : 4-5 semaines (au lieu de 3-4) pour inclure ghost users flow.

---

> *Document Architecture MVP v2.0 – 27 juillet 2025*  
> *Simplifié pour validation rapide, complexité ajoutée post-PMF*