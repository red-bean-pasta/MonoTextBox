using System.Diagnostics;
using Icu;
using MonoTextBox.Formatting;
using MonoTextBox.Formatting.Font;
using MonoTextBox.Positioning.SpanEnumerating;
using MonoTextBox.Positioning.WordBreaking;

namespace MonoTextBox.Positioning;

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
        Buffer buffer,
        Locale? locale)
    {
        var p = new Paragraph();
        p.Init(width, buffer, locale);
        return p;
    }
    
    
    private void Init(
        float lineWidth,
        Buffer buffer,
        Locale? locale) 
        => Update(lineWidth, buffer, 0, locale);

    public unsafe void Update(
        float lineWidth,
        Buffer paragraph,
        int changeIndex,
        Locale? locale)
    {
        Debug.Assert(paragraph.Text.IndexNewLine() < 0);
        
        CharCount = paragraph.Length;
        
        var updateIndex = FindRewrapIndex(changeIndex);
        InvalidateAndCleanUp(updateIndex);
        
        var updateBuffer = paragraph[updateIndex..];
        if (updateBuffer.Length == 0) // Possible when the update is solely about removing and no word rewrapping
            return;
        var wordWrapper = LineBreakerManager.Get(locale);
        fixed (char* ptr = updateBuffer.Text)
        {
            foreach (var offset in wordWrapper.Enumerate(ptr, updateBuffer.Length)) 
                AppendWord(lineWidth, updateBuffer.Slice(offset));
        }
    }
    

    private void AppendWord(float lineWidth, Buffer buffer)
    {
        if (buffer.Text.IsWhiteSpace())
        {
            AppendWhitespaces(lineWidth, buffer);
            return;
        }
        
        var range = CalculateWordRange(buffer);
        if (range.Width > lineWidth) // Word super long 
        {
            AppendLongWord(lineWidth, buffer);
            return;
        }

        if (Line.LineRange(range, _lines.Last()).EndPos > lineWidth)
        {
            _lines.Add(new Line());
        } 
        AppendWithinWord(buffer);
    }

    private void AppendWhitespaces(float lineWidth, Buffer buffer)
    {
        var line = _lines.Last();
        foreach (var (c, f) in buffer)
        {
            var range = CalculateCharRange(c, f);
            var room = lineWidth - line.RightEdge;
            var width = Math.Min(range.Width, room);
            var clamped = new Range(range.StartPos, range.StartPos + width);
            line.Append(clamped);
        }
    }
    
    private void AppendLongWord(float lineWidth, Buffer buffer)
    {
        var i = 0;
        var line = new Line();
        while (i < buffer.Length)
        {
            var (c, f) = buffer[i];
            var addend = CalculateCharRange(c, f);
            
            if (Line.LineRange(addend, line).EndPos <= lineWidth)
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

    private void AppendWithinWord(Buffer buffer)
    {
        var line = _lines.Last();
        foreach (var (c, f) in buffer) 
            line.Append(CalculateCharRange(c, f));
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
    

    private static Range CalculateWordRange(Buffer buffer)
    {
        var range = new Range();
        foreach (var (c, f) in buffer)
            range += CalculateCharRange(c, f);
        return range;
    }
    
    private static Range CalculateCharRange(char c, Format format)
    {
        Debug.Assert(!char.IsControl(c));

        var font = FontManager.GetFont(format.Font);
        var glyph = font.GetGlyph(c);
        var start = glyph.LeftSideBearing;
        var end = glyph.LeftSideBearing + glyph.Width + glyph.RightSideBearing;
        return new Range(start, end);
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