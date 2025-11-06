# User Preferences - Notifications Screen Spec

**Objectif:** Écran `/settings/notifications` ultra-simple pour activer/désactiver notifications push (global + par type).

**Priorité:** Critique MVP | **Taille:** S | **Dépendance:** US12 (Notifications Push)

---

## Wireframe ASCII

```
┌─────────────────────────────────┐
│ ← Notifications           [×]   │
├─────────────────────────────────┤
│ 🔔 Autoriser les notifications  │
│ [Toggle: ON/OFF]                │
├─────────────────────────────────┤
│ Par type:                       │
│                                 │
│ 🟢 Nouveau joueur rejoint   [✓] │
│ 🟢 Joueur a quitté          [✓] │
│ 🟢 Partie complète (4/4)    [ ] │
│ 🟢 Rappel 2h avant          [✓] │
│ 🟢 Partie annulée           [✓] │
│                                 │
├─────────────────────────────────┤
│ ℹ️ Les notifications sont envoyées│
│ via push. Vous pouvez modifier  │
│ vos préférences à tout moment.   │
└─────────────────────────────────┘
```

---

## Types de Notifications MVP

| ID | Nom User-Friendly | Description Backend | Défaut | Priorité |
|----|--------------------|-------------------|--------|----------|
| **N01** | Nouveau joueur rejoint | Un participant rejoint → hôte + tous les participants | ON | Critique |
| **N02** | Joueur a quitté | Un participant quitte → hôte + participants restants | ON | Critique |
| **N03** | Partie complète (4/4) | Partie atteint max joueurs → hôte | OFF | Optionnel |
| **N04** | Rappel 2h avant | Job Hangfire 2h avant match → tous les participants | ON | Critique |
| **N05** | Partie annulée | Partie annulée → tous les participants | ON | Critique |

---

## Acceptance Criteria MVP

- [ ] **Toggle Global ON/OFF** – Désactiver toutes les notifications (override les toggles individuels)
- [ ] **Toggles Individuels** – Chaque type de notif a son toggle indépendant (visible sauf si global OFF)
- [ ] **Persistence Backend** – Prefs sauvegardées dans `User.NotificationPreferences` (table/enum JSON)
- [ ] **Endpoint API** – `PUT /users/notification-preferences` (accepte global on/off + dict par type)
- [ ] **UX si Désactivé Global** – Les toggles individuels sont grisés/disabled visuellement
- [ ] **Guest Users** – Guests (non inscrits) = N/A, prefs ignorées (notifications post-MVP pour guests)

---

## Comportement UX

### Si Global = OFF
- Tous les toggles individuels deviennent **disabled** (grisés)
- Message sub: "Activer les notifications globalement pour gérer par type"
- Aucune notification push envoyée, peu importe le type

### Si Global = ON
- Chaque type a son propre toggle actif
- Les toggles individuels OFF = cet événement n'envoie pas de notif
- Exemple: N03 OFF → pas de notif "Partie complète", mais N01, N02, N04, N05 peuvent être reçues

### Toast Confirmation
- Après toggle changement: *"Préférences mises à jour"* (1.5s)

---

## Fichiers Frontend

- **Page:** `/app/settings/notifications/page.tsx`
- **Component:** `components/notifications/NotificationPreferencesForm.tsx`
- **Hook:** `useNotificationPreferences()` (fetch + update prefs backend)
- **Types:** `viboraApi.users.getNotificationPreferences()`, `updateNotificationPreferences(prefs)`

---

## Fichiers Backend

- **Endpoint:** `PUT /users/notification-preferences` (UsersController)
- **Command:** `UpdateNotificationPreferencesCommand`
- **Entity:** `User.NotificationPreferences` (ajout champ)
- **Migration:** Ajouter colonne `notification_preferences` (JSON PostgreSQL)

---

## Notes

- **Guest Users:** Screen N/A (redirects vers login ou affiche message "Connectez-vous pour gérer les préférences")
- **Post-MVP:** Notification par canal (push/email/SMS), quiet hours (Do Not Disturb), etc.
- **Scope:** Prefs stockées per-user, pas de réglages globaux admin

---

**Statut:** 🔜 À implémenter (post-infrastructure FCM)

**Référence:** `docs/NOTIFICATIONS_PUSH_MVP_LISTE_EXHAUSTIVE.md` (N01-N05)
