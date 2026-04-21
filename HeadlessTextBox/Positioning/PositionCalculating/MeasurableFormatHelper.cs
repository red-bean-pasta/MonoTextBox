using System.Diagnostics;
using HarfBuzzSharp;
using HeadlessTextBox.Formatting;

namespace HeadlessTextBox.Positioning.PositionCalculating;

public static class MeasurableFormatHelper
{
    public static FormatPieceExtent Calculate(
        ReadOnlySpan<char> text, 
        IMeasurableFormat format,
        int scale)
    {
        var infos = new GlyphInfo[text.Length];
        var positions = new GlyphPosition[text.Length];

        var cluster = (uint)0;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            
            var id = format.GetGlyphId(c);
            var info = new GlyphInfo(){ Cluster = cluster, Codepoint = id };

            var position = CalculateGlyphPosition(c, format, scale);
                
            infos[i] = info;
            positions[i] = position;

            cluster++;
        }
        
        var fontExtents = CalculateFontExtents(format, scale);
        
        return FormatPieceExtent.Build(fontExtents, infos, positions, format, scale, text);
    }

    private static GlyphPosition CalculateGlyphPosition(
        char character, 
        IMeasurableFormat format,
        int scale = 1)
    {
        Debug.Assert(!char.IsControl(character));
        var (leftBearing, width, rightBearing) = format.GetGlyphMetrics(character);
        var position = new GlyphPosition()
        {
            XAdvance = (int)((leftBearing + width + rightBearing) * scale),
            YAdvance = 0,
            XOffset = 0,
            YOffset = 0
        };
        return position;
    }

    private static FontExtents CalculateFontExtents(
        IMeasurableFormat format,
        int scale = 1)
    {
        var (ascender, descender, lineGap) = format.GetFontExtents();
        var extent = new FontExtents()
        {
            Ascender = (int)(ascender * scale),
            Descender = (int)(descender * scale),
            LineGap = (int)(lineGap * scale)
        };
        return extent;
    }
}