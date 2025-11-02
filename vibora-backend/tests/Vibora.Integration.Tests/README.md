# Vibora Integration Tests

Tests d'intégration end-to-end avec **WebApplicationFactory** et **TestContainers PostgreSQL**.

## État Actuel

✅ **78 tests passent** (68 Games, 6 Users, 4 Notifications)
✅ **0 tests flaky** (polling robuste au lieu de Task.Delay)
✅ **34% moins de code** après refactoring (2,200 vs 3,300 lignes)

## Architecture

### Classes de Base

- **`IntegrationTestBaseImproved`** - Tests standards HTTP
- **`EventIntegrationTestBase`** - Tests événements MassTransit
- **`TestDataSeeder`** - Seeding centralisé avec builders
- **`GameBuilder`** / **`UserBuilder`** - Fluent API pour données de test

### Infrastructure

- **`ViboraWebApplicationFactory`** - Lance PostgreSQL via TestContainers
- **`HttpClientExtensions`** - Helpers pour auth et deserialization
- **`TestJwtGenerator`** - Génération de tokens JWT pour tests

## Exemples

### Test Standard

```csharp
public class MyTest : IntegrationTestBaseImproved
{
    [Fact]
    public async Task MyScenario_ShouldSucceed()
    {
        // Arrange
        var user = await Seeder.SeedUserAsync(u => u
            .WithExternalId("auth0|user")
            .Advanced());

        AuthenticateAs(user.ExternalId);

        // Act
        var response = await Client.GetAsync("/endpoint");

        // Assert
        var result = await response.ReadAsAsync<MyDto>();
        result.Should().NotBeNull();
    }
}
```

### Test Événement

```csharp
public class MyEventTest : EventIntegrationTestBase
{
    [Fact]
    public async Task Action_ShouldPublishEvent()
    {
        // Arrange
        var (host, game) = await Seeder.SeedGameWithHostAsync();
        AuthenticateAs(host.ExternalId);

        // Act
        await Client.PostAsync($"/games/{game.Id}/cancel", null);

        // Assert
        var received = await WaitForEventAsync<GameCanceledEvent>(
            msg => msg.Context.Message.GameId == game.Id);

        received.Should().BeTrue();
    }
}
```

## Commandes

```bash
# Run all tests
dotnet test tests/Vibora.Integration.Tests

# Run specific module
dotnet test --filter FullyQualifiedName~Games
dotnet test --filter FullyQualifiedName~Users
dotnet test --filter FullyQualifiedName~Notifications

# Run specific test
dotnet test --filter FullyQualifiedName~CreateGameIntegrationTests
```

## Dépendances

- **WebApplicationFactory** - Tests end-to-end
- **TestContainers PostgreSQL** - Base de données isolée
- **MassTransit.Testing** - Test harness pour événements
- **FluentAssertions** - Assertions expressives
- **xUnit** - Framework de test

## Guide de Refactoring

Voir **REFACTORING_GUIDE.md** pour patterns détaillés et migration complète.
