# Backlog MVP Vibora

**Dernière mise à jour:** 2025-10-30 | **Audit PO:** 10 agents parallèles

---

## 📊 Vue d'Ensemble MVP

| Epic | Statut | Bloquants MVP |
|------|--------|---------------|
| Comptes Utilisateurs & Profils | ✅ 100% | 0 |
| Gestion des Parties | 🔶 80% | 1 (Google Places) |
| Notifications & Rappels | 🔴 10% | 1 (Push Notifications) |
| Plateforme Mobile (PWA) | 🔴 0% | 1 (Manifest + SW) |
| Marketing & Lancement | 🔴 60% | 1 (Pages légales RGPD) |
| UI/UX Design | 🔴 50% | 1 (Design System) |
| Bot WhatsApp/Telegram | ❌ Post-MVP | 0 |
| Récompenses & Fidélisation | ❌ Post-MVP | 0 |

**🔴 Total bloquants critiques MVP:** 5

---

## Epic 1: Comptes Utilisateurs & Profils

**Statut:** ✅ **COMPLÉTÉ** (Backend + Frontend)

### US01 - Inscription & Connexion Utilisateur
**Statut:** ✅ Complété | **Priorité:** Haute | **Taille:** M

**Description:** Création de compte via Supabase Auth (email/Google/Apple) avec skillLevel 1-10.

**Backend:**
- ✅ Webhook `/users/sync` (Edge Function Supabase v5)
- ✅ JWT validation middleware
- ✅ Auto-réconciliation guest participations

**Frontend:**
- ✅ Pages `/auth/signup`, `/auth/login` (Supabase SDK)
- ✅ OAuth Google/Apple
- ✅ Middleware protection routes privées

---

### US02 - Profil Joueur (Édition & Consultation)
**Statut:** ✅ Complété | **Priorité:** Haute | **Taille:** S

**Description:** Éditer profil (nom, photo, niveau, bio) et consulter profil public autres joueurs.

**Backend:**
- ✅ `GET /users/profile` (current user)
- ✅ `PUT /users/profile` (édition + upload photo multipart)
- ✅ `GET /users/{externalId}/profile` (profil public)

**Frontend:**
- ✅ Page `/settings/profile` (formulaire édition)
- ✅ Page `/users/[externalId]` (profil public)
- ✅ Upload photo avec preview

---

### US03 - Statistiques de Participation
**Statut:** ⚠️ Partiel (Post-MVP) | **Priorité:** Faible | **Taille:** M

**Description:** Afficher taux présence, annulations, score fiabilité sur profil joueur.

**Implémenté:**
- ✅ `GamesPlayedCount` uniquement

**Manquant (Post-MVP):**
- ❌ `AttendanceStatus` (Confirmed/NoShow) + endpoint `POST /games/{id}/attendance`
- ❌ Métriques: `AttendanceRate`, `CancellationsCount`, `ReliabilityScore`
- ❌ Badge "joueur vertueux" (>= 85% présence + >= 5 parties)

---

## Epic 2: Gestion des Parties (Matchmaking)

**Statut:** 🔶 **80% COMPLÉTÉ** | **Bloquants:** 1 (Google Places)

### US04 - Création de Partie
**Statut:** ✅ Backend + Frontend complets | **Priorité:** Haute | **Taille:** M

**Description:** Créer partie avec date/heure, lieu, niveau, max joueurs.

**Backend:**
- ✅ `POST /games` (hôte auto-ajouté en premier participant)

**Frontend:**
- ✅ Page `/create-game` avec FAB centrale "+"
- ✅ Formulaire simplifié (date, lieu, niveau optionnel, maxPlayers=4)
- ✅ Validation frontend (date future, location requis)
- 🔴 **MANQUE:** Google Places Autocomplete (input texte basique seulement)

**🔴 Tâche bloquante (Taille: S):**
- [ ] Installer `@googlemaps/js-api-loader`
- [ ] Composant `<LocationInput>` avec autocomplete Google Places
- [ ] Fichier: `app/create-game/page.tsx`

