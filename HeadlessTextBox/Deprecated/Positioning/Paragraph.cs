using System.Diagnostics;
using HeadlessTextBox.Compositing.Contracts;
using HeadlessTextBox.Formatting;
using HeadlessTextBox.Positioning.Manual.Models;
using HeadlessTextBox.Positioning.Manual.WordBreaking;
using HeadlessTextBox.Positioning.WordBreaking;
using Icu;
using Range = HeadlessTextBox.Positioning.Manual.Models.Range;

namespace HeadlessTextBox.Positioning.Manual;

public class Paragraph
{
    private readonly List<Line> _lines;

    public int CharCount { get; private set; }
    
    
    public int LineCount => _lines.Count;
    public IReadOnlyList<Line> Lines => _lines;
    public float Height => Lines.Sum(line => line.Height);


    public Paragraph() => _lines = new();

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
    
    
    public ParagraphEnumerator GetEnumerator() => new(_lines);
    
    
    private void Init(
        float lineWidth,
        in SourceRef paragraph,
        Locale? locale) 
        => Update(lineWidth, paragraph, 0, locale);

    public unsafe void Update(
        float lineWidth,
        in SourceRef paragraph,
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


    [Obsolete("Not implemented: No need for line level accuracy: Not enough efficiency")]
    public (int index, Range range) Locate(float x, float y)
    {
        var charIndex = 0;
        var lineIndex = 0;
        
        var heightSum = 0f;
        foreach (var line in Lines)
        {
            lineIndex++;
            heightSum += line.Height;
            if (heightSum > y)
                break;
            charIndex += line.Length;
        }

        foreach (var slot in Lines[lineIndex].Positions)
        {
            if (x <= (slot.Range.EndPos + slot.Range.StartPos) / 2)
                break;
            charIndex++;
        }

        return (charIndex, Lines[lineIndex].Positions[charIndex].Range);
    }
    

    private void AppendWord(float lineWidth, in SourceRef source)
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

    private void AppendWhitespaces(float lineWidth, in SourceRef source)
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
    
    private void AppendLongWord(float lineWidth, in SourceRef source)
    {
        var line = new Line();
        foreach (var (c, f) in source)
        {
            var addend = CalculateCharSlot(c, f);
            
            if (Line.LineSlot(addend, line).Range.EndPos <= lineWidth)
            {
                line.Append(addend);
                continue;
            }

            if (_lines.Count > 0 && _lines[^1].Empty)
                _lines[^1] = line;
            else
                _lines.Add(line);
            line = new Line();
        }
    }

    private void AppendWithinWord(in SourceRef source)
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
    

    private static Range CalculateWordRange(in SourceRef source)
    {
        var range = new Range();
        foreach (var (c, f) in source)
            range += CalculateCharSlot(c, f).Range;
        return range;
    }
    
    private static Slot CalculateCharSlot(char character, IFormat format)
    {
        Debug.Assert(!char.IsControl(character));

        var metrics = format.GetGlyphMetrics(character);
        
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


public ref struct ParagraphEnumerator
{
    private int _lineIndex;
    private int _charIndex;
    private readonly IReadOnlyList<Line> _lines;

    private float _heightOffset;
    
    
    private Range CurrentRange => _lines[_lineIndex].Positions[_charIndex].Range;
    public (float HeightOffset, Range range) Current => (_heightOffset, CurrentRange);
    

    public ParagraphEnumerator(IReadOnlyList<Line> lines)
    {
        _lines = lines;
        
        _lineIndex = 0;
        _charIndex = -1;

        _heightOffset = _lines.Count > 0
            ? _lines[_lineIndex].Height
            : 0f;
    }

    public bool MoveNext()
    {
        if (_lines[_lineIndex].Length - 1 > _charIndex)
        {
            _charIndex++;
            return true;
        }
        
        _lineIndex++;
        if (_lines.Count <= _lineIndex)
            return false;

        _charIndex = 0;
        _heightOffset += _lines[_lineIndex].Height;
        return true;
    }
}