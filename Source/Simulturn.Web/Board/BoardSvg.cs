using Microsoft.AspNetCore.Components;
using System.Net;

namespace Simulturn.Web.Board;

/// <summary>
/// SVG markup helpers. Razor reserves the lowercase &lt;text&gt; tag for literal text
/// (RZ1023 forbids attributes on it), so SVG text elements are emitted as MarkupString.
/// </summary>
public static class BoardSvg
{
    public static MarkupString Text(string cssClass, double x, double y, string content) =>
        new($"<text class=\"{cssClass}\" x=\"{HexLayout.Svg(x)}\" y=\"{HexLayout.Svg(y)}\">{WebUtility.HtmlEncode(content)}</text>");
}