---

### US05 - Liste des Parties Disponibles
**Statut:** ✅ Complété | **Priorité:** Haute | **Taille:** M

**Description:** Afficher liste parties ouvertes avec filtres (location, skillLevel, date).

**Backend:**
- ✅ `GET /games` (query params: location, skillLevel, fromDate, toDate, pageNumber, pageSize)

**Frontend:**
- ✅ Page `/` (home page)
- ✅ Liste parties avec dates simplifiées ("Aujourd'hui 19h", "Demain")
- ✅ Section "Vos prochaines parties" si authentifié
- ✅ Mode invité (accès sans auth)
- ✅ Navigation bottom bar (2 icônes + FAB centrale)

---

### US06 - Détail d'une Partie
**Statut:** ✅ Complété | **Priorité:** Haute | **Taille:** S

**Description:** Consulter détails partie (lieu, joueurs inscrits, hôte, status).

**Backend:**
- ✅ `GET /games/{id}`

**Frontend:**
- ✅ Page `/games/[id]`
- ✅ Affichage participants unifiés (users + guests)
- ✅ Boutons contextuels (Rejoindre/Quitter selon statut)

---

### US07 - Rejoindre une Partie
**Statut:** ✅ Complété | **Priorité:** Haute | **Taille:** M

**Description:** Rejoindre partie ouverte (user authentifié ou guest).

**Backend:**
- ✅ `POST /games/{id}/players` (authenticated user)
- ✅ `POST /games/{id}/players/guest` (guest avec name, phone/email)

**Frontend:**
- ✅ Bouton "Rejoindre" dans page détail
- ✅ Formulaire guest si non connecté
- ✅ Toast confirmation

---

### US08 - Quitter une Partie
**Statut:** ✅ Complété | **Priorité:** Moyenne | **Taille:** S

**Description:** Se désinscrire d'une partie avant son début.

**Backend:**
- ✅ `DELETE /games/{id}/players`

**Frontend:**
- ✅ Bouton "Quitter" avec modal confirmation
- ✅ Toast + redirection `/my-games`

---

### US09 - Mes Parties (Tableau de bord)
**Statut:** ✅ Complété | **Priorité:** Haute | **Taille:** S

**Description:** Liste des parties créées ou rejointes par l'utilisateur.

**Backend:**
- ✅ `GET /games/me`

**Frontend:**
- ✅ Page `/my-games`
- ✅ Section home page "Vos prochaines parties" (max 3)
- ✅ États vides avec CTA "Créer partie"

---

### US10 - Partage de Partie (Magic Links)
**Statut:** ✅ Complété | **Priorité:** Haute | **Taille:** M

**Description:** Générer lien partage avec Open Graph pour WhatsApp/Telegram.

**Backend:**
- ✅ `POST /games/{id}/shares` (génère token)
- ✅ `GET /shares/{token}` (récupère game + metadata)
- ✅ `GET /shares/{token}/metadata` (Open Graph SSR)

**Frontend:**
- ✅ Bouton "Partager" dans page détail
- ✅ Page `/join/[token]` (affichage partie + formulaire guest)
- ✅ Page `/join/success` (confirmation + modal conversion)
- ✅ Open Graph meta tags dynamiques (1200x630px)
- ✅ États: token expiré, partie complète, token invalide

---

### US10.1 - Message Contextualisé au Départ ⭐ NOUVEAU
**Statut:** 🔜 À implémenter | **Priorité:** Haute | **Taille:** M

**Description:** Indiquer raison de départ (empêchement perso / terrain fermé / météo) pour clarifier aux autres joueurs.

**Justification:** Remplace US11 (Cancel Game) supprimée. Hôte = simple initiateur sans légitimité à annuler pour tous. Maximise matchs joués (autonomie > paternalisme).

