using Labyrinth.Build;
using Labyrinth.Exploration;
using NUnit.Framework;

namespace LabyrinthTest;

public class LocalMultiAgentIntegrationTests
{
    [Test]
    public async Task Two_agents_run_in_parallel()
    {
        var ascii = """
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
        var lab = new Labyrinth.Labyrinth(new AsciiParser(ascii));

        var map = await MultiAgentRunner.RunTwoAsync(lab, 200);

        Assert.That(map.KnownCount, Is.GreaterThan(5));
    }
}
