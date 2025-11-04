using Labyrinth;
using Labyrinth.Sys;
using Moq;
using static Labyrinth.RandExplorer;

namespace LabyrinthTest;

public class ExplorerTest
{
    public RandExplorer NewExplorerFor(
        string labyrinth, 
        params Actions[] actions
    ) {
        var laby = new Labyrinth.Labyrinth(labyrinth);
        var mockRnd = new Mock<IEnumRandomizer<Actions>>();

        mockRnd.Setup(r => r.Next()).Returns(
            new Queue<Actions>(actions).Dequeue
        );
        return new RandExplorer(
            laby.NewCrawler(),
            mockRnd.Object
        );
    }

    [Test]
    public void GetOutNegativeThrowsException()
    {
        var test = NewExplorerFor("""
            + +
            |x|
            +-+
            """
        );
        Assert.That(
            () => test.GetOut(-3), 
            Throws.TypeOf<ArgumentOutOfRangeException>()
        );
    }

    [Test]
    public void GetOutZeroThrowsException()
    {
        var test = NewExplorerFor("""
            + +
            |x|
            +-+
            """
        );
        Assert.That(
            () => test.GetOut(0), 
            Throws.TypeOf<ArgumentOutOfRangeException>()
        );
    }

    [Test]
    public void GetOutInHole()
    {
        var test = NewExplorerFor("""
            +-+
            |x/
            +-+
            |k|
            +-+
            """
        );
        var left = test.GetOut(10);

        Assert.That(left, Is.EqualTo(0));
    }

    [Test]
    public void GetOutFacingOutsideAtStart()
    {
        var test = NewExplorerFor("""
            | x |
            |   |
            +---+
            """
        );
        var left = test.GetOut(10);

        Assert.That(left, Is.EqualTo(10));
    }

    [Test]
    public void GetOutRotatingOnce()
    {
        var test = NewExplorerFor("""
            --+
              |
            x |
            --+
            """, 
            Actions.TurnLeft
        );
        var left = test.GetOut(10);

        Assert.That(left, Is.EqualTo(9));
    }

    [Test]
    public void GetOutRotatingTwice()
    {
        var test = NewExplorerFor("""
            +---+
            |   |
            | x |
            """,
            Actions.TurnLeft,
            Actions.TurnLeft
        );
        var left = test.GetOut(10);

        Assert.That(left, Is.EqualTo(8));
    }

    [Test]
    public void GetOutWalkingOnce()
    {
        var test = NewExplorerFor("""
            --+
             x|
            --+
            """,
            // auto turn left
            Actions.Walk
        );
        var left = test.GetOut(10);

        Assert.That(left, Is.EqualTo(8));
    }

    [Test]
    public void GetOutWalkingExactMoves()
    {
        var test = NewExplorerFor("""
            ---+
              x|
            ---+
            """,
            // auto turn left
            Actions.Walk,
            Actions.Walk
        );
        var left = test.GetOut(3);

        Assert.That(left, Is.EqualTo(0));
    }


    [Test]
    public void GetOutWithMultipleMoves()
    {
        var test = NewExplorerFor("""
            +---+
               k|
            + -/+
            |  x|
            | --+
            """,
            // auto turn left
            Actions.Walk,
            Actions.Walk,
            // auto turn left
            Actions.TurnLeft,
            Actions.TurnLeft,
            Actions.Walk,
            Actions.Walk,
            // auto turn left
            Actions.Walk
        );
        var left = test.GetOut(15);

        Assert.That(left, Is.EqualTo(5));
    }

    [Test]
    public void GetOutPassingADoor()
    {
        var test = NewExplorerFor("""
            +-/-+
            | k |
            | x |
            +---+
            """,
            Actions.Walk,
            Actions.Walk
        );
        var left = test.GetOut(10);

        Assert.That(left, Is.EqualTo(8));
    }

    [Test]
    public void GetOutPassingTwoDoors()
    {
        var test = NewExplorerFor("""
            +--+
            |kx|
            +/-+
            | k/
            +--+
            """,
            // auto turn left
            Actions.Walk, // key
            // auto turn left
            Actions.Walk, // door
            Actions.Walk,
            // auto turn left
            Actions.Walk, // key
            Actions.Walk  // door
        );
        var left = test.GetOut(10);

        Assert.That(left, Is.EqualTo(2));
    }

    [Test]
    public void GetOutPassingTwoKeysBeforeDoors()
    {
        var test = NewExplorerFor("""
            +--+
            |kx/
            | k/
            +--+
            """,
            Actions.Walk,// key
            Actions.Walk,
            Actions.Walk,// swap keys
            Actions.Walk // door
        );
        var left = test.GetOut(10);

        Assert.That(left, Is.EqualTo(3));
    }
}