**Backend:**
- [ ] Créer enum `LeaveReason` (Personal, VenueClosed, Weather, Other)
- [ ] Modifier `RemoveParticipantCommand` (ajouter `LeaveReason? reason`, `string? message`)
- [ ] Modifier `ParticipationRemovedDomainEvent` (propager raison + message)
- [ ] Adapter notifications selon raison:
  - Personal → "Pierre a quitté (empêchement perso). Vous pouvez continuer (3/4)"
  - VenueClosed → "⚠️ Pierre a quitté - Terrain indisponible"
  - Weather → "Pierre a quitté (météo). À vous de décider (3/4)"

**Frontend:**
- [ ] Composant `<LeaveGameDialog>` (radio group + textarea optionnel 150 chars)
- [ ] Intégrer dans `/games/[id]/page.tsx` (remplacer action "Quitter" directe)
- [ ] Modifier `viboraApi.games.leaveGame()` pour accepter `{ reason?, message? }`

**Fichiers:**
- Backend: `Domain/LeaveReason.cs`, `Commands/RemoveParticipant/*`, `Events/ParticipationRemovedDomainEvent.cs`
- Frontend: `components/games/LeaveGameDialog.tsx`, `app/games/[id]/page.tsx`, `lib/api/vibora-client.ts`

---

### ~~US11 - Annuler une Partie~~ ❌ SUPPRIMÉE
**Décision PO 2025-10-30:** L'hôte n'a aucune légitimité à annuler unilatéralement (pas de paiement/réservation). Maximiser matchs joués > pouvoir arbitraire. Remplacée par US10.1 (message contextualisé).

**Référence:** Analyse comparative 3 scénarios (empêchement perso 80%, météo 15%, terrain fermé 5%) - Host Leave vainqueur dans 95% des cas.

---

## Epic 3: Notifications & Rappels

**Statut:** 🔴 **10% COMPLÉTÉ** | **Bloquant:** Push Notifications

### US12 - Notifications Push ⚠️ BLOQUANT MVP
**Statut:** Backend stub / Frontend 0% | **Priorité:** Critique | **Taille:** XL

**Description:** Système complet de notifications push temps réel pour 5 événements critiques MVP.

**📋 RÉFÉRENCE COMPLÈTE:** `docs/NOTIFICATIONS_PUSH_MVP_LISTE_EXHAUSTIVE.md` (analyse exhaustive 30 Oct 2025)

**5 Notifications Critiques MVP:**
1. **N01 - Nouveau participant rejoint** (hôte + tous les participants existants)
2. **N02 - Participant quitte avec raisons** (hôte + participants restants - nécessite US10.1)
3. **N03 - Partie complète (4/4)** (hôte uniquement)
4. **N04 - Rappel 2h avant** (tous les participants - nécessite Hangfire)
5. **N05 - Partie annulée** (tous les participants - déjà complet backend)

**Backend Infrastructure (Taille: M):**
- [ ] Implémenter `FcmNotificationChannel.SendAsync()` (Firebase Admin SDK)
- [ ] Configuration `FirebaseAdmin` dans `Program.cs`
- [ ] Endpoint `POST /users/device-tokens` (enregistrer FCM token)
- [ ] Tests unitaires + E2E Firebase

**Backend Notifications Spécifiques (Taille: L):**
- [ ] Modifier `PlayerJoinedEventConsumer` (notifier tous les participants, pas juste hôte)
- [ ] Modifier `ParticipationRemovedEventConsumer` (multi-destinataires + raisons US10.1)
- [ ] Créer `GameCompletedDomainEvent` + consumer + template
- [ ] Installer Hangfire + créer `GameReminderJob` (rappels 2h avant)
- [ ] Enrichir templates avec messages contextualisés

**Frontend FCM Complet (Taille: L):**
- [ ] Créer projet Firebase Console
- [ ] Installer package `firebase`
- [ ] Service worker `public/firebase-messaging-sw.js`
- [ ] Config `lib/firebase-config.ts`
- [ ] Hook `useNotifications` (permissions + device tokens)
- [ ] Composant `<NotificationPermissionPrompt>` (après 1ère action)
- [ ] Enregistrement service worker dans `app/layout.tsx`
- [ ] Gestion device tokens (récupération + envoi backend)
- [ ] Deep links (click notification → ouvre game detail)
- [ ] Tests multi-devices (iOS Safari, Android Chrome, Desktop)

