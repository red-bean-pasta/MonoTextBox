using HeadlessTextBox.Storage.WeightedTree;

namespace HeadlessTextBox.TextStoring;

public class TextTree: Node<TextPiece>
{
    public int Length => SubTreeLength;
    
    public TextTree(
        TextPiece value, 
        TextTree? leftSubNode, 
        TextTree? rightSubNode
    ) : base(value, leftSubNode, rightSubNode)
    { }
    
    public static TextTree Empty => new(new TextPiece(0, 0, TextPiece.SourceType.Original), null, null);
    
    
    public NodeEnumerator GetEnumerator() => base.GetEnumerator();

    public NodeEnumerator SlicedEnumerate(int start, int length) => new(this, start, length);


    public (TextPiece Value, int RelativeIndex) Locate(Index position) => base.Find(position);


    public TextTree Insert(int position, TextPiece value)
    {
        return this.InsertAndBalance(position, value);
    }


    public TextTree Remove(int start, int length)
    {
        return (TextTree?)base.RemoveAndBalance(start, length) ?? Empty;
    }


    protected override TextTree InsertAndBalance(int index, TextPiece value)
    {
        OptimizedInsert(value, index);
        return (TextTree)Balance();
    }
    
    private void OptimizedInsert(TextPiece value, int insertIndex)
    {
        if (value.Length <= 0 
            || insertIndex <= LeftLength 
            || insertIndex >= BeforeRightLength)
        {
            base.Insert(value, insertIndex);
            return;
        }

        MergeInsert(value, insertIndex);
        Recalculate();
    }

    private void MergeInsert(TextPiece piece, int absoluteIndex)
    {
        if (!TryMergePieces(Value, piece, out var merged))
        {
            base.InsertToCurrentT(piece, absoluteIndex - LeftLength);
            return;
        }
        
        base.ReplaceAndBalance(merged, absoluteIndex);
    }
    
    private static bool TryMergePieces(TextPiece a, TextPiece b, out TextPiece merged)
    {
        if (a.Source != b.Source)
        {
            merged = default;
            return false;
        }

        TextPiece left, right;
        if (a.Start <= b.Start)
        {
            left = a; 
            right = b;
        }
        else
        {
            left = b; 
            right = a;
        }

        if (left.Start + left.Length < right.Start)
        {
            merged = default;
            return false;
        }
        
        var start = left.Start;
        var end = Math.Max(left.Start + left.Length, right.Start + right.Length);
        var length = end - start;
        merged = new TextPiece(start, length, left.Source);
        return true;
    }
}