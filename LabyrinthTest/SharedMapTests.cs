using Labyrinth.Exploration;
using NUnit.Framework;

namespace LabyrinthTest;

public class SharedMapTests
{
    [Test]
    public void Unknown_should_not_override_known()
    {
        var map = new SharedMap();
        map.Update(1,1, TileKnowledge.Room);
        map.Update(1,1, TileKnowledge.Unknown);
        Assert.That(map.Get(1,1), Is.EqualTo(TileKnowledge.Room));
    }
}