**⚠️ DÉPENDANCES:**
- **US10.1 (Message contextualisé départ)** doit être complétée AVANT N02

**Fichiers:**
- Backend: `Vibora.Notifications/Infrastructure/FcmNotificationChannel.cs`, `Program.cs`
- Backend Consumers: `PlayerJoinedEventConsumer.cs`, `ParticipationRemovedEventConsumer.cs`, `GameCompletedEventConsumer.cs`, `GameStartingSoonEventConsumer.cs`
- Backend Jobs: `Vibora.Games/Infrastructure/Jobs/GameReminderJob.cs`
- Frontend: `public/firebase-messaging-sw.js`, `lib/firebase-config.ts`, `hooks/useNotifications.ts`, `components/notifications/NotificationPermissionPrompt.tsx`, `app/layout.tsx`

**Validation MVP (checklist complète dans doc référence):**
- [ ] Infrastructure FCM backend + frontend opérationnelle
- [ ] 5 notifications critiques (N01-N05) fonctionnent E2E
- [ ] Tests passent sur iOS Safari + Android Chrome
- [ ] Hangfire job rappels exécuté toutes les 5min sans erreur
- [ ] Device tokens enregistrés et notifications reçues < 2s

---

### US13 - Rappel de Partie (2h avant) ❌ POST-MVP
**Statut:** Inclus dans US12 (N04) | **Priorité:** Moyenne | **Taille:** -

**Description:** Couvert par notification N04 de US12. Job Hangfire planifié 2h avant chaque partie.

---

### US14 - Notification Place Libre ❌ POST-MVP
**Statut:** Non implémenté | **Priorité:** Faible | **Taille:** M

**Description:** Système de "wishlist" + notification push si place libérée dans partie pleine (suite à désistement). Nécessite nouvelle table `GameWatchlist` + endpoint `POST /games/{id}/watch`.

---

## Epic 4: Plateforme Mobile (PWA)

**Statut:** 🔴 **0% COMPLÉTÉ** | **Bloquant:** Manifest + Service Worker

### US15 - PWA Mobile-First ⚠️ BLOQUANT MVP
**Statut:** Non implémenté | **Priorité:** Critique | **Taille:** M

**Description:** App installable sur iOS/Android avec manifest et service worker.

**Frontend:**
- [ ] Créer `public/manifest.json`:
  - `name`: "Vibora - Padel Matchmaking"
  - `short_name`: "Vibora"
  - `start_url`: "/"
  - `display`: "standalone"
  - `icons`: 192x192, 512x512
- [ ] Créer icônes PWA:
  - `public/icon-192.png`
  - `public/icon-512.png`
  - `public/apple-touch-icon.png` (180x180)
- [ ] Service worker (cache offline):
  - Installer `next-pwa` ou manuel
  - Configuration `next.config.mjs`
  - Stratégie: NetworkFirst pour API, CacheFirst pour assets
- [ ] Meta tags:
  - `<meta name="viewport" content="width=device-width, initial-scale=1, maximum-scale=5">`
  - `<meta name="theme-color" content="#..." />`
  - `<meta name="apple-mobile-web-app-capable" content="yes">`
  - `<link rel="manifest" href="/manifest.json">`
  - `<link rel="apple-touch-icon" href="/apple-touch-icon.png">`

**Fichiers:**
- `public/manifest.json`, `public/icon-*.png`
- `app/layout.tsx` (meta tags)
- `next.config.mjs` (PWA config)

---

### US16 - Performance & Responsive
**Statut:** ✅ OK (mobile-first design) | **Priorité:** Haute | **Taille:** -

---

### US17 - Wrapper Natif & Stores ❌ POST-MVP
**Statut:** Non implémenté | **Priorité:** Moyenne | **Taille:** XL

**Description:** Wrapper Capacitor/Cordova pour App Store/Play Store. Notifications natives iOS, In-App Purchases, distribution officielle. Nécessite comptes développeur Apple ($99/an) + Google ($25 one-time).

