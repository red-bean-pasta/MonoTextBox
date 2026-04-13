using HeadlessTextBox.Storage.WeightedTree;

namespace HeadlessTextBox.Formatting;

public class FormatTree: Node<FormatPiece>
{
    public FormatTree(): this(default, null, null)
    { }
    
    public FormatTree(
        FormatPiece value, 
        FormatTree? leftSubNode, 
        FormatTree? rightSubNode) 
        : base(value, leftSubNode, rightSubNode)
    { }
    
    
    public NodeEnumerator GetEnumerator() => base.GetEnumerator();
    
    public NodeEnumerator EnumerateSliced(int start, int length) => base.GetEnumerator(start, length);
    

    
    public (FormatPiece Value, int RelativeIndex) Locate(Index position) => base.Find(position);


    public FormatTree Append(FormatPiece value)
    {
        return (FormatTree)base.AppendAndBalance(value);
    }


    protected override FormatTree InsertAndBalance(
        int index, 
        FormatPiece value)
    {
        OptimizedInsert(index, value);
        return (FormatTree)Balance();
    }

    private void OptimizedInsert(int index, FormatPiece value)
    {
        if (value.Length <= 0
            || index < LeftLength
            || index > BeforeRightLength)
        {
            base.Insert(value, index);
            return;
        }
        
        var (branch, relativeIndex) = Find(index);
        if (!branch.Format.Equals(value.Format))
        {
            base.Insert(value, index);
            return;
        }
        
        Value = branch with { Length = branch.Length + value.Length };
        Recalculate();
    }
}