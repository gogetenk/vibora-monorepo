# Notifications Screen Specs - Vibora MVP

**Document:** Écran des notifications (icône cloche, top-right home)
**Date:** 2025-11-05 | **Statut:** À valider PO
**Lien Backlog:** US12 (Notifications Push) + US13 (Rappels)

---

## 1. Wireframe ASCII

```
┌─────────────────────────────────────────┐
│  ← Notifications             [×]        │ ← Close/Back
├─────────────────────────────────────────┤
│                                         │
│  🔔 Vous avez 3 notifications           │ ← Unread count (optional)
│                                         │
├─────────────────────────────────────────┤
│ ✓ [Unread] Nouveau joueur rejoint       │ ← Swipe left = mark read
│ Pierre a rejoint "Padel ce soir 19h"    │   Long press = delete
│ Aujourd'hui 14:32                       │
│                                         │
├─────────────────────────────────────────┤
│ [Read] Rappel - Partie dans 2h          │ ← Grayed out = read
│ "Demain 18h - Casa Padel"               │
│ Hier 16:00                              │
│                                         │
├─────────────────────────────────────────┤
│ [Unread] Partie annulée                 │
│ Terrain fermé - "Match demain 19h"      │
│ Hier 12:15                              │
│                                         │
├─────────────────────────────────────────┤
│ [Unread] Invitation à rejoindre          │
│ Pierre t'invite: "Padel ce soir 19h"    │
│ Il y a 2 jours                          │
│                                         │
├─────────────────────────────────────────┤
│                                         │
│  [Aucune notification] Empty state      │ ← If no items
│                                         │
└─────────────────────────────────────────┘
```

---

## 2. Notification Types & Display Rules

| Type | Template | Recipients | Read State |
|------|----------|------------|-----------|
| **N01** Player Joined | "{PlayerName} a rejoint {GameName}" | Host + all participants | ✓ Unread by default |
| **N02** Player Left | "{PlayerName} a quitté - {LeaveReason}" | Host + remaining players | ✓ Unread by default |
| **N03** Game Full | "Partie complète! {GameName}" | Host only | ✓ Unread by default |
| **N04** 2h Reminder | "Rappel - Partie dans 2h: {GameName}" | All participants | ✓ Unread by default |
| **N05** Game Cancelled | "Partie annulée - {CancelReason} ({GameName})" | All participants | ✓ Unread by default |

---

## 3. Key Behaviors

### Mark as Read
- **Tap notification** → Opens game detail page + marks as read
- **Swipe left** → Inline action "Mark read" (optional, less important)
- **Visual feedback:** Unread = **bold font + colored left border**, Read = grayed out

### Delete Notification
- **Long press** or **swipe right** → Confirm "Delete notification?"
- **Deleted notifications** → Not stored (dismiss from DB immediately, no trash)
- Action is **secondary & discreet** (not blocking)

### Auto-cleanup
- Notifications older than **30 days** → Auto-archived (optional, post-MVP)
- Unread notifications **never auto-delete** (only manual)

---

## 4. Empty State

```
┌──────────────────────────────┐
│                              │
│      🔔 Aucune notification  │
│                              │
│  Vous êtes à jour !          │
│                              │
│  Les mises à jour de vos     │
│  parties apparaîtront ici.   │
│                              │
└──────────────────────────────┘
```

**Text:** "Aucune notification" (Simple, not exaggerated)
**CTA:** None (user returns to home via back button)

---

## 5. Acceptance Criteria (MVP)

- [X] **Max 3 taps** to read notification (tap bell → tap notification → see game detail)
- [X] **Notifications list loads < 2s** (backend query with date DESC, limit 20)
- [X] **Unread indicator visible** (bold font, left border, or dot)
- [X] **Delete action works** without confirmation if swipe-only (confirm if long-press)
- [X] **Empty state displays** when no notifications exist
- [X] **No persistence bugs** (mark read = instant, doesn't revert on refresh)
- [X] **Guest-friendly** (read-only if guest, no delete for some types)
- [X] **Responsive mobile layout** (full width, 60px min tap target, padding-safe area iOS)

---

## 6. Backend Requirements

- [ ] `GET /notifications` (query: pageNumber, pageSize; response: sorted by createdDate DESC)
- [ ] `PUT /notifications/{id}/read` (mark as read)
- [ ] `DELETE /notifications/{id}` (soft delete or hard delete)
- [ ] Notification entity: `Id, UserId, Type, GameId, Title, Body, CreatedDate, IsRead, DeletedAt`
- [ ] Domain event consumers: Create notification entries on N01-N05 events

---

## 7. Frontend Files

- **Page:** `app/notifications/page.tsx`
- **Component:** `components/notifications/NotificationList.tsx`
- **Component:** `components/notifications/NotificationCard.tsx`
- **Hook:** `lib/hooks/useNotifications.ts` (fetch, mark read, delete)
- **API:** `viboraApi.notifications.getAll()`, `markAsRead()`, `delete()`

---

## 8. UX Philosophy Alignment

✅ **Less is More:**
- Single-column list, minimal chrome (no tabs, no filters initially)
- Actions: tap (read) | swipe (delete) | no modals unless dangerous
- Typography: Dark/light mode support, high contrast for unread

✅ **Guest-Friendly:**
- Guests see notifications but cannot delete (read-only)
- No "Follow" recommendations in notifications

✅ **Max 3 Taps:**
- Tap bell icon → Tap notification → Lands on game detail (reads notification implicitly)

✅ **Invisible Persistence:**
- Read/unread state saved locally + synced to backend
- No loading spinners for mark-read (optimistic update)

---

## 9. References

- **UX Doc:** `Conception UX.md`, Parcours 1-2 (notification flows implicit, added here)
- **Backlog:** `Backlog.md`, US12 (Notifications Push)
- **Domain Events:** `NOTIFICATIONS_PUSH_MVP_LISTE_EXHAUSTIVE.md` (N01-N05)

---

**Status for Backlog:** 🔜 À spécifier (avant dev US12)
