using System.Buffers;
using System.Diagnostics;

namespace HeadlessTextBox.Utils.WeightedTree;

public class Node<T> where T : IBranch<T>
{
    protected T Value;

    protected Node<T>? LeftSubNode;
    protected Node<T>? RightSubNode;

    protected int SubTreeLength;
    protected int SubTreeHeight;
    
    
    public NodeEnumerator GetEnumerator() => new(this);


    protected int LeftLength => LeftSubNode?.SubTreeLength ?? 0;
    protected int BeforeRightLength => LeftLength + Value.Length;
    protected int LeftHeight => LeftSubNode?.SubTreeHeight ?? 0;
    protected int RightHeight => RightSubNode?.SubTreeHeight ?? 0;
    
    
    public Node(
        T value, 
        Node<T>? leftSubNode, 
        Node<T>? rightSubNode)
    {
        Value = value;
        LeftSubNode = leftSubNode;
        RightSubNode = rightSubNode;

        Recalculate();
    }
    
    
    public (T Value, int RelativeIndex) Find(Index absoluteIndex)
    {
        var normalized = absoluteIndex.GetOffset(SubTreeLength);
        return Find(normalized);
    }
    
    public (T Value, int RelativeIndex) Find(int absoluteIndex)
    {
        switch (CheckLeftRight(absoluteIndex))
        {
            case < 0:
                Debug.Assert(LeftSubNode is not null);
                return LeftSubNode.Find(absoluteIndex);
            case 0:
                return (Value, absoluteIndex);
            case > 0:
                Debug.Assert(RightSubNode is not null);
                return RightSubNode.Find(absoluteIndex - BeforeRightLength);
        }
    }

    
    public virtual Node<T> AppendAndBalance(T value)
    {
        var index = BeforeRightLength + RightSubNode?.SubTreeLength ?? 0;
        return InsertAndBalance(index, value);
    }
    
    /// <returns>If depth added</returns>
    public virtual Node<T> InsertAndBalance(int index, T value)
    {
        Insert(value, index);
        return Balance();
    }
    
    protected void Insert(T value, int insertIndex)
    {
        if (value.Length <= 0)
        {
            Debug.Assert(LeftSubNode is null && RightSubNode is null);
            Value = value;
        }
        else if (insertIndex <= LeftLength)
        {
            LeftSubNode = InsertToSubNode(LeftSubNode, value, insertIndex);
        }
        else if (insertIndex >= BeforeRightLength)
        {
            var rightIndex = insertIndex - BeforeRightLength;
            RightSubNode = InsertToSubNode(RightSubNode, value, rightIndex);
        }
        else
        {
            InsertToCurrentT(value, insertIndex - LeftLength);
        }
        
        Recalculate();
    }

    protected void InsertToCurrentT(T value, int splitIndex)
    {
        var (leftSplit, rightSplit) = SplitT(Value, splitIndex);
        LeftSubNode = InsertToSubNode(LeftSubNode, leftSplit, LeftLength);
        RightSubNode = InsertToSubNode(RightSubNode, rightSplit, 0);
        Value = value;
    }

    protected static Node<T> InsertToSubNode(Node<T>? subNode, T value, int insertIndex)
    {
        if (subNode is null)
            return new Node<T>(value, null, null);
        else
            return subNode.InsertAndBalance(insertIndex, value);
    }


    public virtual Node<T>? RemoveAndBalance(int index)
    {
        if (index < LeftLength)
        {
            Debug.Assert(LeftSubNode is not null);
            LeftSubNode = LeftSubNode.RemoveAndBalance(index);
        }
        else if (index >= BeforeRightLength)
        {
            RightSubNode = RightSubNode?.RemoveAndBalance(index - BeforeRightLength);
        }
        else if (LeftSubNode is not null)
        {
            var (value, left) = LeftSubNode.PopRightLeaf();
            LeftSubNode = left;
            Value = value;
        }
        else if (RightSubNode is not null)
        {
            var (value, right) = RightSubNode.PopLeftLeaf();
            RightSubNode = right;
            Value = value;
        }
        else
        {
            return null;
        }
        
        Recalculate();
        return Balance();
    }

    protected (T Value, Node<T>? Node) PopRightLeaf()
    {
        if (RightSubNode is null)
            return (Value, LeftSubNode);
        
        var (leaf, right) = RightSubNode.PopRightLeaf();
        RightSubNode = right;
        Recalculate();
        return (leaf, Balance());
    }
    