---

### US18 - Suivi Analytics ❌ POST-MVP
**Statut:** Non implémenté | **Priorité:** Moyenne | **Taille:** S

**Description:** Intégration Google Analytics 4 ou Plausible (RGPD-friendly). Events: signup, create_game, join_game, share_game, conversion_rate guest→user.

---

## Epic 5: Marketing & Lancement

**Statut:** 🔴 **60% COMPLÉTÉ** | **Bloquant:** Pages légales RGPD

### US19 - Pages FAQ/Contact/Legal ⚠️ BLOQUANT MVP
**Statut:** Non implémenté | **Priorité:** Critique (RGPD) | **Taille:** S

**Description:** Conformité RGPD + Support utilisateur de base.

**Frontend:**
- [ ] Page `app/faq/page.tsx` (accordion Radix UI, sections: Compte, Parties, Notifications)
- [ ] Page `app/contact/page.tsx` (formulaire simple ou `mailto:support@vibora.app`)
- [ ] Page `app/legal/privacy/page.tsx` (RGPD: données collectées, finalité, droits utilisateur, DPO)
- [ ] Page `app/legal/terms/page.tsx` (CGU: usage acceptable, responsabilités, mentions légales)
- [ ] Composant `components/Footer.tsx` (liens: FAQ, Contact, Privacy, Terms)
- [ ] Ajouter `<Footer />` dans `app/layout.tsx`

**Fichiers:**
- `app/faq/page.tsx`, `app/contact/page.tsx`
- `app/legal/privacy/page.tsx`, `app/legal/terms/page.tsx`
- `components/Footer.tsx`

---

### US20 - Beta Test Communauté Pilote
**Statut:** ⏳ Opérationnel | **Priorité:** Haute | **Taille:** -

---

### US21 - Partenariats Clubs
**Statut:** ⏳ Opérationnel | **Priorité:** Moyenne | **Taille:** -

---

### US22 - Landing Page Web ❌ POST-MVP
**Statut:** Non implémenté | **Priorité:** Moyenne | **Taille:** M

**Description:** Page marketing standalone (vibora.app) avec présentation features, témoignages, CTA "Télécharger l'app". SEO optimisé. Peut être sur Webflow/Framer pour rapidité.

---

### US23 - Communication Lancement
**Statut:** ⏳ Opérationnel | **Priorité:** Moyenne | **Taille:** -

---

## Epic 6: UI/UX Design

**Statut:** 🔴 **50% COMPLÉTÉ** | **Bloquant:** Design System + Review UI

### US24 - Identité Visuelle & Charte
**Statut:** ✅ OK | **Priorité:** Moyenne | **Taille:** -

---

### US25 - Design Onboarding & Profil
**Statut:** ✅ OK | **Priorité:** Haute | **Taille:** -

---

### US26 - Design Pages de Parties
**Statut:** ✅ OK | **Priorité:** Haute | **Taille:** -

---

### US27 - Design Responsive Mobile
**Statut:** ✅ OK | **Priorité:** Haute | **Taille:** -

**Correctifs UX recommandés (non bloquants, Taille: S):**
- [ ] Formulaire guest: Retirer champ "nom", rendre skillLevel optionnel (2 champs max)
- [ ] Date picker création: Boutons quick actions "Ce soir", "Demain", "Week-end" + calendrier fallback
- [ ] Pull-to-refresh liste parties (geste natif)
- [ ] Créer `tailwind.config.ts` (centraliser design tokens)
- [ ] Viewport meta dans `app/layout.tsx` (si pas ajouté par PWA)

---

### US28 - Design System & Review UI ⚠️ BLOQUANT MVP
**Statut:** 🔜 À implémenter | **Priorité:** Critique | **Taille:** L

**Description:** Audit complet UI + mise en place design system avec composants réutilisables. Actuellement : composants UI non réutilisables, pages non alignées (topbar inconsistants, spacing différents, composants dupliqués).

