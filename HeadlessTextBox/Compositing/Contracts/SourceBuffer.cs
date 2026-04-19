using System.Diagnostics;
using HeadlessTextBox.Compositing.Storage;
using HeadlessTextBox.Formatting;
using HeadlessTextBox.Utils;

namespace HeadlessTextBox.Compositing.Contracts;

public class SourceBuffer
{
    private readonly TextStorage _text;
    private readonly FormatStorage _format;

    
    public int Length => _text.Length;
    
    
    public SourceBuffer()
    : this(string.Empty, new FormatTree())
    { }
    
    public SourceBuffer(
        string text, 
        FormatTree format)
    {
        _text = new TextStorage(text);
        _format = new FormatStorage(format);
    }
    
    
    // Enumerated lazy query
    public TextBufferEnumerator SlicedEnumerate(int start, int length) 
        => new(_text, _format, start, length);
    
    
    public (string Text, string Format) Serialize() => (_text.Serialize(), _format.Serialize());


    // Buffer manipulation
    public void Insert(
        int index, 
        ReadOnlySpan<char> text)
    {
        _text.Insert(index, text);
        _format.Extend(index, text.Length);
    }
    
    public void Insert(
        int index, 
        ReadOnlySpan<char> text,
        IFormat format)
    {
        _text.Insert(index, text);
        _format.Insert(index, text.Length, format);
    }
    
    public void Insert(
        int index, 
        ReadOnlySpan<char> text,
        IEnumerable<FormatPiece> format)
    {
        _text.Insert(index, text);

        var pos = index;
        foreach (var piece in format)
        {
            _format.Insert(pos, piece.Length, piece.Format);
            pos += piece.Length;
        }
    }

    public void Remove(int index, int length)
    {
        _text.Remove(index, length);
        _format.Remove(index, length);
    }

    public void ChangeFormat(int index, int length, IFormat format)
    {
        _format.Change(index, length, format);
    }
    

    // Flattened query
    public SourceRef this[Range range] => Slice(range);

    private SourceRef Slice(Range range)
    {
        var (start, end) = range.GetOffsetAndLength(Length);
        return Slice(start, end);
    }
    
    public SourceRef Slice(Slice slice) => Slice(slice.Start, slice.Length);

    public SourceRef Slice(int start, int length)
    {
        return new SourceRef(start, length, _text, _format);
    }
}


public ref struct TextBufferEnumerator
{
    private int _remainInTextSpan;
    private TextStorage.TextPieceEnumerator _textEnumerator;
    private FormatStorage.FormatEnumerator _formatEnumerator;
    
    
    public TextElement Current => GetCurrent();
    
    
    public TextBufferEnumerator(
        TextStorage text,
        FormatStorage format,
        int start = 0, 
        int length = -1)
    {
        Debug.Assert(text.Length == format.Length && text.Length >= length);
        
        var normalizedLength = length < 0 ? text.Length - start : length;
        _textEnumerator = text.SlicedEnumerate(start, normalizedLength);
        _formatEnumerator = format.SliceEnumerate(start, normalizedLength);

        _remainInTextSpan = 0;
    }


    public bool MoveNext()
    {
        if (_remainInTextSpan <= 0)
        {
            if (!_textEnumerator.MoveNext())
            {
                Debug.Assert(!_formatEnumerator.MoveNext());
                return false;
            }
            
            _remainInTextSpan = _textEnumerator.Current.Length;
        }

        _formatEnumerator.MoveNext();
        
        Debug.Assert(_remainInTextSpan > 0);
        return true;
    }


    private TextElement GetCurrent()
    {
        var textIndex = _textEnumerator.Current.Length - _remainInTextSpan;
        var c = _textEnumerator.Current[textIndex];
        
        var f = _formatEnumerator.Current;
        
        return new TextElement(c, f);
    }
    

    public void Dispose()
    {
        _textEnumerator.Dispose();
        _formatEnumerator.Dispose();
    }
}