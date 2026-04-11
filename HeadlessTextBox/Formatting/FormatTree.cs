using HeadlessTextBox.Utils.WeightedTree;

namespace HeadlessTextBox.Formatting;

public class FormatTree: Node<FormatBranch>
{
    public FormatTree(): this(default, null, null)
    { }
    
    public FormatTree(
        FormatBranch value, 
        Node<FormatBranch>? leftSubNode, 
        Node<FormatBranch>? rightSubNode) 
        : base(value, leftSubNode, rightSubNode)
    { }


    public override Node<FormatBranch> InsertAndBalance(
        int index, 
        FormatBranch value)
    {
        OptimizedInsert(index, value);
        return Balance();
    }

    private void OptimizedInsert(int index, FormatBranch value)
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

public readonly record struct FormatBranch(
    IFormat Format, 
    int Length
    ) : IBranch<FormatBranch>
{
    public (FormatBranch, FormatBranch) Split(int index)
    {
        var left = new FormatBranch(Format, index);
        var right = new FormatBranch(Format, Length - index);
        return (left, right);
    }
}