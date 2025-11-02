# 📱 NOTIFICATIONS PUSH MVP - LISTE EXHAUSTIVE

**Auteur:** Product Owner Vibora
**Date:** 2025-10-30
**Contexte:** US12 (Notifications Push) - Définition complète des notifications requises pour le MVP
**Références:** `docs/Conception UX.md`, `docs/Backlog du MVP Vibora.md`

---

## 🎯 CONTEXTE & OBJECTIF

### Problème Initial
L'US12 du backlog définissait vaguement : "Notifications temps réel (invitation, annulation, nouveau joueur)".

Cette description est **insuffisante** pour:
- Guider le développement backend/frontend
- Estimer l'effort correctement
- Identifier les gaps techniques (events manquants, consumers à créer)

### Objectif de ce Document
Fournir une **liste exhaustive et priorisée** des notifications push nécessaires au MVP, en analysant:
1. **Events backend existants** (domain events déjà implémentés)
2. **Parcours utilisateurs critiques** (doc UX)
3. **User stories complétées** (US07-US10.1)

---

## 📊 ÉTAT DE L'INFRASTRUCTURE BACKEND

### ✅ Ce qui Existe Déjà

**Module Notifications complet:**
- ✅ Architecture CQRS + MediatR
- ✅ Abstraction `INotificationChannel` (Push, Email, SMS)
- ✅ Service `NotificationTemplateService` avec templates français
- ✅ Enum `NotificationType` (6 types définis)
- ✅ 4 consumers MassTransit opérationnels

**Domain Events existants (module Games):**
| Event | Fichier | Publié via MassTransit |
|-------|---------|------------------------|
| `PlayerJoinedDomainEvent` | `Vibora.Games/Domain/Events/` | ✅ Oui → `PlayerJoinedEvent` |
| `GuestJoinedGameDomainEvent` | `Vibora.Games/Domain/Events/` | ✅ Oui → `GuestJoinedEvent` |
| `ParticipationRemovedDomainEvent` | `Vibora.Games/Domain/Events/` | ✅ Oui → `ParticipationRemovedEvent` |
| `GameCanceledDomainEvent` | `Vibora.Games/Domain/Events/` | ✅ Oui → `GameCanceledEvent` |
| `GameCreatedDomainEvent` | `Vibora.Games/Domain/Events/` | ❌ Non publié |
| `GameSharedDomainEvent` | `Vibora.Games/Domain/Events/` | ❌ Non publié |

**Consumers existants (module Notifications):**
| Consumer | Event Consommé | Template Existant | Notifie Qui |
|----------|---------------|-------------------|-------------|
| `PlayerJoinedEventConsumer` | `PlayerJoinedEvent` | ✅ `BuildPlayerJoinedContent()` | Hôte uniquement |
| `GuestJoinedEventConsumer` | `GuestJoinedEvent` | ✅ `BuildGuestJoinedContent()` | Hôte uniquement |
| `ParticipationRemovedEventConsumer` | `ParticipationRemovedEvent` | ✅ `BuildPlayerLeftContent()` | Hôte uniquement |
| `GameCanceledEventConsumer` | `GameCanceledEvent` | ✅ `BuildGameCanceledContent()` | **TOUS les participants** |

**NotificationTypes existants:**
```csharp
public enum NotificationType
{
    GameCreated = 1,       // ✅ Template existe (non utilisé)
    PlayerJoined = 2,      // ✅ Utilisé (Player + Guest)
    PlayerLeft = 3,        // ✅ Utilisé
    GameCancelled = 4,     // ✅ Utilisé
    GameStartingSoon = 5,  // ✅ Template existe (non utilisé)
    NewChatMessage = 6     // ✅ Template existe (non utilisé - chat post-MVP)
}
```

### 🔴 Ce qui Manque (Bloquants MVP)

**Backend:**
1. ❌ **`FcmNotificationChannel.SendAsync()` = stub vide**
   - Fichier: `Vibora.Notifications/Infrastructure/FcmNotificationChannel.cs`
   - Besoin: Implémenter Firebase Admin SDK

2. ❌ **Endpoint `POST /users/device-tokens` manquant**
   - Module: `Vibora.Users`
   - Besoin: Enregistrer FCM tokens des appareils

3. ❌ **Configuration Firebase manquante**
   - Fichier: `Program.cs` (API)
   - Besoin: `FirebaseApp.Create()` avec credentials

4. ❌ **Job scheduler pour rappels 2h avant**
   - Besoin: Hangfire ou Quartz.NET
   - Event déclenché: `GameStartingSoon`

5. ❌ **Event `GameCompletedDomainEvent` manquant**
   - Pour notifier "Partie complète (4/4)" à l'hôte

6. ❌ **Notifications multi-destinataires (participants) incomplètes**
   - Seul `GameCanceledEventConsumer` notifie tous les participants
   - `PlayerJoinedEventConsumer` notifie uniquement l'hôte (devrait notifier aussi les participants déjà inscrits)

**Frontend:**
1. ❌ **Projet Firebase Console non créé**
2. ❌ **Service Worker Firebase non implémenté**
3. ❌ **Configuration `firebase-messaging-sw.js` manquante**
4. ❌ **Composant `<NotificationPermissionPrompt>` manquant**
5. ❌ **Envoi device token au backend non implémenté**

---

## 📱 NOTIFICATIONS CRITIQUES MVP (Bloquantes Release)

### N01 - Nouveau Participant Rejoint la Partie ⭐

**Priorité:** 🔴 CRITIQUE MVP
**Statut Backend:** 🟡 PARTIEL (hôte uniquement)
**Statut Frontend:** ❌ Non implémenté

**Déclencheur:**
- Event: `PlayerJoinedEvent` (registered user)
- Event: `GuestJoinedEvent` (guest participant)

**Destinataires ACTUELS (INCOMPLET):**
- ✅ Hôte uniquement (via `PlayerJoinedEventConsumer`)

**Destinataires REQUIS (conformité UX):**
- ✅ Hôte
- ❌ **MANQUE: Tous les participants déjà inscrits** (sauf celui qui vient de rejoindre)

**Message:**
```
Titre: "Nouveau joueur"
Corps: "[Nom] a rejoint la partie ! (3/4 joueurs)"
```

**Contexte UX (Conception UX.md, ligne 78):**
> "À mesure que d'autres joueurs rejoignent, le créateur reçoit une notification (push) et voit les nouveaux inscrits apparaître dans la liste des participants."

**Justification:**
- **Hôte:** Doit savoir quand la partie est complète pour arrêter de chercher des joueurs
- **Participants existants:** Doivent savoir qui sont leurs coéquipiers (coordination, anticipation)

