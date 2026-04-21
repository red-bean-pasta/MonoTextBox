using HarfBuzzSharp;

namespace HeadlessTextBox.Positioning.LineManaging;

/// <summary>
/// 
/// </summary>
/// <param name="Id">Same as <see cref="GlyphInfo.Codepoint"/></param>
/// <param name="Cluster"></param>
/// <param name="X"></param>
/// <param name="XOffset"></param>
/// <param name="YOffset"></param>
public readonly record struct LineGlyph(
    uint Id,
    uint Cluster,
    int X,
    int XOffset,
    int YOffset
);