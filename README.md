# TrainingServer

## Description
le serveur d’entraînement permet de piloter des crawlers dans un labyrinthe via une API REST.
Il permet :
- la création de crawlers
- leur déplacement dans le labyrinthe
- la collecte d’objets (clés)
- l’ouverture de portes
- la suppression des crawlers

Les tests sont réalisés via Swagger (`http://localhost:5030/swagger`), Après avoir exécuté la commande dotnet run --project TrainingServer

## Exemple : 
- appKey = 00000000-0000-0000-0000-000000000000
--> cela génère un id qui sera utilisé ensuite ( collé dans l'espace adequat)

### Tourner le crwaler
{
  "direction": "East",
  "walking": false
}

### Avancer
changer: "walking": true

### Trouver une clé
"items": [ { "type": "Key" } ]

### Ramasser la clé :
Dans PUT /crawlers/{id}/items :
[
  { "type": "Key", "move-required": true }
]

### Ouvrir une porte
"facing-tile": "Door"

Faire un Patch avce :
"walking": true

### Supprimer le crawler :
DELETE /crawlers/{id}