**Fichiers Backend:**
- ✅ Event: `Vibora.Games/Domain/Events/PlayerJoinedDomainEvent.cs`
- ✅ Integration Event: `Vibora.Games.Contracts/Events/PlayerJoinedEvent.cs`
- 🔶 Consumer: `Vibora.Notifications/Application/EventHandlers/PlayerJoinedEventConsumer.cs` (MODIFIER)
- ✅ Template: `NotificationTemplateService.BuildPlayerJoinedContent()`

**Modifications Requises:**
```csharp
// AVANT (actuel - INCOMPLET):
public async Task Consume(ConsumeContext<PlayerJoinedEvent> context)
{
    var deviceToken = await GetHostDeviceToken(@event.HostExternalId);
    await SendNotification(@event.HostExternalId, deviceToken, content); // Hôte uniquement
}

// APRÈS (requis - COMPLET):
public async Task Consume(ConsumeContext<PlayerJoinedEvent> context)
{
    // 1. Notifier l'hôte
    await NotifyHost(@event.HostExternalId, content);

    // 2. Récupérer TOUS les participants de la partie (sauf celui qui vient de rejoindre)
    var existingParticipants = await _gameRepository.GetParticipantIds(
        @event.GameId,
        excludeUserId: @event.UserExternalId);

    // 3. Notifier chaque participant existant
    foreach (var participantId in existingParticipants)
    {
        await NotifyParticipant(participantId, content);
    }
}
```

**Effort estimé:** 3 heures
- Modifier `PlayerJoinedEventConsumer` (1h)
- Créer `IGameRepository.GetParticipantIds()` (1h)
- Tests unitaires + intégration (1h)

---

### N02 - Participant Quitte la Partie ⭐

**Priorité:** 🔴 CRITIQUE MVP
**Statut Backend:** 🟡 PARTIEL (hôte uniquement + raisons manquantes)
**Statut Frontend:** ❌ Non implémenté

**Déclencheur:**
- Event: `ParticipationRemovedEvent`

**Destinataires ACTUELS (INCOMPLET):**
- ✅ Hôte uniquement (via `ParticipationRemovedEventConsumer`)

**Destinataires REQUIS:**
- ✅ Hôte
- ❌ **MANQUE: Tous les participants restants**

**Message (selon raison de départ - US10.1):**
```
Si LeaveReason.Personal:
  "Pierre a quitté (empêchement perso). Vous pouvez continuer (2/4)"

Si LeaveReason.VenueClosed:
  "⚠️ Pierre a quitté - Le terrain n'est plus disponible"

Si LeaveReason.Weather:
  "Pierre a quitté (météo). À vous de décider (2/4)"

Si LeaveReason.Other + message:
  "Pierre a quitté : [message]. Places restantes (2/4)"
```

**Contexte UX (Conception UX.md, lignes 50-51):**
> "Une indication "Vous avez rejoint cette partie" rassure l'utilisateur. Option de communication intégrée : pour surpasser WhatsApp, l'écran offre un mini-chat de groupe"

**Justification:**
Info critique pour tous les participants pour décider si la partie peut toujours avoir lieu. Maximise les matchs joués (autonomie > paternalisme).

**⚠️ DÉPENDANCE US10.1:**
Cette notification nécessite d'abord l'implémentation de US10.1 (Message Contextualisé au Départ):
- ❌ Enum `LeaveReason` non créé
- ❌ `ParticipationRemovedDomainEvent` ne contient pas `LeaveReason` ni `Message`
- ❌ `RemoveParticipantCommand` ne prend pas ces paramètres

**Fichiers Backend:**
- ✅ Event: `Vibora.Games/Domain/Events/ParticipationRemovedDomainEvent.cs`
- ✅ Integration Event: `Vibora.Games.Contracts/Events/ParticipationRemovedEvent.cs`
- 🔶 Consumer: `Vibora.Notifications/Application/EventHandlers/ParticipationRemovedEventConsumer.cs` (MODIFIER)
- 🔶 Template: `NotificationTemplateService.BuildPlayerLeftContent()` (MODIFIER - ajouter raisons)

**Modifications Requises:**
1. **Implémenter US10.1 d'abord** (2 jours - voir backlog)
2. Modifier `ParticipationRemovedEventConsumer` pour notifier tous les participants (1h)
3. Enrichir template avec raisons contextualisées (1h)

**Effort estimé:** 4 heures (après US10.1)
- Modifier consumer pour multi-destinataires (1h)
- Adapter templates selon `LeaveReason` (2h)
- Tests (1h)

---

### N03 - Partie Complète (4/4 joueurs) ⭐

**Priorité:** 🔴 CRITIQUE MVP
**Statut Backend:** ❌ Event manquant
**Statut Frontend:** ❌ Non implémenté

**Déclencheur:**
- Event: `GameCompletedDomainEvent` (❌ **À CRÉER**)
- Condition: `CurrentPlayers === MaxPlayers`

**Destinataire:**
- Hôte uniquement (confirmation positive)

**Message:**
```
Titre: "🎉 Partie complète !"
Corps: "Votre partie est complète ! Rendez-vous [date] à [lieu]"
```

**Contexte UX (Conception UX.md, ligne 78):**
> "Une fois le nombre de joueurs atteint (4/4), la partie est complète – l'interface peut le signaler (ex. barre verte 'Partie complète !')."

**Justification:**
- Confirmation positive pour l'hôte
- Peut arrêter de chercher des joueurs
- Encourage la conversion guest → compte ("Créez un compte pour gérer vos parties facilement")

**Fichiers Backend à Créer:**
```csharp
// Vibora.Games/Domain/Events/GameCompletedDomainEvent.cs
public sealed class GameCompletedDomainEvent : IDomainEvent
{
    public Guid GameId { get; }
    public string HostExternalId { get; }
    public DateTime GameDateTime { get; }
    public string Location { get; }
    public int MaxPlayers { get; }
    public IReadOnlyCollection<string> ParticipantNames { get; }
    // ...
}

// Vibora.Games.Contracts/Events/GameCompletedEvent.cs (MassTransit)
public record GameCompletedEvent { /* ... */ }

// Vibora.Notifications/Application/EventHandlers/GameCompletedEventConsumer.cs
public sealed class GameCompletedEventConsumer : IConsumer<GameCompletedEvent> { /* ... */ }
```

**Logique de Déclenchement:**
Dans `Game.AddParticipation()` ou `Game.AddGuestParticipant()`:
```csharp
public Result AddParticipation(Participation participation)
{
    _participations.Add(participation);

    RaiseDomainEvent(new PlayerJoinedDomainEvent(...));

    // NOUVEAU: Vérifier si partie complète
    if (CurrentPlayerCount() == MaxPlayers)
    {
        RaiseDomainEvent(new GameCompletedDomainEvent(...));
    }

    return Result.Success();
}
```

