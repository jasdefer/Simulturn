using Simulturn.Core.Model;
using Simulturn.Core.Setup;

namespace Simulturn.Test.Common;

/// <summary>
/// The standard rules every test plays with, taken from <see cref="DefaultSettings"/>. Tests only
/// differ in their map (see <see cref="Maps"/>) and, rarely, in the start matter.
/// </summary>
public static class TestSettings
{
    public const int DefaultStartMatter = DefaultSettings.DefaultStartMatter;

    public static GameSettings Create(IReadOnlyDictionary<Hexagon, HexagonSettings> hexagonSettings, int startMatter = DefaultStartMatter)
    {
        return DefaultSettings.Create(hexagonSettings, startMatter);
    }
}
