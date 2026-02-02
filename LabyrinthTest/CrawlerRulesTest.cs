using Labyrinth.Build;
using Labyrinth.Items;
using Labyrinth.Tiles;
using NUnit.Framework;

namespace LabyrinthTest;

public class CrawlerRulesTest
{
    // construit un labyrinthe depuis ASCII et retourne (lab, crawler, bag)
    private static (Labyrinth.Labyrinth lab, Labyrinth.Crawl.ICrawler crawler, Inventory bag) Create(string ascii)
    {
        var lab = new Labyrinth.Labyrinth(new AsciiParser(ascii));
        var crawler = lab.NewCrawler();
        var bag = new MyInventory();
        return (lab, crawler, bag);
    }

    private const string ValidMazeWithDoorsAndKeys = """
+---------------+
|      /        |
| +-----------+ |
| |  /        | |
| | +-------+ | |
| | | x   k | | |
| | | +---+ | | |
| | | |   | | | |
| | +-------+ | |
| |        /  | |
| +-----------+ |
|      k   k    |
+---------------+
""";

// Déplacement déterministe: règle "main gauche"
// - si mur devant => tourne à gauche
// - sinon => tente d'avancer
private static async Task StepLeftHandAsync(Labyrinth.Crawl.ICrawler crawler, Inventory bag)
{
    var facing = await crawler.FacingTileType;
    if (facing == typeof(Wall))
    {
        crawler.Direction.TurnLeft();
        return;
    }

    var roomContent = await crawler.TryWalk(bag);
    if (roomContent is Inventory inv)
    {
        await bag.TryMoveItemsFrom(inv, inv.ItemTypes.Select(_ => true).ToList());
    }
}

// Cherche à se mettre face à une porte en explorant (main gauche)
private static async Task<bool> FaceDoorAsync(Labyrinth.Crawl.ICrawler crawler, Inventory bag, int maxSteps)
{
    for (int i = 0; i < maxSteps; i++)
    {
        // essayer les 4 orientations sur place (ça coûte presque rien)
        for (int r = 0; r < 4; r++)
        {
            if (await crawler.FacingTileType == typeof(Door))
                return true;
            crawler.Direction.TurnLeft();
        }

        await StepLeftHandAsync(crawler, bag);
    }
    return false;
}


    [Test]
    public async Task Wall_ahead_TryWalk_returns_null_and_position_does_not_change()
    {
        
        // On va forcer la direction vers un mur en tournant jusqu’à avoir FacingTileType == Wall
        var ascii = """
+---+
| x |
|###|
+---+
""";

        
        ascii = """
+---+
| x |
+---+
""";

        var (lab, crawler, bag) = Create(ascii);

        // On cherche une direction où il y a un mur devant.
        // On fait 4 rotations max.
        bool found = false;
        for (int i = 0; i < 4; i++)
        {
            var facing = await crawler.FacingTileType;
            if (facing == typeof(Wall))
            {
                found = true;
                break;
            }
            crawler.Direction.TurnLeft();
        }

        Assert.That(found, Is.True, "On doit pouvoir trouver un mur devant dans cette map de test.");

        var x0 = crawler.X;
        var y0 = crawler.Y;

        var res = await crawler.TryWalk(bag);

        Assert.That(res, Is.Null, "TryWalk doit échouer si mur devant.");
        Assert.That(crawler.X, Is.EqualTo(x0), "X ne doit pas changer.");
        Assert.That(crawler.Y, Is.EqualTo(y0), "Y ne doit pas changer.");
    }

    [Test]
    public async Task Room_ahead_TryWalk_returns_inventory_and_position_changes()
    {
        // Map avec au moins une case vide accessible devant le crawler.
        // On tourne jusqu’à ce que FacingTileType == Room puis on avance.
        var ascii = """
+-----+
| x   |
|     |
+-----+
""";

        var (lab, crawler, bag) = Create(ascii);

        bool found = false;
        for (int i = 0; i < 4; i++)
        {
            var facing = await crawler.FacingTileType;
            if (facing == typeof(Room))
            {
                found = true;
                break;
            }
            crawler.Direction.TurnLeft();
        }

        Assert.That(found, Is.True, "On doit pouvoir trouver une room devant dans cette map.");

        var x0 = crawler.X;
        var y0 = crawler.Y;

        var res = await crawler.TryWalk(bag);

        Assert.That(res, Is.Not.Null, "TryWalk doit renvoyer un Inventory si la room est atteinte.");
        Assert.That(crawler.X != x0 || crawler.Y != y0, Is.True, "La position doit changer après un move réussi.");
    }

    [Test]
    
public async Task Door_ahead_without_key_TryWalk_fails_and_position_does_not_change()
{
    var (lab, crawler, bag) = Create(ValidMazeWithDoorsAndKeys);

    // Bag vide pour être sûr qu'on n'a aucune clé
    Assert.That(bag.ItemTypes.Any(), Is.False);

    // Se mettre face à une porte
    var found = await FaceDoorAsync(crawler, bag, maxSteps: 600);
    Assert.That(found, Is.True, "On doit pouvoir se mettre face à une porte dans le labyrinthe de test.");

    var x0 = crawler.X;
    var y0 = crawler.Y;

    // Tenter d'avancer dans la porte sans clé
    var res = await crawler.TryWalk(bag);

    Assert.That(res, Is.Null, "Sans clé, la porte ne doit pas s’ouvrir.");
    Assert.That(crawler.X, Is.EqualTo(x0), "X ne doit pas changer.");
    Assert.That(crawler.Y, Is.EqualTo(y0), "Y ne doit pas changer.");
}


    [Test]
    public async Task Door_ahead_with_key_TryWalk_succeeds_and_position_changes()
{
    var (lab, crawler, bag) = Create(ValidMazeWithDoorsAndKeys);

    // Étape 1 : explorer jusqu'à récupérer au moins une clé
    // (règle main gauche + transfert inventaire room -> bag)
    for (int i = 0; i < 800 && !bag.ItemTypes.Any(); i++)
    {
        await StepLeftHandAsync(crawler, bag);
    }

    Assert.That(bag.ItemTypes.Any(), Is.True, "On doit récupérer au moins une clé dans le bag.");

    // Étape 2 : essayer d'ouvrir une porte.
    // IMPORTANT: chaque porte peut demander une clé spécifique.
    // Donc on boucle : se mettre face à une porte, tenter, si échec continuer explorer (peut récupérer d'autres clés).
    bool opened = false;

    for (int attempt = 0; attempt < 6 && !opened; attempt++)
    {
        var foundDoor = await FaceDoorAsync(crawler, bag, maxSteps: 600);
        Assert.That(foundDoor, Is.True, "On doit pouvoir retrouver une porte.");

        var x0 = crawler.X;
        var y0 = crawler.Y;

        var res = await crawler.TryWalk(bag);

        if (res is not null && (crawler.X != x0 || crawler.Y != y0))
        {
            opened = true;
            break;
        }

        // Si ça n'a pas ouvert, on continue à explorer pour récupérer d'autres clés
        for (int i = 0; i < 400 && !opened; i++)
        {
            await StepLeftHandAsync(crawler, bag);
        }
    }

    Assert.That(opened, Is.True, "Avec des clés récupérées, on doit finir par ouvrir une porte et bouger.");
}

}