**Effort estimé:** 3 heures
- Créer `GameCompletedDomainEvent` + integration event (30min)
- Modifier logique `Game` pour lever l'event (30min)
- Créer `GameCompletedEventConsumer` (1h)
- Créer template `BuildGameCompletedContent()` (30min)
- Tests (30min)

---

### N04 - Rappel Partie (2h avant) ⭐

**Priorité:** 🔴 CRITIQUE MVP
**Statut Backend:** ❌ Non implémenté (nécessite job scheduler)
**Statut Frontend:** ❌ Non implémenté

**Déclencheur:**
- Job planifié (Cron/Hangfire)
- Exécuté 2h avant `GameDateTime` pour chaque partie avec status `Open` ou `Full`

**Destinataires:**
- **TOUS les participants** (hôte + joueurs + guests)

**Message:**
```
Titre: "⏰ Rappel : Partie dans 2h"
Corps: "Partie de padel à 19h au Club Padel Paris avec Pierre, Marie, Jean"
```

**Contexte UX (Conception UX.md, lignes 47-52):**
> "Grâce à cette coordination intégrée, Vibora devient plus simple que WhatsApp : pas besoin de basculer d'appli, le contexte est partagé automatiquement avec les bonnes personnes."

**Justification:**
- **Éviter les no-shows** (problème majeur WhatsApp)
- Critique pour la fiabilité de l'app
- Différenciateur vs WhatsApp (rappels automatiques)

**Architecture Requise:**

**Option A: Hangfire (Recommandé MVP):**
```csharp
// Program.cs
builder.Services.AddHangfire(config => config.UsePostgreSqlStorage(...));
builder.Services.AddHangfireServer();

// Vibora.Games/Infrastructure/Jobs/GameReminderJob.cs
public class GameReminderJob
{
    public async Task ScheduleReminders()
    {
        var upcomingGames = await _gameRepository.GetGamesStartingIn(
            TimeSpan.FromHours(2),
            TimeSpan.FromMinutes(5)); // Window: 2h-1h55

        foreach (var game in upcomingGames)
        {
            await _messageBus.Publish(new GameStartingSoonEvent(game));
        }
    }
}

// Enqueue job récurrent toutes les 5 minutes
RecurringJob.AddOrUpdate<GameReminderJob>(
    "game-reminders",
    job => job.ScheduleReminders(),
    "*/5 * * * *"); // Cron: chaque 5 minutes
```

**Option B: Quartz.NET (Alternative):**
Plus complexe, overkill pour MVP.

**Fichiers Backend à Créer:**
1. `Vibora.Games/Infrastructure/Jobs/GameReminderJob.cs`
2. `Vibora.Games.Contracts/Events/GameStartingSoonEvent.cs`
3. `Vibora.Notifications/Application/EventHandlers/GameStartingSoonEventConsumer.cs`
4. Configuration Hangfire dans `Program.cs`

**Notifications Multi-Destinataires:**
```csharp
public async Task Consume(ConsumeContext<GameStartingSoonEvent> context)
{
    var @event = context.Message;

    // Récupérer TOUS les participants (users + guests)
    var participants = await _gameRepository.GetAllParticipants(@event.GameId);

    // Template avec liste des joueurs
    var content = _templateService.BuildGameStartingSoonContent(
        @event.Location,
        @event.GameDateTime,
        participantNames: participants.Select(p => p.Name).ToList());

    // Notifier tous (registered users via push, guests via SMS/Email si disponible)
    await NotifyAllParticipants(participants, content);
}
```

**⚠️ Complexité Guests:**
Les guests n'ont pas de device token → nécessite SMS/Email channel (stub actuel).
**Décision MVP:** Notifier uniquement les registered users via push, guests = post-MVP.

**Effort estimé:** 1 jour (8 heures)
- Installer et configurer Hangfire (2h)
- Créer `GameReminderJob` + scheduler (2h)
- Créer `GameStartingSoonEvent` + consumer (2h)
- Template + tests (2h)

---

### N05 - Partie Annulée par l'Hôte ⭐

**Priorité:** 🔴 CRITIQUE MVP
**Statut Backend:** ✅ COMPLET
**Statut Frontend:** ❌ Non implémenté

**Déclencheur:**
- Event: `GameCanceledEvent`

**Destinataires:**
- ✅ **TOUS les participants** (hôte + joueurs + guests)

**Message:**
```
Titre: "Partie annulée"
Corps: "La partie de padel prévue le 15 Oct 2025 19:00 à Club Padel Paris a été annulée"
```

**Contexte UX:**
Bien que US11 (Cancel Game) ait été supprimée du backlog (hôte = pas de légitimité), l'événement `GameCanceledDomainEvent` existe déjà et pourrait être déclenché par d'autres mécanismes (admin, conditions météo API future, etc.).

**Statut Implémentation:**
✅ **Déjà complet:**
- ✅ Event domain + integration event existent
- ✅ Consumer `GameCanceledEventConsumer` notifie tous les participants
- ✅ Template `BuildGameCanceledContent()` existe
- ✅ Batch fetch device tokens optimisé

**Action Requise:**
Aucune (déjà fonctionnel backend). Uniquement frontend FCM à implémenter.

**Effort estimé:** 0 heure (backend déjà fait)

---

## 🟡 NOTIFICATIONS HAUTE PRIORITÉ MVP (Recommandées mais non bloquantes)

### N06 - Invitation à Rejoindre une Partie (Magic Link)

**Priorité:** 🟡 HAUTE (améliore viralité)
**Statut Backend:** 🔶 PARTIEL (event `GameSharedDomainEvent` existe mais non publié)
**Statut Frontend:** ❌ Non implémenté

**Déclencheur:**
- Event: `GameSharedEvent` (❌ **Non publié via MassTransit**)
- Action utilisateur: Clic sur "Partager" dans `/games/[id]`

**Destinataire:**
- Utilisateur externe (via WhatsApp/SMS/Telegram)
- **Pas une notification push** → Deep link avec Open Graph

**Message:**
```
(WhatsApp preview)
Titre: "Pierre vous invite à une partie de padel"
Image: 1200x630px avec détails partie
Lien: https://vibora.app/join/{token}
```

**Contexte UX (Conception UX.md, ligne 74):**
> "Lien d'invitation : en action secondaire, un bouton permet de partager la partie via un lien (WhatsApp, SMS...) pour inviter des amis spécifiques"

