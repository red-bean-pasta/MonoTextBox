using HeadlessTextBox.Compositing.Serialization;
using HeadlessTextBox.Storage;
using HeadlessTextBox.TextStoring;

namespace HeadlessTextBox.Compositing.Storage;

public class TextStorage
{
    private readonly string _original;
    private readonly TextAddBuffer _added;
    private TextTree _textTree;
    
    
    public int Length => _textTree.Length;


    public TextStorage(string original)
    {
        _original = original;
        _added = new TextAddBuffer();
        
        var piece = new TextPiece(0, _original.Length, TextPiece.SourceType.Original);
        var tree = new TextTree(piece, null, null);
        _textTree = tree;
    }
    
    
    public TextPieceEnumerator GetEnumerator() => new(this);

    public TextPieceEnumerator SlicedEnumerate(int start, int length) => new(this, start, length);


    public string Serialize() => TextSerializer.Serialize(this);
    
    
    public void Remove(int position, int length)
    {
        _textTree = _textTree.Remove(position, length);
    }
    
    public void Insert(int position, ReadOnlySpan<char> text)
    {
        var (start, length) = _added.Append(text);
        var piece = new TextPiece(start, length, TextPiece.SourceType.Add);
        _textTree = _textTree.Insert(position, piece);
    }
    

    public ReadOnlySpan<char> GetPieceSpan(TextPiece piece) => GetContinuousSpan(piece.Start, piece.Length, piece.Source);

    private ReadOnlySpan<char> GetContinuousSpan(
        int start, 
        int length, 
        TextPiece.SourceType type)
    {
        return type == TextPiece.SourceType.Original
            ? _original.AsSpan(start, length)
            : _added.GetSpan(start, length);
    }
    
    
    public ref struct TextPieceEnumerator
    {
        private readonly TextStorage _storage;
        private TextTree.NodeEnumerator _pieceEnumerator;
    
    
        public ReadOnlySpan<char> Current => _storage.GetPieceSpan(_pieceEnumerator.Current);
    
    
        public TextPieceEnumerator(
            TextStorage storage,
            int start = 0, 
            int length = -1)
        {
            _storage = storage;
            _pieceEnumerator = storage._textTree.SlicedEnumerate(start, length);
        }
    
    
        public TextPieceEnumerator GetEnumerator() => this;


        public bool MoveNext() => _pieceEnumerator.MoveNext();
    
    
        public void Dispose() => _pieceEnumerator.Dispose();
    }
}