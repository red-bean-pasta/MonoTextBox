using HeadlessTextBox.Formatting;
using HeadlessTextBox.Positioning.PositionCalculating;

namespace HeadlessTextBox.Positioning.LineManaging;

public class LineManager
{
    private int _height = -1;
    
    private readonly List<Line> _lines = new(16);

    private Line LastLine => _lines[^1];
    public int LastLineWidth => LastLine.Width;
    public int TotalHeight => CalculateHeight();
    public IReadOnlyList<Line> Lines => _lines;

    
    public void AddNewLine()
    {
        _lines.Add(new Line());
    }
    
    public void AppendExtent(FormatPieceExtent extent)
    {
        if (extent.GlyphLength <= 0)
            return;

        var xOffset = 0;
        if (LastLine.IsEmpty)
        {
            var leftBearing = GetFirstCharLeftBearing(extent);
            if (leftBearing < 0) xOffset = -leftBearing;
        }
            
        LastLine.Append(extent, xOffset);
    }

    
    public (int LineIndex, int InLineIndex) FindIndex(int charIndex)
    {
        if (_lines.Count <= 2)
            return (0, 0);

        var sum = 0;
        for (var i = 0; i < _lines.Count; i++)
        {
            sum += _lines[i].CharLength;
            
            if (sum < charIndex)
                continue;

            return (i, charIndex - sum + _lines[i].CharLength);
        }
        
        throw new IndexOutOfRangeException();
    }
    
    
    public void InvalidateAndRemoveLines(int lineIndex, int length = -1)
    {
        if (length < 0) 
            length = _lines.Count - lineIndex;
        
        _lines.RemoveRange(lineIndex, length);
    }


    private static int GetFirstCharLeftBearing(FormatPieceExtent extent)
    {
        var metadata = extent.Metadata;
        
        if (metadata.Format is IMeasurableFormat measurableFormat)
        {
            var character = metadata.SourceChars[0];
            var metrics = measurableFormat.GetGlyphMetrics(character);
            return (int)(metrics.LeftBearing * metadata.Scale);
        }

        if (metadata.Format is IFontFormat fontFormat)
        {
            var id = extent.GlyphInfos[0].Codepoint;
            if (!fontFormat.Font.TryGetGlyphExtents(id, out var extents))
                return 0;
            return extents.XBearing * metadata.Scale;
        }
        
        throw new ArgumentException($"Unknown format type: {metadata.Format.GetType().Name}");
    }
    
    
    private int CalculateHeight()
    {
        if (_height < 0f)
            _height = _lines.Sum(l => l.Height);
        return _height;
    }
}


public struct Line
{
    public int Width { get; private set; } = 0;
    
    public int CharLength { get; private set; } = 0;
    
    private readonly List<LineGlyph> _glyphs = new(128);

    public int AboveBaseline { get; private set; } = 0;
    public int BelowBaseline { get; private set; } = 0;

    
    public int Height => AboveBaseline + BelowBaseline;
    public bool IsEmpty => CharLength == 0;
    public int GlyphLength => _glyphs.Count;
    public IReadOnlyList<LineGlyph> Glyphs => _glyphs;
    
    
    public Line()
    { }
    
    
    public void Append(
        FormatPieceExtent extent, 
        int xOffset = 0)
    {
        CharLength += extent.CharLength;

        var above = extent.FontExtents.LineGap / 2 + Math.Abs(extent.FontExtents.Ascender);
        var below = extent.FontExtents.LineGap / 2 + Math.Abs(extent.FontExtents.Descender);
        if (above > AboveBaseline)
            AboveBaseline = above;
        if (below > BelowBaseline)
            BelowBaseline = below;
        
        Width += xOffset;
        for (var i = 0; i < extent.GlyphLength; i++)
        {
            var info = extent.GlyphInfos[i];
            var position = extent.GlyphPositions[i];
            var glyph = new LineGlyph(
                info.Codepoint,
                info.Cluster,
                Width,
                position.XOffset,
                position.YOffset
            );
            _glyphs.Add(glyph);
            
            Width += position.XAdvance;
        }
    }
}