**Statut Actuel:**
- ✅ US10 (Magic Links) complétée frontend
- ✅ Open Graph SSR fonctionnel
- ✅ `GameSharedDomainEvent` existe
- ❌ Event non publié via MassTransit
- ❌ Pas de consumer (tracking viralité)

**Justification Post-MVP:**
Ce n'est **pas une notification push** mais un **lien de partage**. Le mécanisme existe déjà (US10). Un consumer pourrait être ajouté pour **tracker la viralité** (analytics), mais ce n'est pas critique pour le MVP.

**Effort estimé (si implémenté):** 2 heures
- Publier `GameSharedEvent` via MassTransit (30min)
- Créer consumer analytics (tracking seulement, pas de notification) (1h)
- Tests (30min)

**Recommandation PO:** Post-MVP (analytics V2)

---

### N07 - Message Reçu dans le Chat Partie

**Priorité:** 🟡 HAUTE (si chat implémenté au MVP)
**Statut Backend:** ✅ Template existe (`NotificationType.NewChatMessage`)
**Statut Frontend:** ❌ Chat post-MVP (pas dans scope actuel)

**Déclencheur:**
- Event: `MessageSentEvent` (module Chat - **post-MVP**)

**Destinataires:**
- Tous les participants sauf l'expéditeur

**Message:**
```
Titre: "Nouveau message"
Corps: "Pierre : Je serai 5 min en retard, désolé !"
```

**Contexte UX (Conception UX.md, lignes 49-52):**
> "Option de communication intégrée : pour surpasser WhatsApp, l'écran offre un mini-chat de groupe [...] Ce chat contextuel à la partie évite aux utilisateurs de devoir échanger leurs numéros"

**Décision PO:**
Le **chat n'est PAS dans le scope MVP initial** (backlog ne mentionne pas de US Chat).

**Recommandation:**
- **MVP:** Coordination via notifications système uniquement (join/leave)
- **V2:** Implémenter chat avec `NewChatMessage` notifications

**Effort estimé (si implémenté V2):** 1 jour
- Module Chat complet (6h)
- Notifications messages (2h)

---

## 🟢 NOTIFICATIONS POST-MVP (V2+)

### N08 - Place Libérée dans Partie Pleine

**Priorité:** 🟢 POST-MVP
**Contexte:** Wishlist, file d'attente

**Déclencheur:**
- Un joueur quitte une partie qui était à 4/4 → retombe à 3/4

**Destinataires:**
- Utilisateurs en "wishlist" (feature V2)

**Message:**
```
"🎉 Une place s'est libérée dans la partie du 15 Oct 19h ! Rejoignez maintenant."
```

**Effort estimé:** 3 jours (nécessite système wishlist)

---

### N09 - Joueur Suivi Crée une Partie

**Priorité:** 🟢 POST-MVP
**Contexte:** Epic "Social" - Suivre joueurs

**Déclencheur:**
- Un joueur que l'utilisateur "suit" crée une partie

**Destinataires:**
- Followers du créateur

**Message:**
```
"Pierre (que vous suivez) a créé une partie demain 19h. Rejoignez-le !"
```

**Effort estimé:** 2 jours (nécessite système follow)

---

### N10 - Feedback Post-Partie (Évaluation)

**Priorité:** 🟢 POST-MVP
**Contexte:** US03 complète (statistiques fiabilité)

**Déclencheur:**
- 1h après `GameDateTime`

**Destinataires:**
- Tous les participants

**Message:**
```
"Comment s'est passée votre partie ? Évaluez vos coéquipiers (optionnel)."
```

**Effort estimé:** 1 jour

---

### N11 - Badge Débloqué / Récompense

**Priorité:** 🟢 POST-MVP
**Contexte:** Epic "Gamification"

**Effort estimé:** 3 jours

---

## 📊 SYNTHÈSE PRIORISATION & EFFORT

| ID | Notification | Priorité | Event Backend | Consumer Existant | Template Existant | Effort Backend | Effort Frontend | Total |
|----|--------------|----------|---------------|-------------------|-------------------|----------------|-----------------|-------|
| **N01** | Nouveau participant rejoint | 🔴 MVP | ✅ Existe | 🟡 Partiel (hôte only) | ✅ Oui | 3h | Inclus US12 | 3h |
| **N02** | Participant quitte (avec raisons) | 🔴 MVP | ✅ Existe | 🟡 Partiel (hôte only) | 🟡 À enrichir | 4h (après US10.1) | Inclus US12 | 4h |
| **N03** | Partie complète (4/4) | 🔴 MVP | ❌ À créer | ❌ Non | ❌ À créer | 3h | Inclus US12 | 3h |
| **N04** | Rappel 2h avant | 🔴 MVP | ❌ À créer | ❌ Non | ✅ Existe | 1 jour (8h) | Inclus US12 | 8h |
| **N05** | Partie annulée | 🔴 MVP | ✅ Complet | ✅ Complet | ✅ Complet | 0h | Inclus US12 | 0h |
| **N06** | Invitation Magic Link | 🟡 Optionnel | ✅ Existe (non publié) | ❌ Non (analytics) | N/A | 2h | N/A | 2h |
| **N07** | Message chat | 🟡 Post-MVP | ❌ Module Chat manquant | ❌ Non | ✅ Existe | 1 jour | 1 jour | 2j |
| **N08** | Place libérée (wishlist) | 🟢 V2 | ❌ Non | ❌ Non | ❌ Non | 3j | - | 3j |
| **N09** | Joueur suivi crée partie | 🟢 V2 | ❌ Non | ❌ Non | ❌ Non | 2j | - | 2j |
| **N10** | Feedback post-partie | 🟢 V2 | ❌ Non | ❌ Non | ❌ Non | 1j | - | 1j |

**Total effort notifications critiques MVP (N01-N05):** 18 heures backend + Frontend FCM (US12 = 3 jours) = **~4-5 jours total**

**Répartition effort MVP:**
- **Backend spécifique notifications:** 18h (2.25 jours)
- **Frontend FCM infrastructure (US12):** 3 jours (inclut service worker, Firebase setup, device tokens)
- **Backend FCM infrastructure (US12):** 2 jours (inclut Firebase Admin SDK, endpoint device tokens)

**Total MVP (US12 complète):** ~7-8 jours

---

## 🔧 PLAN D'IMPLÉMENTATION BACKEND

### Phase 1: Infrastructure FCM (Bloquant) - 2 jours

**Fichiers à créer/modifier:**

1. **Firebase Configuration**
   ```csharp
   // vibora-backend/src/API/Program.cs
   FirebaseApp.Create(new AppOptions
   {
       Credential = GoogleCredential.FromFile("firebase-adminsdk.json")
   });
   ```

