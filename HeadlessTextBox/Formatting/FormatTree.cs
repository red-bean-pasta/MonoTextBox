using System.Diagnostics;
using HeadlessTextBox.Storage.WeightedTree;

namespace HeadlessTextBox.Formatting;

public class FormatTree: Node<FormatPiece>
{
    public int Length => SubTreeLength;
    
    
    public FormatTree(): this(default, null, null)
    { }
    
    public FormatTree(
        FormatPiece value, 
        FormatTree? leftSubNode, 
        FormatTree? rightSubNode) 
        : base(value, leftSubNode, rightSubNode)
    { }
    
    public static FormatTree Empty => new(default, null, null);

    
    
    public NodeEnumerator GetEnumerator() => base.GetEnumerator();
    
    public NodeEnumerator EnumerateSliced(int start, int length) => base.GetEnumerator(start, length);
    

    
    public (FormatPiece Value, int RelativeIndex) Locate(Index position) => base.Find(position);
    

    public FormatTree Append(FormatPiece value)
    {
        return (FormatTree)base.AppendAndBalance(value);
    }


    public FormatTree Extend(int index, int length)
    {
        if (index < LeftLength)
        {
            Debug.Assert(LeftSubNode is not null);
            LeftSubNode = ((FormatTree)LeftSubNode).Extend(index, length);
        }

        if (index > BeforeRightLength)
        {
            Debug.Assert(RightSubNode is not null);
            RightSubNode = ((FormatTree)RightSubNode).Extend(index, length);
        }
        
        Value = Value with { Length = Value.Length + length };
        
        Recalculate();
        return this;
    }


    public FormatTree Insert(int index, FormatPiece value)
    {
        return InsertAndBalance(index, value);
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
        
        if (!Value.Format.Equals(value.Format))
        {
            base.Insert(value, index);
            return;
        }
        
        Value = Value with { Length = Value.Length + value.Length };
        Recalculate();
    }
    
    
    public FormatTree Remove(int start, int length)
    {
        return (FormatTree?)base.RemoveAndBalance(start, length) ?? Empty;
    }


    public FormatTree Change(int start, int length, IFormat format)
    {
        var piece = new FormatPiece(format, length);
        return (FormatTree)ChangeAndBalance(piece, start, length);
    }
}