**Objectifs:**
- Unifier tous les composants UI dans `components/ui/` (Radix + custom)
- Standardiser layouts (topbar, spacing, typographie)
- Créer `tailwind.config.ts` avec design tokens centralisés
- Documenter composants réutilisables (Storybook optionnel)

**Tâches Frontend:**
- [ ] **Audit UI complet:**
  - Inventaire composants dupliqués (buttons, cards, modals, inputs)
  - Liste pages avec layout non aligné (topbar/navbar inconsistant)
  - Identification spacing/colors/fonts non standardisés
- [ ] **Design Tokens (`tailwind.config.ts`):**
  - Colors (primary, secondary, accent, neutrals)
  - Spacing scale (4px base unit)
  - Typography (font families, sizes, weights, line-heights)
  - Border radius, shadows, transitions
- [ ] **Composants UI Réutilisables:**
  - Unifier `<Button>` variants (primary, secondary, ghost, danger)
  - Créer `<GameCard>` réutilisable (liste, mes parties, détail)
  - Standardiser `<Modal>` / `<Dialog>` (confirmation, forms)
  - Unifier `<Input>`, `<Textarea>`, `<Select>` avec états (error, disabled)
  - Créer `<TopBar>` / `<PageHeader>` standardisé (titre, back button, actions)
- [ ] **Layout Standardization:**
  - Template `<PageLayout>` (topbar + content + footer)
  - Spacing consistant (px-4, py-6, gap-4)
  - Breakpoints responsive (sm, md, lg, xl)
- [ ] **Refactoring Pages:**
  - Refactor `/create-game` avec nouveaux composants
  - Refactor `/games/[id]` avec `<GameCard>` + `<TopBar>`
  - Refactor `/my-games` avec composants unifiés
  - Refactor `/join/[token]` (cohérence formulaires)
- [ ] **Documentation (optionnel mais recommandé):**
  - `DESIGN_SYSTEM.md` avec guidelines
  - Exemples usage composants
  - Storybook (si temps) pour catalogue composants

**Validation:**
- [ ] Aucun composant UI dupliqué (DRY)
- [ ] Toutes les pages utilisent les mêmes composants de base
- [ ] `tailwind.config.ts` contient tous les design tokens
- [ ] Spacing/colors/fonts consistants sur toutes les pages
- [ ] Topbar/navbar standardisé (même structure partout)

**Fichiers:**
- `tailwind.config.ts` (design tokens)
- `components/ui/` (tous les composants de base)
- `components/layouts/PageLayout.tsx`, `components/layouts/TopBar.tsx`
- Refactor: `app/create-game/`, `app/games/[id]/`, `app/my-games/`, `app/join/[token]/`

---

## Epic 7: Bot WhatsApp/Telegram ❌ POST-MVP

**Statut:** Non implémenté | **Priorité:** Basse | **Taille:** XL

**Description:** Bot Telegram/WhatsApp capable d'analyser messages ("Dispo padel ce soir 19h Paris?") et créer partie automatiquement + poster Magic Link dans la conversation. Nécessite NLP basique (regex ou GPT-4 API), webhook Telegram, WhatsApp Business API ($$$).

**Justification Post-MVP:** Magic Links suffisent pour partage au MVP. Bot = feature viralité pour V2.

---

## Epic 8: Récompenses & Fidélisation ❌ POST-MVP

**Statut:** Non implémenté | **Priorité:** Faible | **Taille:** L

**Description:** Système badges joueurs vertueux (>85% présence), récompenses physiques mensuelles (raquettes, goodies). Nécessite US03 complète (tracking attendance), endpoint admin `/users/badges`, partenariats clubs pour lots.

**Justification Post-MVP:** Nécessite tracking attendance (US03 complète). Gamification pour V2+.

---

## 🚀 Plan d'Action Release MVP

### Phase 1 - Bloquants Critiques (Taille: 2 XL + 2 M + 1 S + 1 L)
1. **Notifications Push FCM** (XL) - US12 + US10.1 (dépendance)
2. **Design System & Review UI** (L) - US28
3. **PWA Manifest + Service Worker** (M) - US15
4. **Pages FAQ/Contact/Legal RGPD** (S) - US19
5. **Google Places Autocomplete** (S) - US04 (complément)