2. **FcmNotificationChannel Implementation**
   ```csharp
   // Vibora.Notifications/Infrastructure/FcmNotificationChannel.cs
   public async Task<Result> SendAsync(NotificationContent content, string deviceToken)
   {
       var message = new Message
       {
           Token = deviceToken,
           Notification = new Notification
           {
               Title = content.Title,
               Body = content.Body
           }
       };

       await FirebaseMessaging.DefaultInstance.SendAsync(message);
       return Result.Success();
   }
   ```

3. **Device Token Endpoint**
   ```csharp
   // Vibora.Users/Presentation/UsersController.cs
   [HttpPost("device-tokens")]
   public async Task<IActionResult> RegisterDeviceToken(
       [FromBody] RegisterDeviceTokenRequest request)
   {
       // Sauvegarder dans Users.DeviceToken (nouveau champ)
   }
   ```

**Tests:**
- Unit tests `FcmNotificationChannelTests.cs`
- Integration test avec Firebase Test Lab

---

### Phase 2: Consumers Multi-Destinataires - 3h

**Fichiers à modifier:**

1. **PlayerJoinedEventConsumer**
   ```csharp
   // AVANT: Notifie hôte uniquement
   // APRÈS: Notifie hôte + tous les participants existants

   var existingParticipants = await _gameRepository.GetParticipantIds(
       @event.GameId,
       excludeUserId: @event.UserExternalId);

   foreach (var participantId in existingParticipants)
   {
       await NotifyParticipant(participantId, content);
   }
   ```

2. **ParticipationRemovedEventConsumer**
   ```csharp
   // APRÈS: Notifie hôte + tous les participants restants
   // + intégrer LeaveReason de US10.1

   var content = _templateService.BuildPlayerLeftContent(
       @event.UserName,
       @event.LeaveReason, // NOUVEAU
       @event.LeaveMessage, // NOUVEAU
       @event.RemainingPlayers);
   ```

**Dépendance:**
- Créer `IGameRepository.GetParticipantIds(Guid gameId, string? excludeUserId)`

---

### Phase 3: GameCompleted Event & Consumer - 3h

**Fichiers à créer:**

1. **Domain Event**
   ```csharp
   // Vibora.Games/Domain/Events/GameCompletedDomainEvent.cs
   internal sealed class GameCompletedDomainEvent : IDomainEvent
   {
       public Guid GameId { get; }
       public string HostExternalId { get; }
       public int MaxPlayers { get; }
       // ...
   }
   ```

2. **Integration Event**
   ```csharp
   // Vibora.Games.Contracts/Events/GameCompletedEvent.cs
   public record GameCompletedEvent
   {
       public Guid GameId { get; init; }
       // ...
   }
   ```

3. **Consumer**
   ```csharp
   // Vibora.Notifications/Application/EventHandlers/GameCompletedEventConsumer.cs
   public sealed class GameCompletedEventConsumer : IConsumer<GameCompletedEvent>
   {
       public async Task Consume(ConsumeContext<GameCompletedEvent> context)
       {
           // Notifier hôte uniquement
           var content = _templateService.BuildGameCompletedContent(...);
           await NotifyHost(@event.HostExternalId, content);
       }
   }
   ```

4. **Logique Domain**
   ```csharp
   // Vibora.Games/Domain/Game.cs
   public Result AddParticipation(Participation participation)
   {
       _participations.Add(participation);
       RaiseDomainEvent(new PlayerJoinedDomainEvent(...));

       if (CurrentPlayerCount() == MaxPlayers)
       {
           RaiseDomainEvent(new GameCompletedDomainEvent(...)); // NOUVEAU
       }

       return Result.Success();
   }
   ```

5. **Template**
   ```csharp
   // NotificationTemplateService.cs
   public NotificationContent BuildGameCompletedContent(
       string location,
       DateTime gameDateTime,
       IEnumerable<string> participantNames)
   {
       var participantsText = string.Join(", ", participantNames);
       var context = new Dictionary<string, string>
       {
           ["location"] = location,
           ["date"] = gameDateTime.ToString("dd MMM yyyy HH:mm"),
           ["participants"] = participantsText
       };

       // Template: "🎉 Votre partie est complète ! Rendez-vous le {date} à {location} avec {participants}"
       return GenerateContent(NotificationType.GameCompleted, context);
   }
   ```

**Ajout Enum:**
```csharp
// NotificationType.cs
public enum NotificationType
{
    // ... existants
    GameCompleted = 7  // NOUVEAU
}
```

---

### Phase 4: Job Scheduler Rappels - 1 jour (8h)

**Fichiers à créer:**

1. **Installer Hangfire**
   ```bash
   dotnet add package Hangfire.AspNetCore
   dotnet add package Hangfire.PostgreSql
   ```

2. **Configuration**
   ```csharp
   // Program.cs
   builder.Services.AddHangfire(config =>
   {
       config.UsePostgreSqlStorage(connectionString);
   });
   builder.Services.AddHangfireServer();

   app.UseHangfireDashboard("/hangfire");
   ```

3. **Job Récurrent**
   ```csharp
   // Vibora.Games/Infrastructure/Jobs/GameReminderJob.cs
   public class GameReminderJob
   {
       private readonly IGameRepository _gameRepository;
       private readonly IPublishEndpoint _publishEndpoint;

       public async Task ScheduleReminders()
       {
           var targetTime = DateTime.UtcNow.AddHours(2);
           var windowStart = targetTime;
           var windowEnd = targetTime.AddMinutes(5);

           var upcomingGames = await _gameRepository.GetGamesInTimeWindow(
               windowStart,
               windowEnd);

           foreach (var game in upcomingGames)
           {
               await _publishEndpoint.Publish(new GameStartingSoonEvent
               {
                   GameId = game.Id,
                   Location = game.Location,
                   GameDateTime = game.DateTime,
                   Participants = game.GetAllParticipants()
               });
           }
       }
   }

   // Program.cs (après Build)
   RecurringJob.AddOrUpdate<GameReminderJob>(
       "game-reminders-2h",
       job => job.ScheduleReminders(),
       "*/5 * * * *"); // Toutes les 5 minutes
   ```

4. **Integration Event**
   ```csharp
   // Vibora.Games.Contracts/Events/GameStartingSoonEvent.cs
   public record GameStartingSoonEvent
   {
       public Guid GameId { get; init; }
       public DateTime GameDateTime { get; init; }
       public string Location { get; init; }
       public List<ParticipantInfo> Participants { get; init; }
   }

   public record ParticipantInfo
   {
       public string ExternalId { get; init; }
       public string Name { get; init; }
       public bool IsGuest { get; init; }
   }
   ```