    protected (T Value, Node<T>? Node) PopLeftLeaf()
    {
        if (LeftSubNode is null)
            return (Value, RightSubNode);
        
        var (leaf, left) = LeftSubNode.PopLeftLeaf();
        LeftSubNode = left;
        Recalculate();
        return (leaf, Balance());
    }
    

    // Rotate the tree if skewed.
    // Example:
    // a -> b -> c
    // =>
    // a <- b -> c
    protected Node<T> Balance()
    {
        var difference = LeftHeight - RightHeight;
        switch (difference)
        {
            case > 1:
                StraightenLeft();
                return RotateRight(this);
            case < -1:
                StraightenRight();
                return RotateLeft(this);
            default:
                return this;
        }
    }

    protected static Node<T> RotateRight(Node<T> node)
    {
        Debug.Assert(node.LeftSubNode is not null);
        
        var newRoot = node.LeftSubNode;
        
        var newLeft = newRoot.LeftSubNode;
        
        var newRight = node;
        newRight.LeftSubNode = newRoot.RightSubNode;
        newRight.Recalculate();
        
        newRoot.LeftSubNode = newLeft;
        newRoot.RightSubNode = newRight;
        newRoot.Recalculate();
        
        return newRoot;
    }

    protected static Node<T> RotateLeft(Node<T> node)
    {
        Debug.Assert(node.RightSubNode is not null);

        var newRoot = node.RightSubNode;
        
        var newLeft = node;
        newLeft.RightSubNode = newRoot.LeftSubNode;
        newLeft.Recalculate();
        
        var newRight = newRoot.RightSubNode;
        
        newRoot.LeftSubNode = newLeft;
        newRoot.RightSubNode = newRight;
        newRoot.Recalculate();
        
        return newRoot;
    }

    protected void StraightenLeft()
    {
        var left = LeftSubNode;
        Debug.Assert(left is not null);
        
        LeftSubNode = left.LeftHeight < left.RightHeight
            ? RotateLeft(left)
            : left;
    }
    
    protected void StraightenRight()
    {
        var right = RightSubNode;
        Debug.Assert(right is not null);
        
        RightSubNode = right.RightHeight < right.LeftHeight
            ? RotateRight(right)
            : right;
    }


    protected void Recalculate()
    {
        SubTreeHeight = Math.Max(
                LeftSubNode?.SubTreeHeight ?? 0,
                RightSubNode?.SubTreeHeight ?? 0
            ) + 1;

        SubTreeLength = 
            Value.Length
            + (LeftSubNode?.SubTreeLength ?? 0)
            + (RightSubNode?.SubTreeLength ?? 0);
    }

    
    // left subtree: [0, leftLength)
    // current value: [leftLength, leftLength + T.Length)
    // right subtree: [starts at leftLength + T.Length, ...)
    private int CheckLeftRight(int index)
    {
        if (index < LeftLength)
            return -1;
        
        if (index < BeforeRightLength) 
            return 0;
        
        return 1;
    }

    private static (T Left, T Right) SplitT(T value, int index)
    {
        Debug.Assert(0 < index && index < value.Length);
        return value.Split(index);
    }
    
    
    public ref struct NodeEnumerator
    {
        private readonly Node<T>[] _paths;
        private int _depth;

        private bool _enumerateStarted = false;

            
        private Node<T> CurrentNode => _paths[_depth - 1];
        public T Current => CurrentNode.Value;
    
    
        public NodeEnumerator(Node<T> root)
        {
            var maxDepth = root.SubTreeHeight;
            _paths = ArrayPool<Node<T>>.Shared.Rent(maxDepth);
            _depth = 0;
            
            MoveToLeftest(root);
            
            AddPath(null!); // For initial MoveNext
        }
    

        public bool MoveNext()
        {
            if (!_enumerateStarted)
            {
                _enumerateStarted = true;
                return true;
            }
            
            if (_depth == 0) 
                return false;
            
            PopPath();
            var right = CurrentNode.RightSubNode;
            if (right is not null)
            {
                AddPath(right);
                MoveToLeftest(right);
            }
            return _depth > 0;
        }


        private void MoveToLeftest(Node<T> root)
        {
            AddPath(root);
            
            var current = root;
            while (true)
            {
                var left = current.LeftSubNode;
                if (left is null) 
                    return;

                Debug.Assert(left.SubTreeLength > 0);
                AddPath(left);
                current = left;
            }
        }
        
        private void AddPath(Node<T> node)
        {
            _depth++;
            _paths[_depth - 1] = node;
        }

        private void PopPath()
        {
            _depth--;
        }
    
    
        public void Dispose()
        {
            if (_paths != null) 
                ArrayPool<Node<T>>.Shared.Return(_paths, clearArray: true);
        }
    }
}

