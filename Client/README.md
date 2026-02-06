# Client Labyrinthe

Client console pour explorer un labyrinthe via un serveur d'entraînement ou de compétition.

## Utilisation

```bash
dotnet run --project Client <serverUrl> <appKey>
```

### Arguments

- `serverUrl` : Adresse du serveur (ex: `http://localhost:5000`)
- `appKey` : Clé d'application (GUID fourni par le serveur)

### Exemple

```bash
dotnet run --project Client http://localhost:5000 12345678-1234-1234-1234-123456789abc
```

## Stratégie d'exploration

Le client utilise un algorithme BFS (Breadth-First Search) pour explorer le labyrinthe :
- Maintient une carte partagée des zones connues
- Identifie les cellules frontières (cases adjacentes inexplorées)
- Calcule le chemin le plus court vers la frontière la plus proche
- Continue jusqu'à explorer toutes les zones accessibles