### Phase 2 - Améliorations UX (Taille: S)
1. **Correctifs UX mineurs** (S) - US27 (formulaire guest, date picker, pull-to-refresh)

### Phase 3 - Post-MVP
- Bot WhatsApp/Telegram (XL) - Epic 7
- Récompenses & badges (L) - Epic 8
- Wrapper natif stores (XL) - US17
- Landing page marketing (M) - US22
- Analytics (S) - US18
- Notification place libre (M) - US14

---

## 📝 Décisions Architecturales Clés

### Frontend
- ✅ Next.js 15 (App Router) + React 19
- ✅ Tailwind CSS v4 + Radix UI
- ✅ Supabase Auth (JWT)
- ✅ API client centralisé (`lib/api/vibora-client.ts`)
- 🔜 Google Places Autocomplete (frontend only, pas module Clubs backend)
- 🔜 Design System centralisé (`tailwind.config.ts` + `components/ui/`)

### Backend
- ✅ .NET 9 Modular Monolith (Clean Architecture + DDD)
- ✅ CQRS + MediatR
- ✅ Result Pattern (Ardalis.Result)
- ✅ Unit of Work + Domain Events (MassTransit)
- ✅ PostgreSQL (EF Core)
- ✅ Supabase Auth JWT validation
- 🔜 Hangfire (job scheduler pour rappels)

### UX "Less is More"
- ✅ Navigation minimale (2 sections + FAB)
- ✅ Maximum 3 taps actions core
- ✅ Mode invité sans friction
- ✅ Dates relatif ("Aujourd'hui 19h")
- 🔶 Formulaires ultra-simplifiés (correctif recommandé)

---

## 📊 Estimation Complexité Totale MVP

| Epic | Taille Totale |
|------|---------------|
| Comptes Utilisateurs & Profils | ✅ Complété |
| Gestion des Parties | S (Google Places) |
| Notifications & Rappels | XL (US12) |
| Plateforme Mobile (PWA) | M (US15) |
| Marketing & Lancement | S (US19) |
| UI/UX Design | L (US28) + S (US27) |
| **Total bloquants MVP** | **2 XL + 1 L + 1 M + 2 S** |

---

## 📋 Changelog

### 2025-10-30 - Audit PO + Refonte Backlog
- **Suppression US11 (Cancel Game):** Remplacée par US10.1 (message contextualisé)
- **5 bloquants MVP identifiés:** Push Notifications, Design System, PWA, Pages légales, Google Places
- **Ajout US28 (Design System & Review UI):** Audit complet + composants réutilisables
- **Suppression estimations temps:** Remplacées par tailles T-shirt (XS, S, M, L, XL)
- **Description US Post-MVP concise:** Epics 7-8 + US individuelles (US14, US17, US18, US22)
- **Analyse exhaustive notifications push:** Document `NOTIFICATIONS_PUSH_MVP_LISTE_EXHAUSTIVE.md`
  - 5 notifications critiques MVP (N01-N05)
  - 6 domain events backend analysés (4 consumers existants, 2 manquants)
  - Dépendance US10.1 pour N02
  - Job scheduler Hangfire requis pour N04

### 2025-10-26
- Magic Links (US10) complétés avec Open Graph dynamique
- Pages auth, liste parties, détail, mes parties complétées

### 2025-10-24
- Création partie (US04) complétée (backend + frontend basique)

### 2025-10-18
- Backend audit complet (modules Games, Users, Notifications)

---

**Documents de référence:**
- `docs/Conception UX minimaliste et centrée utilisateur pour le MVP de Vibora.md`
- `docs/AUDIT_BACKEND_2025-10-18.md`
- `docs/NOTIFICATIONS_PUSH_MVP_LISTE_EXHAUSTIVE.md` ⭐ (2025-10-30)
- `CLAUDE.md` (guidance développement)