5. **Consumer**
   ```csharp
   // Vibora.Notifications/Application/EventHandlers/GameStartingSoonEventConsumer.cs
   public sealed class GameStartingSoonEventConsumer : IConsumer<GameStartingSoonEvent>
   {
       public async Task Consume(ConsumeContext<GameStartingSoonEvent> context)
       {
           var @event = context.Message;

           var participantNames = @event.Participants
               .Select(p => p.Name)
               .ToList();

           var content = _templateService.BuildGameStartingSoonContent(
               @event.Location,
               @event.GameDateTime,
               participantNames);

           // Notifier tous les participants (registered users uniquement au MVP)
           var registeredParticipants = @event.Participants
               .Where(p => !p.IsGuest)
               .Select(p => p.ExternalId)
               .ToList();

           var deviceTokens = await _userPreferencesService.GetDeviceTokensBatchAsync(
               registeredParticipants,
               context.CancellationToken);

           foreach (var (userId, deviceToken) in deviceTokens)
           {
               await SendNotification(userId, deviceToken, content);
           }
       }
   }
   ```

6. **Repository Method**
   ```csharp
   // Vibora.Games/Infrastructure/GameRepository.cs
   public async Task<List<Game>> GetGamesInTimeWindow(
       DateTime start,
       DateTime end)
   {
       return await _context.Games
           .Include(g => g.Participations)
           .Include(g => g.GuestParticipants)
           .Where(g => g.DateTime >= start && g.DateTime < end)
           .Where(g => g.Status == GameStatus.Open || g.Status == GameStatus.Full)
           .ToListAsync();
   }
   ```

**Tests:**
- Test unitaire `GameReminderJobTests.cs`
- Test avec Hangfire InMemory
- Test E2E avec délai simulé

---

### Phase 5: Templates Contextualisés (US10.1) - 2h

**Fichier à modifier:**

```csharp
// NotificationTemplateService.cs
public NotificationContent BuildPlayerLeftContent(
    string playerName,
    LeaveReason? reason,
    string? customMessage,
    int remainingPlayers,
    string location,
    DateTime gameDateTime)
{
    string body = reason switch
    {
        LeaveReason.Personal =>
            $"{playerName} a quitté (empêchement perso). Vous pouvez continuer ({remainingPlayers}/4)",

        LeaveReason.VenueClosed =>
            $"⚠️ {playerName} a quitté - Le terrain n'est plus disponible",

        LeaveReason.Weather =>
            $"{playerName} a quitté (météo). À vous de décider ({remainingPlayers}/4)",

        LeaveReason.Other when !string.IsNullOrWhiteSpace(customMessage) =>
            $"{playerName} a quitté : {customMessage}. Places restantes ({remainingPlayers}/4)",

        _ =>
            $"{playerName} a quitté la partie. Places restantes ({remainingPlayers}/4)"
    };

    var context = new Dictionary<string, string>
    {
        ["playerName"] = playerName,
        ["remainingPlayers"] = remainingPlayers.ToString(),
        ["location"] = location,
        ["date"] = gameDateTime.ToString("dd MMM yyyy HH:mm")
    };

    return new NotificationContent("Joueur parti", body, context);
}
```

**Dépendances:**
- ❌ Enum `LeaveReason` doit être créé en Phase US10.1
- ❌ `ParticipationRemovedDomainEvent` doit être enrichi

---

## 🎨 PLAN D'IMPLÉMENTATION FRONTEND (US12 - 3 jours)

### Phase 1: Configuration Firebase - 4h

1. **Créer projet Firebase Console**
   - Nom: "Vibora MVP"
   - Activer Firebase Cloud Messaging (FCM)
   - Télécharger `firebase-adminsdk.json` (backend)
   - Récupérer `vapidKey` (frontend web push)

2. **Installer dépendances**
   ```bash
   npm install firebase
   ```

3. **Configuration Firebase**
   ```typescript
   // lib/firebase-config.ts
   import { initializeApp } from 'firebase/app';
   import { getMessaging, getToken, onMessage } from 'firebase/messaging';

   const firebaseConfig = {
     apiKey: process.env.NEXT_PUBLIC_FIREBASE_API_KEY,
     authDomain: process.env.NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN,
     projectId: process.env.NEXT_PUBLIC_FIREBASE_PROJECT_ID,
     messagingSenderId: process.env.NEXT_PUBLIC_FIREBASE_MESSAGING_SENDER_ID,
     appId: process.env.NEXT_PUBLIC_FIREBASE_APP_ID
   };

   const app = initializeApp(firebaseConfig);
   export const messaging = getMessaging(app);
   ```

4. **Variables d'environnement**
   ```env
   # .env.local
   NEXT_PUBLIC_FIREBASE_API_KEY=...
   NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN=...
   NEXT_PUBLIC_FIREBASE_PROJECT_ID=vibora-mvp
   NEXT_PUBLIC_FIREBASE_MESSAGING_SENDER_ID=...
   NEXT_PUBLIC_FIREBASE_APP_ID=...
   NEXT_PUBLIC_FIREBASE_VAPID_KEY=...
   ```

---

### Phase 2: Service Worker - 4h

1. **Créer Service Worker**
   ```javascript
   // public/firebase-messaging-sw.js
   importScripts('https://www.gstatic.com/firebasejs/10.7.1/firebase-app-compat.js');
   importScripts('https://www.gstatic.com/firebasejs/10.7.1/firebase-messaging-compat.js');

   firebase.initializeApp({
     apiKey: "...",
     authDomain: "...",
     projectId: "vibora-mvp",
     messagingSenderId: "...",
     appId: "..."
   });

   const messaging = firebase.messaging();

   messaging.onBackgroundMessage((payload) => {
     console.log('Background notification received:', payload);

     const notificationTitle = payload.notification.title;
     const notificationOptions = {
       body: payload.notification.body,
       icon: '/icon-192.png',
       badge: '/badge-72.png',
       data: {
         gameId: payload.data?.gameId,
         url: payload.data?.url
       }
     };

     self.registration.showNotification(notificationTitle, notificationOptions);
   });

   // Click handler
   self.addEventListener('notificationclick', (event) => {
     event.notification.close();

     const url = event.notification.data.url || '/';
     event.waitUntil(
       clients.openWindow(url)
     );
   });
   ```

