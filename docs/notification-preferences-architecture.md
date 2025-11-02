# Architecture des Préférences de Notifications

## Vue d'ensemble

Les préférences de notifications utilisateur sont stockées dans le **module Users** plutôt que dans un module séparé. Cette décision architecturale privilégie la cohésion et la simplicité.

## Décision Architecturale : Pourquoi Users et pas un nouveau module ?

### ✅ Avantages

1. **Cohésion forte** : Les préférences de notifications sont intrinsèquement liées à l'utilisateur
2. **Simplicité** : Évite la complexité d'un module supplémentaire pour une entité relativement simple
3. **Performance** : Requêtes directes sans traverser plusieurs modules
4. **Maintenance** : Un seul module à gérer pour tout ce qui concerne l'utilisateur

### Entité UserNotificationSettings

```
Users Module
  ├── Domain/
  │   ├── User.cs
  │   ├── UserNotificationSettings.cs          ✨ NEW
  │   ├── IUserRepository.cs
  │   └── IUserNotificationSettingsRepository.cs  ✨ NEW
  ├── Application/
  │   ├── Commands/
  │   │   └── UpdateNotificationSettings/       ✨ NEW
  │   └── Queries/
  │       └── GetUserNotificationSettings/      ✨ NEW
  └── Infrastructure/
      ├── Data/
      │   └── UsersDbContext.cs (updated)
      └── Persistence/
          └── UserNotificationSettingsRepository.cs ✨ NEW
```

## Communication Inter-Modules : Pattern Monolithe Modulaire

### ⚠️ PRINCIPE PRIMORDIAL

**Les appels inter-modules DOIVENT supporter deux modes de déploiement :**
- **Mode Monolithe** : Appels in-process via MediatR (même processus)
- **Mode Microservices** : Appels HTTP via HttpClient (processus séparés)

### Architecture de Communication

```
┌─────────────────────────────────────────────────────────────┐
│  Notifications Module                                        │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  UserPreferencesService                              │  │
│  │  (Application Layer)                                 │  │
│  └──────────────────┬───────────────────────────────────┘  │
│                     │                                        │
│                     │ Depends on                             │
│                     ▼                                        │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  IUsersServiceClient (Interface)                     │  │
│  │  (Infrastructure Layer - PUBLIC)                     │  │
│  └──────────────────┬───────────────────────────────────┘  │
│                     │                                        │
│           ┌─────────┴─────────┐                             │
│           ▼                   ▼                             │
│  ┌────────────────┐  ┌────────────────────┐               │
│  │ InProcessClient│  │ HttpClient         │               │
│  │ (Monolith)     │  │ (Microservices)    │               │
│  └────────────────┘  └────────────────────┘               │
└─────────────────────────────────────────────────────────────┘

         MediatR (in-process)           HTTP (cross-process)
                  │                              │
                  ▼                              ▼
         ┌─────────────────────────────────────────────┐
         │  Users Module                                │
         │  - GetUserNotificationSettingsQueryHandler  │
         └─────────────────────────────────────────────┘
```

### Implémentation

#### 1. Interface Publique (`IUsersServiceClient`)

```csharp
public interface IUsersServiceClient
{
    Task<UserNotificationSettingsDto?> GetUserNotificationSettingsAsync(
        string userExternalId, 
        CancellationToken cancellationToken = default);
}
```

#### 2. Mode Monolithe : `UsersServiceInProcessClient`

```csharp
public sealed class UsersServiceInProcessClient : IUsersServiceClient
{
    private readonly ISender _sender;

    public async Task<UserNotificationSettingsDto?> GetUserNotificationSettingsAsync(
        string userExternalId,
        CancellationToken cancellationToken = default)
    {
        // Appel direct via MediatR (in-process)
        var query = new GetUserNotificationSettingsQuery(userExternalId);
        var result = await _sender.Send(query, cancellationToken);
        // ...
    }
}
```

#### 3. Mode Microservices : `UsersServiceHttpClient`

```csharp
public sealed class UsersServiceHttpClient : IUsersServiceClient
{
    private readonly HttpClient _httpClient;

    public async Task<UserNotificationSettingsDto?> GetUserNotificationSettingsAsync(
        string userExternalId,
        CancellationToken cancellationToken = default)
    {
        // Appel HTTP vers le service Users distant
        var response = await _httpClient.GetAsync(
            $"/api/users/{userExternalId}/notification-settings", 
            cancellationToken);
        // ...
    }
}
```

#### 4. Enregistrement dans `Program.cs`

Le **Host** choisit l'implémentation selon la configuration :

```csharp
static void ConfigureCrossModuleCommunication(IServiceCollection services, IConfiguration configuration)
{
    var deploymentMode = configuration.GetValue<string>("DeploymentMode") ?? "Monolith";

    if (deploymentMode.Equals("Monolith", StringComparison.OrdinalIgnoreCase))
    {
        // Mode Monolithe : In-process
        services.AddScoped<IUsersServiceClient, UsersServiceInProcessClient>();
    }
    else if (deploymentMode.Equals("Microservices", StringComparison.OrdinalIgnoreCase))
    {
        // Mode Microservices : HTTP
        var usersServiceUrl = configuration["Services:UsersService:Url"];
        services.AddHttpClient<IUsersServiceClient, UsersServiceHttpClient>(client =>
        {
            client.BaseAddress = new Uri(usersServiceUrl);
        });
    }
}
```

