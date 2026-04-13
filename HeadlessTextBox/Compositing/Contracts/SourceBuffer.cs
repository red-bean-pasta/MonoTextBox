using System.Diagnostics;
using HeadlessTextBox.Formatting;
using HeadlessTextBox.Storage;
using HeadlessTextBox.TextStoring;
using HeadlessTextBox.Utils;

namespace HeadlessTextBox.Compositing.Contracts;

public class SourceBuffer
{
    public TextStorage Text { get; }
    public FormatTree Format { get; }

    
    public int Length => Text.TextTree.Length;
    
    
    public SourceBuffer()
    : this(string.Empty, new FormatTree())
    { }
    
    public SourceBuffer(string text, FormatTree format)
    {
        var originalBuffer = text;
        var addedBuffer = new AddBuffer();
        var piece = new TextPiece(0, originalBuffer.Length, TextPiece.SourceType.Original);
        var pieceTree = new TextTree(piece, null, null);
        Text = new TextStorage(originalBuffer, addedBuffer, pieceTree);
        
        Format = format;
    }
    
    
    public TextBufferEnumerator SlicedEnumerate(int start, int length) => new(this, start, length);


    public void Remove()
    {
        throw new NotImplementedException();
    }
    

    public FlatSourceSlice this[Range range] => Slice(range);

    private FlatSourceSlice Slice(Range range)
    {
        var (start, end) = range.GetOffsetAndLength(Length);
        return Slice(start, end);
    }
    
    public FlatSourceSlice Slice(Slice slice) => Slice(slice.Start, slice.Length);

    public FlatSourceSlice Slice(int start, int length)
    {
        return new FlatSourceSlice(start, length, this);
    }
}


public ref struct TextBufferEnumerator
{
    private int _remaining;
    
    private int _remainInTextSpan;
    private TextStorageEnumerator _textEnumerator;
    private int _remainInFormatPiece;
    private FormatTree.NodeEnumerator _formatEnumerator;
    
    
    public TextElement Current => GetCurrent();
    
    
    public TextBufferEnumerator(
        SourceBuffer buffer, 
        int start = 0, 
        int length = -1)
    {
        _remaining = length < 0 ? buffer.Length - start : length;
        
        _textEnumerator = buffer.Text.SlicedEnumerate(start, _remaining);
        _formatEnumerator = buffer.Format.EnumerateSliced(start, _remaining);

        _remainInTextSpan = 0;
        _remainInFormatPiece = 0;
    }


    public bool MoveNext()
    {
        if (_remainInTextSpan <= 0)
        {
            if (!_textEnumerator.MoveNext())
            {
                Debug.Assert(_remainInFormatPiece <= 0 && !_formatEnumerator.MoveNext());
                return false;
            }
            
            _remainInTextSpan = _textEnumerator.Current.Length;
        }

        if (_remainInFormatPiece <= 0)
        {
            _formatEnumerator.MoveNext();
            _remainInFormatPiece = _formatEnumerator.Current.Length;
        }
        
        Debug.Assert(_remainInTextSpan > 0 && _remainInFormatPiece > 0);
        _remaining--;
        return true;
    }


    private TextElement GetCurrent()
    {
        var textIndex = _textEnumerator.Current.Length - _remainInTextSpan;
        var c = _textEnumerator.Current[textIndex];
        
        var f = _formatEnumerator.Current.Format;
        
        return new TextElement(c, f);
    }
    

    public void Dispose()
    {
        _textEnumerator.Dispose();
        _formatEnumerator.Dispose();
    }
}