2. **Enregistrer Service Worker**
   ```typescript
   // app/layout.tsx
   'use client';

   import { useEffect } from 'react';

   export default function RootLayout({ children }) {
     useEffect(() => {
       if ('serviceWorker' in navigator) {
         navigator.serviceWorker
           .register('/firebase-messaging-sw.js')
           .then((registration) => {
             console.log('Service Worker registered:', registration);
           })
           .catch((error) => {
             console.error('Service Worker registration failed:', error);
           });
       }
     }, []);

     return (
       <html lang="fr">
         <body>{children}</body>
       </html>
     );
   }
   ```

---

### Phase 3: Demande Permission & Device Token - 6h

1. **Hook useNotifications**
   ```typescript
   // hooks/useNotifications.ts
   import { useState, useEffect } from 'react';
   import { getToken, onMessage } from 'firebase/messaging';
   import { messaging } from '@/lib/firebase-config';
   import { viboraApi } from '@/lib/api/vibora-client';

   export function useNotifications() {
     const [permission, setPermission] = useState<NotificationPermission>('default');
     const [deviceToken, setDeviceToken] = useState<string | null>(null);

     const requestPermission = async () => {
       try {
         const permission = await Notification.requestPermission();
         setPermission(permission);

         if (permission === 'granted') {
           const token = await getToken(messaging, {
             vapidKey: process.env.NEXT_PUBLIC_FIREBASE_VAPID_KEY
           });

           setDeviceToken(token);

           // Envoyer token au backend
           await viboraApi.users.registerDeviceToken({ deviceToken: token });

           console.log('Device token registered:', token);
         }
       } catch (error) {
         console.error('Permission request failed:', error);
       }
     };

     // Écouter messages foreground
     useEffect(() => {
       if (permission === 'granted') {
         const unsubscribe = onMessage(messaging, (payload) => {
           console.log('Foreground notification:', payload);

           // Afficher toast/notification in-app
           showInAppNotification(payload.notification);
         });

         return unsubscribe;
       }
     }, [permission]);

     return {
       permission,
       deviceToken,
       requestPermission,
       isSupported: typeof window !== 'undefined' && 'Notification' in window
     };
   }

   function showInAppNotification(notification: any) {
     // TODO: Utiliser Sonner toast ou Radix Toast
     console.log('Show in-app notification:', notification);
   }
   ```

2. **Composant Permission Prompt**
   ```typescript
   // components/notifications/NotificationPermissionPrompt.tsx
   'use client';

   import { useNotifications } from '@/hooks/useNotifications';
   import { useEffect, useState } from 'react';
   import { Button } from '@/components/ui/button';
   import { Bell, BellOff } from 'lucide-react';

   export function NotificationPermissionPrompt() {
     const { permission, requestPermission, isSupported } = useNotifications();
     const [showPrompt, setShowPrompt] = useState(false);

     useEffect(() => {
       // Afficher prompt après première action (join/create game)
       const hasInteracted = localStorage.getItem('hasInteracted');
       const hasAskedPermission = localStorage.getItem('hasAskedNotificationPermission');

       if (isSupported && permission === 'default' && hasInteracted && !hasAskedPermission) {
         setShowPrompt(true);
       }
     }, [permission, isSupported]);

     const handleRequest = async () => {
       await requestPermission();
       localStorage.setItem('hasAskedNotificationPermission', 'true');
       setShowPrompt(false);
     };

     const handleDismiss = () => {
       localStorage.setItem('hasAskedNotificationPermission', 'true');
       setShowPrompt(false);
     };

     if (!showPrompt) return null;

     return (
       <div className="fixed bottom-20 left-4 right-4 bg-white border rounded-lg shadow-lg p-4 z-50">
         <div className="flex items-start gap-3">
           <Bell className="h-5 w-5 text-primary mt-0.5" />
           <div className="flex-1">
             <h3 className="font-semibold text-sm">Activer les notifications</h3>
             <p className="text-sm text-gray-600 mt-1">
               Recevez des alertes quand quelqu'un rejoint votre partie ou vous envoie un message.
             </p>
             <div className="flex gap-2 mt-3">
               <Button onClick={handleRequest} size="sm">
                 Activer
               </Button>
               <Button onClick={handleDismiss} variant="ghost" size="sm">
                 Plus tard
               </Button>
             </div>
           </div>
         </div>
       </div>
     );
   }
   ```

3. **Intégrer dans Layout**
   ```typescript
   // app/layout.tsx
   import { NotificationPermissionPrompt } from '@/components/notifications/NotificationPermissionPrompt';

   export default function RootLayout({ children }) {
     return (
       <html>
         <body>
           {children}
           <NotificationPermissionPrompt />
         </body>
       </html>
     );
   }
   ```

4. **Tracker "première interaction"**
   ```typescript
   // app/games/[id]/page.tsx
   async function handleJoinGame() {
     localStorage.setItem('hasInteracted', 'true'); // Déclenche prompt
     await viboraApi.games.joinGame(gameId);
   }
   ```

---

### Phase 4: API Client - 2h

1. **Endpoint Device Token**
   ```typescript
   // lib/api/vibora-client.ts
   export const viboraApi = {
     // ... existing methods

     users: {
       // ... existing

       async registerDeviceToken(payload: { deviceToken: string }) {
         const response = await fetch(`${API_BASE_URL}/users/device-tokens`, {
           method: 'POST',
           headers: {
             'Authorization': `Bearer ${getToken()}`,
             'Content-Type': 'application/json'
           },
           body: JSON.stringify(payload)
         });

         if (!response.ok) {
           throw new Error('Failed to register device token');
         }

         return response.json();
       }
     }
   };
   ```

---

### Phase 5: Tests & Debug - 6h

1. **Test notifications localement**
   - Firebase Console → Cloud Messaging → Send test message
   - Tester background vs foreground
   - Vérifier deep links

2. **Tests multi-devices**
   - iOS Safari (PWA)
   - Android Chrome (PWA)
   - Desktop Chrome

3. **Debugging**
   - Console Firebase: vérifier delivery
   - Logs backend: vérifier FcmNotificationChannel

---

## 📋 CHECKLIST VALIDATION US12 (Product Owner)

### Backend Infrastructure
- [ ] Firebase Admin SDK installé et configuré
- [ ] `FcmNotificationChannel.SendAsync()` implémenté et testé
- [ ] Endpoint `POST /users/device-tokens` créé
- [ ] Tests unitaires `FcmNotificationChannelTests` passent
- [ ] Tests E2E avec Firebase Test Lab passent

### Notifications Critiques MVP
- [ ] **N01** - Nouveau participant: Consumer modifié pour notifier tous les participants ✅
- [ ] **N02** - Participant quitte: Templates contextualisés selon raisons (après US10.1) ✅
- [ ] **N03** - Partie complète: Event + consumer + template créés ✅
- [ ] **N04** - Rappel 2h avant: Hangfire + job + consumer implémentés ✅
- [ ] **N05** - Partie annulée: Déjà complet backend ✅

