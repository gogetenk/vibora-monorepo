# Optimisations de la requête JoinGameAsGuest

**Date**: 4 novembre 2024  
**Module**: Games  
**Endpoint optimisé**: `POST /games/{id:guid}/players/guest`

## 📊 Analyse de la trace initiale

D'après la trace fournie, la requête totale prenait **34.06ms** avec plusieurs sous-requêtes PostgreSQL:
- Plusieurs requêtes DATA postgresql viboradb avec des temps variant de 2.61ms à 4.58ms
- Détection d'un problème potentiel de **cartesian explosion** avec les includes multiples

## ✅ Optimisations implémentées

### 1. **Optimisation des requêtes EF Core avec AsSplitQuery()**

**Fichier modifié**: `GameRepository.cs`

**Problème identifié**: 
Les méthodes `GetByIdAsync`, `GetByIdWithParticipationsAsync` et `GetGamesByUserAsync` utilisent plusieurs `.Include()` ce qui peut créer un **cartesian explosion** (multiplication du nombre de lignes retournées dans un seul résultat JOIN).

**Solution appliquée**:
Ajout de `.AsSplitQuery()` pour séparer les requêtes:
- Au lieu d'une seule requête avec LEFT JOIN qui multiplie les lignes
- EF Core génère maintenant 3 requêtes séparées (plus efficaces):
  1. SELECT pour `Games`
  2. SELECT pour `Participations` 
  3. SELECT pour `GuestParticipants`

```csharp
var game = await _dbContext.Games
    .Include(g => g.Participations)
    .Include(g => g.GuestParticipants)
    .AsSplitQuery() // ✅ Nouveau
    .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
```

**Impact attendu**: 
- Réduction de 20-40% du temps de requête pour les jeux avec plusieurs participants
- Évite la duplication de données dans les résultats
- Meilleure utilisation des index PostgreSQL

### 2. **Nouveaux index composites pour améliorer les performances**

**Fichier créé**: `20251104100000_AddPerformanceIndexes.cs`

#### Index ajoutés:

1. **`IX_Participations_GameId_IsHost`**
   ```sql
   CREATE INDEX ON "Participations" ("GameId", "IsHost")
   ```
   - **Utilité**: Recherche rapide de l'hôte d'un jeu spécifique
   - **Requête optimisée**: `WHERE GameId = @id AND IsHost = true`

2. **`IX_Games_Status_CurrentPlayers_MaxPlayers`**
   ```sql
   CREATE INDEX ON "Games" ("Status", "CurrentPlayers", "MaxPlayers")
   ```
   - **Utilité**: Filtrage rapide des jeux ouverts avec des places disponibles
   - **Requête optimisée**: `WHERE Status = 'Open' AND CurrentPlayers < MaxPlayers`

3. **`IX_GuestParticipants_GuestExternalId`** (avec filtre)
   ```sql
   CREATE INDEX ON "GuestParticipants" ("GuestExternalId") 
   WHERE "GuestExternalId" IS NOT NULL
   ```
   - **Utilité**: Recherche rapide d'un guest par son ID externe (réconciliation)
   - **Requête optimisée**: `WHERE GuestExternalId = @guestId`

4. **`IX_Participations_UserExternalId_GameId`**
   ```sql
   CREATE INDEX ON "Participations" ("UserExternalId", "GameId")
   ```
   - **Utilité**: Vérification rapide si un utilisateur a déjà rejoint un jeu spécifique (doublon)
   - **Requête optimisée**: `WHERE UserExternalId = @userId AND GameId = @gameId`

**Impact attendu**: 
- Réduction de 30-50% du temps des requêtes WHERE avec ces colonnes
- Amélioration des JOINs dans les requêtes complexes
- Index partiels (avec filtre) optimisent l'espace disque

## 📈 Gains de performance attendus

### Avant optimisation
- **Temps total**: ~34ms
- **Requêtes SQL**: 4-5 requêtes avec possibles duplications
- **Cartesian explosion**: Risque élevé avec plusieurs participants

### Après optimisation (estimation)
- **Temps total**: ~20-25ms (**-30% à -40%**)
- **Requêtes SQL**: 3-4 requêtes optimisées sans duplication
- **Cartesian explosion**: Éliminé avec AsSplitQuery()

## 🔍 Index existants conservés

Les index suivants étaient déjà présents et ont été conservés:
- ✅ `IX_Games_Status_DateTime` - Pour les requêtes de jeux ouverts par date
- ✅ `IX_Participations_UserExternalId` - Pour trouver les jeux d'un utilisateur
- ✅ `IX_Participations_GameId` - Pour charger les participants d'un jeu
- ✅ `IX_GuestParticipants_GameId` - Pour charger les invités d'un jeu
- ✅ `IX_GuestParticipants_PhoneNumber` (avec filtre) - Pour réconciliation
- ✅ `IX_GuestParticipants_Email` (avec filtre) - Pour réconciliation

## 🚀 Déploiement

### 1. Appliquer la migration

```bash
cd vibora-backend/src/modules/Games/Vibora.Games
dotnet ef database update
```

### 2. Vérifier les index créés

```sql
-- Vérifier tous les index de la table Participations
SELECT indexname, indexdef 
FROM pg_indexes 
WHERE tablename = 'Participations';

-- Vérifier tous les index de la table Games
SELECT indexname, indexdef 
FROM pg_indexes 
WHERE tablename = 'Games';

-- Vérifier tous les index de la table GuestParticipants
SELECT indexname, indexdef 
FROM pg_indexes 
WHERE tablename = 'GuestParticipants';
```

### 3. Analyser les performances avec EXPLAIN

```sql
-- Exemple de requête à tester
EXPLAIN ANALYZE
SELECT * FROM "Games" g
LEFT JOIN "Participations" p ON g."Id" = p."GameId"
LEFT JOIN "GuestParticipants" gp ON g."Id" = gp."GameId"
WHERE g."Id" = 'your-game-id';
```

## 📝 Notes importantes

1. **AsSplitQuery()** est maintenant la stratégie par défaut pour les requêtes avec plusieurs includes
2. Les **index composites** suivent l'ordre des colonnes dans les clauses WHERE (ordre important!)
3. Les **index partiels** avec filtre réduisent la taille de l'index et améliorent les performances
4. Le code compile sans erreur et est prêt pour le déploiement

## 🔗 Références

- [EF Core Split Queries](https://learn.microsoft.com/en-us/ef/core/querying/single-split-queries)
- [PostgreSQL Composite Indexes](https://www.postgresql.org/docs/current/indexes-multicolumn.html)
- [PostgreSQL Partial Indexes](https://www.postgresql.org/docs/current/indexes-partial.html)
