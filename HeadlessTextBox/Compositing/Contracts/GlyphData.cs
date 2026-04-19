using HarfBuzzSharp;

namespace HeadlessTextBox.Compositing.Contracts;

/// <summary>
/// 
/// </summary>
/// <param name="Id">Same as <see cref="GlyphInfo.Codepoint"/></param>
/// <param name="X"></param>
/// <param name="Y"></param>
/// <param name="XOffset"></param>
/// <param name="YOffset"></param>
public readonly record struct GlyphData(
    uint Id,
    float X,
    float Y,
    float XOffset,
    float YOffset
);