### Utilisation dans les Event Handlers

```csharp
internal sealed class GameCanceledEventConsumer : IConsumer<GameCanceledEvent>
{
    private readonly UserPreferencesService _userPreferencesService;

    public async Task Consume(ConsumeContext<GameCanceledEvent> context)
    {
        // Récupération des préférences (fonctionne en monolithe ET microservices)
        var deviceToken = await _userPreferencesService.GetUserDeviceTokenAsync(
            userExternalId, 
            context.CancellationToken);
        // ...
    }
}
```

## GameCanceledEvent Enrichi

### Problème Initial
Le module Notifications devait requêter le module Games pour obtenir les infos des participants.

### Solution
`GameCanceledEvent` contient maintenant toutes les informations nécessaires :

```csharp
public record GameCanceledEvent
{
    public Guid GameId { get; init; }
    public string HostExternalId { get; init; }
    public DateTime GameDateTime { get; init; }
    public string Location { get; init; }
    
    // ✨ NEW: Liste des participants enregistrés (excluant l'hôte)
    public List<ParticipantInfo> Participants { get; init; }
    
    // ✨ NEW: Liste des invités
    public List<GuestParticipantInfo> GuestParticipants { get; init; }
}
```

### Bénéfices

- **Découplage** : Le module Notifications n'a plus besoin de requêter Games
- **Performance** : Évite les requêtes inter-modules
- **Consistance** : Les données sont atomiques dans l'événement
- **Pattern établi** : Suit le même pattern que `PlayerJoinedEvent` et `GuestJoinedEvent`

## GameCanceledEventConsumer Mis à Jour

Le consumer envoie maintenant des notifications à tous les participants :

```csharp
// Notifier tous les participants enregistrés (sauf l'hôte qui a annulé)
foreach (var participant in @event.Participants)
{
    var command = new SendNotificationCommand(
        participant.UserExternalId,
        NotificationType.GameCancelled,
        NotificationChannel.Push,
        "stub-device-token", // TODO: Fetch from UserNotificationSettings
        content);
    
    await _sender.Send(command, cancellationToken);
}

// TODO: Notifier les invités via SMS/Email (post-MVP)
foreach (var guest in @event.GuestParticipants)
{
    // Envoyer SMS/Email selon les coordonnées disponibles
}
```

## Prochaines Étapes (TODO)

### 1. Créer la migration de base de données

```bash
cd src/modules/Users/Vibora.Users
dotnet ef migrations add AddUserNotificationSettings
```

### 2. Intégrer les préférences dans les event handlers

Modifier tous les event consumers du module Notifications pour :
- Récupérer les préférences via `GetUserNotificationSettingsQuery`
- Utiliser le vrai `DeviceToken` au lieu du stub
- Respecter les préférences utilisateur (PushEnabled, etc.)

### 3. Créer les endpoints API

```csharp
// POST /api/users/me/notification-settings
// GET  /api/users/me/notification-settings
// PUT  /api/users/me/notification-settings
```

### 4. Créer des settings par défaut lors de l'inscription

Modifier `SyncUserFromAuthCommandHandler` pour créer automatiquement des `UserNotificationSettings` par défaut.

### 5. Implémenter les notifications pour les invités

Une fois les canaux SMS/Email fonctionnels, mettre à jour `GameCanceledEventConsumer` pour envoyer des notifications aux invités.

## Tests Mis à Jour

Les tests suivants ont été corrigés pour refléter les changements :

- ✅ `GameCanceledDomainEventHandlerTests` - Tests avec participants et invités
- ✅ `GameCancelTests` - Vérification de l'inclusion des participants dans l'événement domain

## Architecture Finale

```
┌─────────────────────┐
│   Users Module      │
│                     │
│  - User             │
│  - UserNotification │
│    Settings         │
└──────────┬──────────┘
           │
           │ Query via MediatR
           │
           ▼
┌─────────────────────┐       ┌──────────────────┐
│ Notifications       │◄──────│  Games Module    │
│ Module              │       │                  │
│                     │       │  GameCanceled    │
│  Event Consumers:   │       │  Event (enrichi) │
│  - GameCanceled     │       └──────────────────┘
│  - PlayerJoined     │
│  - GuestJoined      │       Integration Events
│  - ParticipationRem │       via MassTransit
└─────────────────────┘
```

## Conformité aux Bonnes Pratiques

✅ **SOLID** : Séparation des responsabilités  
✅ **Clean Architecture** : Dépendances inversées via interfaces  
✅ **DDD** : Agrégats bien définis avec invariants  
✅ **Event-Driven** : Communication asynchrone entre modules  
✅ **CQRS** : Séparation commandes/queries  

---

**Note** : Cette architecture suit les patterns établis dans le projet et respecte les principes de Clean Architecture et DDD.
