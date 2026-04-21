using System.Diagnostics;
using HeadlessTextBox.Compositing.Contracts;
using HeadlessTextBox.Formatting;
using HeadlessTextBox.Positioning.PositionCalculating;
using HeadlessTextBox.Positioning.WordBreaking;
using HeadlessTextBox.Utils;
using HeadlessTextBox.Utils.Extensions;
using Icu;

namespace HeadlessTextBox.Positioning.LineManaging;

public static class LinePositionHelper
{
    private static PieceExtentEnumerator EnumeratePieceExtents(in SourceRef sourceRef, int scale) => new(sourceRef, scale);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="width"></param>
    /// <param name="source"></param>
    /// <param name="lines"></param>
    /// <param name="locale"></param>
    /// <param name="scale">Useful for uniformly scaling width and glyph extents to avoid floating point precision</param>
    public static void ShapeSourceAndAppendToLine(
        float width,
        in SourceRef source,
        LineManager lines,
        Locale? locale,
        int scale)
    {
        using var breakPoints = GetBreakPoints(locale, source);
        
        var addedCharCount = 0;
        foreach (var extent in EnumeratePieceExtents(source, scale))
        {
            ShapeExtentToLine(
                (int)(width * scale),
                extent, 
                lines, 
                breakPoints.AsSpan(), 
                addedCharCount
            );
            addedCharCount += extent.CharLength;
        }
    }

    private static void ShapeExtentToLine(
        int width,
        FormatPieceExtent extent,
        LineManager lines,
        ReadOnlySpan<int> breakPoints,
        int startCharIndex)
    {
        var room = width - lines.LastLineWidth;
        if (room - extent.Width >= 0)
        {
            lines.AppendExtent(extent);
            return;
        }
        
        var breakpoint = FindBreakPoint(room, startCharIndex, breakPoints, extent);
        var relativeBreakPoint = breakpoint - startCharIndex;
        SplitExtent(extent, relativeBreakPoint, out var left, out var right);
        lines.AppendExtent(left);

        SplitByWhitespace(right, out left, out right);
        ClampWhitespaceExtent(room, left);
        lines.AppendExtent(left);

        lines.AddNewLine();
        ShapeExtentToLine(width, right, lines, breakPoints, startCharIndex + extent.CharLength - right.CharLength);
    }

    
    private static unsafe RentedArray<int> GetBreakPoints(
        Locale? locale,
        in SourceRef sourceRef)
    {
        var lineBreaker = LineBreakerManager.Get(locale);
        var breakPoints = new RentedArray<int>();
        fixed (char* ptr = sourceRef.GetTextSpan())
        {
            foreach (var slice in lineBreaker.Enumerate(ptr, sourceRef.Length))
                breakPoints.Add(slice.Start);
        }
        return breakPoints;
    }
    
    private static int FindBreakPoint(
        float room,
        int charStart,
        ReadOnlySpan<int> breakPoints,
        FormatPieceExtent extent)
    {
        var width = 0;
        var breakPointIndex = breakPoints.FindFirstGreater(charStart);
        var lastValidBreakPoint = charStart;
        
        while (breakPointIndex < breakPoints.Length)
        {
            var currentBreakPoint = breakPoints[breakPointIndex];
            
            var length = Math.Min(currentBreakPoint - lastValidBreakPoint, extent.CharLength);
            width += extent.CalculateSliceWidth(lastValidBreakPoint, length);

            if (width > room)
                return lastValidBreakPoint;
            
            if (currentBreakPoint >= charStart + extent.CharLength)
                return charStart + extent.CharLength;
            
            lastValidBreakPoint = currentBreakPoint;
            breakPointIndex++;
        }
        
        return lastValidBreakPoint;
    }
    
    
    private static void SplitExtent(
        FormatPieceExtent extent, 
        int breakpoint,
        out FormatPieceExtent left, 
        out FormatPieceExtent right)
    {
        left = extent.Slice(0, breakpoint);
        right = extent.Slice(breakpoint);
    }
    
    private static void SplitByWhitespace(
        FormatPieceExtent extent, 
        out FormatPieceExtent left, 
        out FormatPieceExtent right)
    {
        // var breakpoint = extent.SourceChars.IndexOfAnyExceptWhiteSpaces();
        var breakpoint = 0;
        for (var i = 0; i < extent.CharLength; i++)
        {
            if (char.IsWhiteSpace(extent.Metadata.SourceChars[i]))
                breakpoint = i + 1;
            else
                break;
        }

        SplitExtent(extent, breakpoint, out left, out right);
    }
    
    private static void ClampWhitespaceExtent(
        int room,
        FormatPieceExtent extent)
    {
        Debug.Assert(extent.Metadata.SourceChars.IsWhiteSpace());
        
        for (var i = 0; i < extent.GlyphLength; i++)
        {
            ref var pos = ref extent.GetGlyphPosition(i);
            var advance = Math.Min(pos.XAdvance, room);
            pos.XAdvance = advance;
            room -= advance;
        }
    }
    
    
    private ref struct PieceExtentEnumerator
    {
        private int _offset = 0;
        private readonly ReadOnlySpan<char> _text;
        private FormatTree.NodeEnumerator _pieceEnumerator;

        private readonly int _scale;
        
        public FormatPieceExtent Current => GetCurrentValue();
        
        public PieceExtentEnumerator(in SourceRef source, int scale)
        {
            _text = source.GetTextSpan();
            _pieceEnumerator = source.EnumerateFormatPieces();
            _scale = scale;
        }
        
        public PieceExtentEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            return _pieceEnumerator.MoveNext();
        }

        private FormatPieceExtent GetCurrentValue()
        {
            var (format, length) = _pieceEnumerator.Current;

            var text = _text.Slice(_offset, length);
            var extent = CalculatePieceExtent(text, format, _scale);
            
            _offset += text.Length;
            
            return extent;
        }
        
        public void Dispose() => _pieceEnumerator.Dispose();
        
        private static FormatPieceExtent CalculatePieceExtent(
            ReadOnlySpan<char> text,
            IFormat format,
            int scale)
        {
            if (format is IFontFormat fontFormat)
                return FontFormatHelper.Calculate(text, fontFormat, scale);
        
            if (format is IMeasurableFormat measurableFormat)
                return MeasurableFormatHelper.Calculate(text, measurableFormat, scale);
        
            throw new ArgumentException($"Unknown format type: {format.GetType().Name}");
        }
    }
}