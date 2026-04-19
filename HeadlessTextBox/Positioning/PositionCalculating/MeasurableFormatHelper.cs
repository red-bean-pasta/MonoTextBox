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
        var height = 0;
        var infos = new GlyphInfo[text.Length];
        var positions = new GlyphPosition[text.Length];

        var cluster = (uint)0;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            
            var id = format.GetGlyphId(c);
            var info = new GlyphInfo(){ Cluster = cluster, Codepoint = id };
            
            var (position, h) = CalculateGlyphExtent(c, format, scale);
            if (h > height) height = h;
                
            infos[i] = info;
            positions[i] = position;

            cluster++;
        }
        
        return new FormatPieceExtent(height, infos, positions, text);
    }
    
    public static (GlyphPosition Position, int Height) CalculateGlyphExtent(
        char character, 
        IMeasurableFormat format,
        int scale = 1)
    {
        Debug.Assert(!char.IsControl(character));
        var (leftBearing, width, rightBearing, ascender, descender, lineGap) = format.GetGlyphMetrics(character);
        var position = new GlyphPosition()
        {
            XAdvance = (int)((leftBearing + width + rightBearing) * scale),
            YAdvance = 0,
            XOffset = 0,
            YOffset = 0
        };
        var height = (int)((Math.Abs(ascender) + Math.Abs(descender) + lineGap) * scale);
        return (position, height);
    }
}