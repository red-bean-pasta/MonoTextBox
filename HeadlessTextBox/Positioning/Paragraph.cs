using System.Diagnostics;
using HeadlessTextBox.Compositing.Contracts;
using HeadlessTextBox.Formatting;
using HeadlessTextBox.Formatting.Font;
using HeadlessTextBox.Positioning.Models;
using HeadlessTextBox.Positioning.WordBreaking;
using Icu;
using Range = HeadlessTextBox.Positioning.Models.Range;

namespace HeadlessTextBox.Positioning;

public class Paragraph
{
    private readonly List<Line> _lines = new();

    public int CharCount { get; private set; }
    
    
    public int LineCount => _lines.Count;
    public IReadOnlyList<Line> Lines => _lines;


    private Paragraph()
    { }

    public Paragraph(List<Line> lines) => _lines = lines;

    public static Paragraph Empty() => new();
    
    public static Paragraph Build(
        float width,
        in SourceSlice paragraph,
        Locale? locale)
    {
        var p = new Paragraph();
        p.Init(width, paragraph, locale);
        return p;
    }
    
    
    private void Init(
        float lineWidth,
        in SourceSlice paragraph,
        Locale? locale) 
        => Update(lineWidth, paragraph, 0, locale);

    public unsafe void Update(
        float lineWidth,
        in SourceSlice paragraph,
        int changeIndex,
        Locale? locale)
    {
        Debug.Assert(!paragraph.GetTextSpan().IsWhiteSpace());
        
        CharCount = paragraph.Length;
        
        var updateIndex = FindRewrapIndex(changeIndex);
        InvalidateAndCleanUp(updateIndex);
        
        var updateBuffer = paragraph[updateIndex..];
        if (updateBuffer.Length == 0) // Possible when the update is solely about removing and no word rewrapping
            return;
        var wordWrapper = LineBreakerManager.Get(locale);
        fixed (char* ptr = updateBuffer.GetTextSpan())
        {
            foreach (var offset in wordWrapper.Enumerate(ptr, updateBuffer.Length)) 
                AppendWord(lineWidth, updateBuffer.Slice(offset));
        }
    }
    

    private void AppendWord(float lineWidth, in SourceSlice source)
    {
        if (source.GetTextSpan().IsWhiteSpace())
        {
            AppendWhitespaces(lineWidth, source);
            return;
        }
        
        var range = CalculateWordRange(source);
        if (range.Width > lineWidth) // Word super long 
        {
            AppendLongWord(lineWidth, source);
            return;
        }

        if (Line.LineRange(range, _lines.Last()).EndPos > lineWidth)
        {
            _lines.Add(new Line());
        } 
        AppendWithinWord(source);
    }

    private void AppendWhitespaces(float lineWidth, in SourceSlice source)
    {
        var line = _lines.Last();
        foreach (var (c, f) in source)
        {
            var slot = CalculateCharSlot(c, f);
            
            var room = lineWidth - line.RightEdge;
            var width = Math.Min(slot.Range.Width, room);
            var clamped = new Range(slot.Range.StartPos, slot.Range.StartPos + width);
            
            line.Append(slot with {Range = clamped});
        }
    }
    
    private void AppendLongWord(float lineWidth, in SourceSlice source)
    {
        var i = 0;
        var line = new Line();
        while (i < source.Length)
        {
            var (c, f) = source[i];
            var addend = CalculateCharSlot(c, f);
            
            if (Line.LineSlot(addend, line).Range.EndPos <= lineWidth)
            {
                 line.Append(addend);
                 i++;
                 continue;
            }

            if (_lines.Count > 0 && _lines[^1].Empty)
                _lines[^1] = line;
            else
                _lines.Add(line);
            line = new Line();
        }
    }

    private void AppendWithinWord(in SourceSlice source)
    {
        var line = _lines.Last();
        foreach (var (c, f) in source) 
            line.Append(CalculateCharSlot(c, f));
    }


    private void InvalidateAndCleanUp(int count)
    {
        var sum = 0;
        for (var i = 0; i < LineCount; i++)
        {
            var line = _lines[i];
            
            Debug.Assert(line.Length > 0);
            sum += line.Length;
            if (sum <= count)
                continue;
            
            _lines.RemoveRange(i, LineCount - i);
            var kept = line.Length - (sum - count);
            if (kept > 0)
            {
                var clipped = new Line(line.Positions.Take(kept));
                _lines.Add(clipped);
            }
            break;
        }
    }
    

    private static Range CalculateWordRange(in SourceSlice source)
    {
        var range = new Range();
        foreach (var (c, f) in source)
            range += CalculateCharSlot(c, f).Range;
        return range;
    }
    
    private static Slot CalculateCharSlot(char c, IFormat format)
    {
        Debug.Assert(!char.IsControl(c));

        var font = FontManager.GetFont(format.Font);
        var metrics = font.GetGlyphMetrics(c);
        
        var start = metrics.LeftSideBearing;
        var end = metrics.LeftSideBearing + metrics.Width + metrics.RightSideBearing;
        var range = new Range(start, end);
        
        var height = metrics.Ascender + Math.Abs(metrics.Descender) + metrics.LineGap;
        
        return new Slot(range, height);
    }


    // Brutally default to the start of previous line.
    // Optimizing Line to binary tree may yield no benefit but more overhead.
    private int FindRewrapIndex(int index)
    {
        if (_lines.Count <= 2)
            return 0;

        var sum = 0;
        for (var i = 0; i < _lines.Count; i++)
        {
            sum += _lines[i].Length;
            if (sum <= index)
                continue;
            return sum - _lines[i].Length - _lines[i-1].Length;
        }
        throw new IndexOutOfRangeException();
    }
}