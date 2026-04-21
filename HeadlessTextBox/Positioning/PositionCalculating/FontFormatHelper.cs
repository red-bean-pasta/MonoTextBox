using System.Diagnostics;
using HarfBuzzSharp;
using HeadlessTextBox.Formatting;
using Buffer = HarfBuzzSharp.Buffer;

namespace HeadlessTextBox.Positioning.PositionCalculating;

public static class FontFormatHelper
{
    private static readonly ThreadLocal<Buffer> BufferPool = new(
        () => new Buffer(),
        trackAllValues: true
    );

    /// <param name="characters"></param>
    /// <param name="format"></param>
    /// <param name="scale"></param>
    /// <param name="reset">Useful when changing to a completely different language or script</param>
    /// <returns></returns>
    public static FormatPieceExtent Calculate(
        ReadOnlySpan<char> characters, 
        IFontFormat format,
        int scale,
        bool reset = false)
    {
        var buffer = BufferPool.Value;
        Debug.Assert(buffer is not null);
        buffer.ClearContents();
        if (reset) buffer.Reset();
        
        format.Font.SetScale(format.FontSize * scale, format.FontSize * scale);
        
        buffer.AddUtf16(characters);
        buffer.GuessSegmentProperties();
        format.Font.Shape(buffer);
        var infos = buffer.GlyphInfos;
        var positions = buffer.GlyphPositions;

        var extents = format.Font.GetFontExtentsForDirection(Direction.LeftToRight);
        return FormatPieceExtent.Build(extents, infos, positions, format, scale, characters);
    }
}