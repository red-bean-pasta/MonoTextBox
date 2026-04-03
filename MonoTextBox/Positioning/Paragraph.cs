using System.Diagnostics;
using MonoTextBox.Font;
using MonoTextBox.Positioning.Helpers;

namespace MonoTextBox.Positioning;

public class Paragraph
{
    private readonly int _width;
    private readonly List<Line> _lines = new();

    
    public int LineCount => _lines.Count;

    
    public Paragraph(
        ReadOnlyMemory<char> content,
        IFont fontManager,
        ILineBreaker wordWrapper)
    {
        wordWrapper.Break(
            content.Span, 
            OnWord
        );
        
        return;
        void OnWord(int start, int end) 
            => OnWordBreak(content.Span, start, end, fontManager);
    }
    

    private void OnWordBreak(
        ReadOnlySpan<char> source,
        int startIndex,
        int endIndex,
        IFont fontManager)
    {
        var word = source.Slice(startIndex, endIndex - startIndex);
        AppendWord(word, fontManager, _width);
    }

    private void AppendWord(
        ReadOnlySpan<char> word,
        IFont fontManager,
        int lineWidth)
    {
        var range = CalculateWordRange(word, fontManager);
        var line = _lines.Last();

        if (range.Width > lineWidth) // Word super long 
        {
            AppendLongWord(word, fontManager, lineWidth);
            return;
        }

        if (Line.LineRange(range, line).End > lineWidth)
        {
            _lines.Add(line);
            line = new Line();
        }
        
        foreach (var c in word) 
            line.Append(CalculateCharRange(c, fontManager)); 
    }

    private void AppendLongWord(
        ReadOnlySpan<char> word,
        IFont fontManager,
        float lineWidth)
    {
        var i = 0;
        var line = new Line();
        while (i < word.Length)
        {
            var addend = CalculateCharRange(word[i], fontManager);
            
            if (Line.LineRange(addend, line).End <= lineWidth)
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


    private static Range CalculateWordRange(
        ReadOnlySpan<char> word,
        IFont fontManager)
    {
        var range = new Range();
        foreach (var c in word) 
            range += CalculateCharRange(c, fontManager);
        return range;
    }
    
    private static Range CalculateCharRange(
        char c,
        IFont fontManager)
    {
        Debug.Assert(!char.IsControl(c));
        
        var glyph = fontManager.GetGlyph(c);
        var start = glyph.LeftSideBearing;
        var end = glyph.LeftSideBearing + glyph.Width + glyph.RightSideBearing;
        return new Range(start, end);
    }
}