### Frontend FCM
- [ ] Projet Firebase Console créé
- [ ] Service Worker `firebase-messaging-sw.js` créé et enregistré
- [ ] Hook `useNotifications` implémenté
- [ ] Composant `<NotificationPermissionPrompt>` créé
- [ ] Device tokens envoyés au backend après permission
- [ ] Notifications foreground affichées via toast
- [ ] Notifications background affichées via service worker
- [ ] Deep links fonctionnels (click → ouvre game detail)

### Tests Validation
- [ ] Test unitaire: Envoi notification via `FcmNotificationChannel`
- [ ] Test E2E: Rejoindre partie → tous les participants reçoivent notif
- [ ] Test E2E: Quitter partie → notif avec raison correcte
- [ ] Test E2E: Partie complète → hôte reçoit notif
- [ ] Test E2E: Rappel 2h avant → tous reçoivent notification
- [ ] Test multi-device (iOS Safari, Android Chrome, Desktop)

### Documentation
- [ ] README backend: configuration Firebase Admin SDK
- [ ] README frontend: configuration Firebase Web
- [ ] Backlog US12: statut mis à jour ✅
- [ ] CHANGELOG: Ajout date complétion notifications

---

## 🚨 RISQUES & MITIGATIONS

### Risque 1: Firebase Free Tier Insuffisant
**Impact:** Limite 10M messages/mois
**Probabilité:** Faible (MVP < 1000 users)
**Mitigation:** Monitoring quotas + plan Blaze si dépassement

### Risque 2: iOS Safari Limitations PWA
**Impact:** Notifications push limitées sur iOS < 16.4
**Probabilité:** Moyenne
**Mitigation:**
- Détecter version iOS
- Fallback: prompt "Ajoutez à l'écran d'accueil"
- Alternative: Email notifications pour iOS < 16.4

### Risque 3: Hangfire Memory Leak (Jobs)
**Impact:** Crash backend si 1000+ jobs/jour
**Probabilité:** Faible (MVP)
**Mitigation:**
- Configuration `JobStorageConnection` timeout
- Monitoring RAM serveur
- Fallback: Quartz.NET si problème

### Risque 4: US10.1 Bloque N02
**Impact:** N02 (raisons départ) dépend de US10.1
**Probabilité:** Haute
**Mitigation:**
- **Plan A:** Implémenter US10.1 AVANT US12 (recommandé)
- **Plan B:** Implémenter N02 sans raisons d'abord, enrichir après

### Risque 5: Guests Non Notifiés
**Impact:** Guests ne reçoivent pas rappels 2h avant
**Probabilité:** 100% (design MVP)
**Mitigation:**
- Documenter limitation
- V2: SMS/Email channel pour guests
- MVP: Accepter que guests = secondaires

---

## 📚 RÉFÉRENCES

### Documents Consultés
1. `docs/Conception UX minimaliste et centrée utilisateur pour le MVP de Vibora.md`
   - Lignes 47-52: Chat contextuel
   - Lignes 78-79: Notifications nouveau joueur
   - Lignes 101-104: Conversion post-partie

2. `docs/Backlog du MVP Vibora.md`
   - US12: Notifications Push (ligne 236)
   - US10.1: Message contextualisé départ (ligne 198)

### Code Backend Analysé
- `Vibora.Games/Domain/Events/*DomainEvent.cs` (6 events)
- `Vibora.Notifications/Application/EventHandlers/*EventConsumer.cs` (4 consumers)
- `Vibora.Notifications/Infrastructure/Services/NotificationTemplateService.cs`
- `Vibora.Notifications/Domain/NotificationType.cs`

### Standards Techniques
- Firebase Cloud Messaging (FCM): https://firebase.google.com/docs/cloud-messaging
- Hangfire: https://docs.hangfire.io/en/latest/
- Service Workers: https://developer.mozilla.org/en-US/docs/Web/API/Service_Worker_API

---

## 🎯 CONCLUSION & RECOMMANDATIONS PO

### Scope MVP Notifications (Validé)

**5 notifications critiques:**
1. ✅ N01 - Nouveau participant rejoint (multi-destinataires)
2. ✅ N02 - Participant quitte avec raisons (après US10.1)
3. ✅ N03 - Partie complète (4/4)
4. ✅ N04 - Rappel 2h avant (job scheduler)
5. ✅ N05 - Partie annulée (déjà complet)

**Total effort:** 7-8 jours (backend + frontend FCM complet)

### Priorisation Recommandée

**Sprint 1 (US12 + US10.1):**
1. **Jour 1-2:** Backend FCM infrastructure (Firebase Admin SDK, endpoint device tokens)
2. **Jour 3-4:** US10.1 (raisons de départ) - BLOQUANT pour N02
3. **Jour 5-7:** Frontend FCM complet (service worker, permissions, device tokens)
4. **Jour 8:** Consumers multi-destinataires (N01, N02)
5. **Jour 9:** Event GameCompleted + consumer (N03)
6. **Jour 10-11:** Hangfire + job rappels 2h avant (N04)
7. **Jour 12:** Tests E2E + validation multi-devices

**Post-MVP (V2):**
- N06: Analytics partages (pas critique)
- N07: Chat + notifications messages (Epic Chat)
- N08-N11: Features gamification/social

### Critères de Validation MVP

✅ **MVP prêt pour release SI:**
1. Infrastructure FCM backend + frontend opérationnelle
2. Les 5 notifications critiques (N01-N05) fonctionnent E2E
3. Tests passent sur iOS Safari + Android Chrome
4. Hangfire job rappels exécuté toutes les 5min sans erreur
5. Device tokens enregistrés et notifications reçues < 2s

❌ **Bloquer release SI:**
- Notifications non reçues (Firebase delivery rate < 95%)
- Service worker crash sur iOS/Android
- Hangfire job échoue > 10% du temps
- US10.1 non complétée (N02 incomplet)

### Décisions Architecturales Validées

1. ✅ **Hangfire** (vs Quartz.NET) pour MVP - plus simple
2. ✅ **Firebase Cloud Messaging** (vs OneSignal) - gratuit + officiel
3. ✅ **MassTransit** pour events (déjà en place)
4. ✅ **Guests non notifiés au MVP** (SMS/Email post-MVP)
5. ✅ **Service Worker** (vs WebSocket) pour PWA compliance

---

**Document validé par:** Product Owner Vibora
**Prochaine étape:** Planifier sprint US10.1 + US12 (12 jours)
**Mise à jour backlog:** Remplacer description vague US12 par référence à ce document
