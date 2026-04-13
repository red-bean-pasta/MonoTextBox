using HeadlessTextBox.Storage;
using HeadlessTextBox.TextStoring;

namespace HeadlessTextBox.Compositing.Contracts;

public record TextStorage(
    string Original,
    AddBuffer Added,
    TextTree TextTree
)
{
    public TextTree TextTree { get; private set; } = TextTree;
    
    
    public int Length => TextTree.Length;
    
    
    public TextStorageEnumerator GetEnumerator() => new(this);

    public TextStorageEnumerator SlicedEnumerate(int start, int length) => new(this, start, length);


    public void Remove(int position, int length)
    {
        TextTree = TextTree.Remove(position, length);
    }
    
    public void Insert(int position, ReadOnlySpan<char> text)
    {
        var (start, length) = Added.Append(text);
        var piece = new TextPiece(start, length, TextPiece.SourceType.Add);
        TextTree = TextTree.Insert(position, piece);
    }
    

    public ReadOnlySpan<char> GetPieceSpan(TextPiece piece) => GetContinuousSpan(piece.Start, piece.Length, piece.Source);

    private ReadOnlySpan<char> GetContinuousSpan(
        int start, 
        int length, 
        TextPiece.SourceType type)
    {
        return type == TextPiece.SourceType.Original
            ? Original.AsSpan(start, length)
            : Added.GetSpan(start, length);
    }
}


public ref struct TextStorageEnumerator
{
    private readonly TextStorage _storage;
    private TextTree.NodeEnumerator _pieceEnumerator;
    
    
    public ReadOnlySpan<char> Current => _storage.GetPieceSpan(_pieceEnumerator.Current);
    
    
    public TextStorageEnumerator(
        TextStorage storage, 
        int start = 0, 
        int length = -1)
    {
        _storage = storage;
        _pieceEnumerator = _storage.TextTree.SlicedEnumerate(start, length);
    }
    
    
    public TextStorageEnumerator GetEnumerator() => this;


    public bool MoveNext() => _pieceEnumerator.MoveNext();
    
    
    public void Dispose() => _pieceEnumerator.Dispose();
}