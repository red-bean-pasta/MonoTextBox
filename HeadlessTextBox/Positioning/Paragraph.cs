using System.Diagnostics;
using HarfBuzzSharp;
using HeadlessTextBox.Compositing.Contracts;
using HeadlessTextBox.Formatting;
using HeadlessTextBox.Positioning.PositionCalculating;
using HeadlessTextBox.Positioning.WordBreaking;
using HeadlessTextBox.Utils;
using HeadlessTextBox.Utils.Extensions;
using Icu;

namespace HeadlessTextBox.Positioning;

public class Paragraph
{
    private const int OptimizeScale = 64;
    
    public int CharCount { get; private set; }
    private readonly List<Line> _lines;

    
    public int LineCount => _lines.Count;
    public IReadOnlyList<Line> Lines => _lines;
    public float Height => Lines.Sum(line => line.Height);


    protected Paragraph() => _lines = new List<Line>();
    
    protected Paragraph(List<Line> lines) => _lines = lines;
    
    public static Paragraph Empty() => new();
    
    public static Paragraph Build(
        float width,
        in SourceRef paragraph,
        Locale? locale)
    {
        var p = Empty();
        p.Init(width, paragraph, locale);
        return p;
    }
    
    
    private void Init(
        float width,
        in SourceRef paragraph,
        Locale? locale) 
        => Update(width, paragraph, 0, locale);

    public void Update(
        float lineWidth,
        in SourceRef paragraph,
        int changeIndex,
        Locale? locale)
    {
        Debug.Assert(!paragraph.GetTextSpan().IsWhiteSpace());
        
        CharCount = paragraph.Length;
        
        var (updateIndex, lineIndex) = FindRewrapIndex(changeIndex);
        PruneLines(lineIndex);
        
        var updateRef = paragraph[updateIndex..];
        
        if (updateRef.Length == 0) // Possible when the update is solely about removing and no word rewrapping
            return;

        ShapeSourceToLine(paragraph, lineWidth, locale);
    }
    
    
    public GlyphDataEnumerator GetEnumerator() => new(_lines);

    
    private static PieceExtentEnumerator EnumeratePieceExtents(in SourceRef sourceRef, int scale) => new(sourceRef, scale);
    private void ShapeSourceToLine(
        in SourceRef source,
        float lineWidth,
        Locale? locale)
    {
        using var breakPoints = GetBreakPoints(locale, source);
        
        var addedCharCount = 0;
        foreach (var extent in EnumeratePieceExtents(source, OptimizeScale))
        {
            AddExtentToLine((int)(lineWidth * OptimizeScale), extent, breakPoints.AsSpan(), addedCharCount);
            addedCharCount += extent.CharLength;
        }
    }

    private int LastLineWidth => Lines[^1].Width;
    private void AddExtentToLine(
        int lineWidth,
        FormatPieceExtent extent,
        ReadOnlySpan<int> breakPoints,
        int startCharIndex)
    {
        var room = lineWidth - LastLineWidth;
        if (room - extent.Width >= 0)
        {
            AddExtentToLine(extent);
            return;
        }
        
        var breakpoint = FindBreakPoint(room, startCharIndex, breakPoints, extent);
        var relativeBreakPoint = breakpoint - startCharIndex;
        SplitExtent(extent, relativeBreakPoint, out var left, out var right);
        AddExtentToLine(left);

        SplitByWhitespace(right, out left, out right);
        ClampWhitespaceExtent(room, left);
        AddExtentToLine(left);

        _lines.Add(new Line());
        AddExtentToLine(lineWidth, right, breakPoints, startCharIndex + extent.CharLength - right.CharLength);
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

    
    private void AddExtentToLine(FormatPieceExtent extent)
    {
        if (extent.GlyphLength <= 0)
            return;
        
        var last = Lines[^1];
        last.Append(extent);
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
    
    private static void SplitByWhitespace(
        FormatPieceExtent extent, 
        out FormatPieceExtent left, 
        out FormatPieceExtent right)
    {
        // var breakpoint = extent.SourceChars.IndexOfAnyExceptWhiteSpaces();
        var breakpoint = 0;
        for (var i = 0; i < extent.CharLength; i++)
        {
            if (char.IsWhiteSpace(extent.SourceChars[i]))
                breakpoint = i + 1;
            else
                break;
        }
        
        left = extent.Slice(0, breakpoint);
        right = extent.Slice(breakpoint);
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

    private static void ClampWhitespaceExtent(
        int room,
        FormatPieceExtent extent)
    {
        Debug.Assert(extent.SourceChars.IsWhiteSpace());
        
        for (var i = 0; i < extent.GlyphLength; i++)
        {
            var pos = extent.GetGlyphPosition(i);
            var advance = Math.Min(pos.XAdvance, room);
            room -= advance;
        }
    }
    
    
    /// <summary>
    /// Brutally default to the start of previous line as optimizing may yield no benefit but more overhead
    /// </summary>
    /// <param name="charIndex"></param>
    /// <returns></returns>
    /// <exception cref="IndexOutOfRangeException"></exception>
    private (int CharIndex, int LineIndex) FindRewrapIndex(int charIndex)
    {
        if (_lines.Count <= 2)
            return (0, 0);

        var sum = 0;
        for (var i = 0; i < _lines.Count; i++)
        {
            sum += _lines[i].CharLength;
            
            if (sum <= charIndex)
                continue;

            return (sum - _lines[i].CharLength - _lines[i - 1].CharLength, i);
        }
        
        throw new IndexOutOfRangeException();
    }

    private void PruneLines(int lineIndex, int length = -1)
    {
        if (length < 0) length = LineCount - lineIndex;
        _lines.RemoveRange(lineIndex, length);
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
    
    
    public ref struct GlyphDataEnumerator
    {
        private int _x = 0;
        private int _y;
        
        private int _lineIndex = 0;
        private int _charIndex = -1;
        private readonly IReadOnlyList<Line> _lines;

        private Line CurrentLine => _lines[_lineIndex];
        private GlyphInfo CurrentInfo => CurrentLine.Infos[_charIndex];
        private GlyphPosition CurrentPosition => CurrentLine.Positions[_charIndex];
        
        public GlyphData Current => GetCurrentValue();
        
        public GlyphDataEnumerator(IReadOnlyList<Line> lines)
        {
            _lines = lines;
            _y = _lines.Count > 0 ? _lines[_lineIndex].Height : 0;
        }

        public bool MoveNext()
        {
            _x += CurrentPosition.XAdvance;
            
            if (_lineIndex >= _lines.Count)
                return false;
            
            _charIndex++;
            if (_charIndex < CurrentLine.CharLength)
            {
                return true;
            }
        
            _charIndex = 0;
            _lineIndex++;
            if (_lineIndex >= _lines.Count)
                return false;

            _x = 0;
            _y += CurrentLine.Height;
            return true;
        }

        private GlyphData GetCurrentValue()
        {
            const float scale = OptimizeScale;
            return new GlyphData(
                CurrentInfo.Codepoint,
                _x / scale,
                _y / scale,
                CurrentPosition.XOffset / scale,
                CurrentPosition.YOffset / scale
            );
        }
    }
}


