using System.Buffers;
using System.Diagnostics;
using HeadlessTextBox.Utils;
using JetBrains.Annotations;

namespace HeadlessTextBox.Storage.WeightedTree;

public class Node<T> where T : IBranch<T>
{
    protected T Value;

    protected Node<T>? LeftSubNode;
    protected Node<T>? RightSubNode;

    protected int SubTreeLength;
    protected int SubTreeHeight;

    
    protected int LeftLength => LeftSubNode?.SubTreeLength ?? 0;
    protected int BeforeRightLength => LeftLength + Value.Length;
    protected int LeftHeight => LeftSubNode?.SubTreeHeight ?? 0;
    protected int RightHeight => RightSubNode?.SubTreeHeight ?? 0;
    
    
    protected Node(
        T value, 
        Node<T>? leftSubNode, 
        Node<T>? rightSubNode)
    {
        Value = value;
        LeftSubNode = leftSubNode;
        RightSubNode = rightSubNode;

        Recalculate();
    }
    
    [MustDisposeResource]
    protected NodeEnumerator GetEnumerator(int start = 0, int length = -1) => new(this, start, length);
    
    
    protected (T Value, int RelativeIndex) Find(Index absoluteIndex)
    {
        var normalized = absoluteIndex.GetOffset(SubTreeLength);
        return Find(normalized);
    }
    
    protected (T Value, int RelativeIndex) Find(int absoluteIndex)
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

    
    protected virtual Node<T> AppendAndBalance(T value)
    {
        var index = BeforeRightLength + RightSubNode?.SubTreeLength ?? 0;
        return InsertAndBalance(index, value);
    }
    
    /// <returns>If depth added</returns>
    protected virtual Node<T> InsertAndBalance(int index, T value)
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
        return subNode is null 
            ? new Node<T>(value, null, null) 
            : subNode.InsertAndBalance(insertIndex, value);
    }

    
    protected virtual Node<T> ChangeAndBalance(T value, int start, int length)
    {
        var result = this;
        var remainValue = value;
        
        var (leftSlice, middleSlice, rightSlice) = DivideRange(start, length);
        
        if (leftSlice.Length > 0)
        {
            Debug.Assert(result.LeftSubNode is not null);
            (var leftValue, remainValue) = remainValue.Split(leftSlice.End);
            result.LeftSubNode = result.LeftSubNode.ChangeAndBalance(leftValue, leftSlice.Start, leftSlice.Length);
        }

        if (middleSlice.Length > 0)
        {
            (var middleValue, remainValue) = remainValue.Split(middleSlice.End);
            if (middleSlice.Length == Value.Length)
            {
                result = result.ReplaceAndBalance(middleValue, middleSlice.Start);
            }
            else 
            {
                var currentValue = result.Value;
                var middleLeft = middleValue;
                var middleRight = currentValue.Split(middleSlice.End).Item2;
                result = result.PopAndBalance(middleSlice.Start);
                result = result?.InsertAndBalance(middleSlice.Start, middleRight) ?? BuildChildNode(middleRight);
                result = result.InsertAndBalance(middleSlice.Start, middleLeft);
            }
        }

        if (rightSlice.Length > 0)
        {
            (var rightValue, remainValue) = remainValue.Split(middleSlice.End);
            Debug.Assert(remainValue.Length == 0);
            Debug.Assert(result.RightSubNode is not null);
            result.RightSubNode = result.RightSubNode.ChangeAndBalance(rightValue, rightSlice.Start - BeforeRightLength, rightSlice.Length);
        }
        
        Recalculate();
        return result.Balance();
    }
    
    
    protected virtual Node<T> ReplaceAndBalance(T value, int index)
    {
        if (index < LeftLength)
        {
            Debug.Assert(LeftSubNode is not null);
            LeftSubNode = LeftSubNode.ReplaceAndBalance(value, index);
        }
        else if (index >= BeforeRightLength)
        {
            Debug.Assert(RightSubNode is not null);
            RightSubNode = RightSubNode.ReplaceAndBalance(value, index - BeforeRightLength);
        }
        else
        {
            Value = value;
        }
        
        Recalculate();
        return Balance();
    }
    
    
    protected virtual Node<T>? RemoveAndBalance(int start, int length)
    {
        var result = this;
        
        var (leftSlice, middleSlice, rightSlice) = DivideRange(start, length);
        
        if (leftSlice.Length > 0)
        {
            Debug.Assert(result.LeftSubNode is not null);
            result.LeftSubNode = result.LeftSubNode.RemoveAndBalance(leftSlice.Start, leftSlice.Length);
        }

        if (middleSlice.Length == Value.Length)
        {
            Debug.Assert(middleSlice.Length > 0);
            result = result.PopAndBalance(middleSlice.Start);
        }
        else if (middleSlice.Length > 0)
        {
            var value = result.Value;
            var (l, m, r) = TrisectT(value, middleSlice.Start, middleSlice.End);
            result = result.PopAndBalance(middleSlice.Start);
            result = result?.InsertAndBalance(middleSlice.Start, r) 
                     ?? BuildChildNode(r);
            result = result.InsertAndBalance(middleSlice.Start, m);
            result = result.InsertAndBalance(middleSlice.Start, l);
        }

        if (rightSlice.Length > 0)
        {
            Debug.Assert(result?.RightSubNode != null);
            result.RightSubNode = result.RightSubNode.RemoveAndBalance(rightSlice.Start - BeforeRightLength, rightSlice.Length);
        }
        
        Recalculate();
        return result?.Balance();
    }


    protected virtual Node<T>? PopAndBalance(int index)
    {
        if (index < LeftLength)
        {
            Debug.Assert(LeftSubNode is not null);
            LeftSubNode = LeftSubNode.PopAndBalance(index);
        }
        else if (index >= BeforeRightLength)
        {
            RightSubNode = RightSubNode?.PopAndBalance(index - BeforeRightLength);
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
    protected virtual Node<T> Balance()
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


    protected virtual void Recalculate()
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
    protected int CheckLeftRight(int index)
    {
        if (index < LeftLength)
            return -1;
        
        if (index < BeforeRightLength) 
            return 0;
        
        return 1;
    }

    protected static (T Left, T Right) SplitT(T value, int index)
    {
        Debug.Assert(0 < index && index < value.Length);
        return value.Split(index);
    }
    
    protected (T Left, T Middle, T Right) TrisectT(T value, int start, int end)
    {
        var (l, r) = value.Split(start);
        var (middle, right) = r.Split(end - start);
        return (l, middle, right);
    }

    protected (Slice Left, Slice Middle, Slice Right) DivideRange(int start, int length)
    {
        var leftSplitPoint = Math.Max(start, LeftLength);
        var rightSplitPoint = Math.Min(start + length, BeforeRightLength);
        
        var left = new Slice(start, leftSplitPoint - start);
        var middle = new Slice(leftSplitPoint, rightSplitPoint - leftSplitPoint);
        var right = new Slice(rightSplitPoint, start + length - rightSplitPoint);
        
        return (left, middle, right);
    }

    protected static Node<T> BuildChildNode(T value, Node<T>? leftSubNode = null, Node<T>? rightSubNode = null)
    {
        return new Node<T>(value, leftSubNode, rightSubNode);
    }
    
    
    public ref struct NodeEnumerator
    {
        private int _remainingLength;
        
        private readonly Node<T>[] _paths;
        private int _depth;

        private bool _enumerateStarted = false;

            
        private Node<T> CurrentNode => _paths[_depth - 1];
        public T Current => GetCurrentValue();
    
    
        public NodeEnumerator(Node<T> root, int start = 0, int length = -1)
        {
            _remainingLength = length < 0 ? root.SubTreeLength - start : length;
            
            var maxDepth = root.SubTreeHeight;
            _paths = ArrayPool<Node<T>>.Shared.Rent(maxDepth);
            _depth = 0;
            
            MoveTo(root, start);
            
            AddPath(null!); // For initial MoveNext
        }
        
        
        public NodeEnumerator GetEnumerator() => this;
    

        public bool MoveNext()
        {
            if (!_enumerateStarted)
            {
                _enumerateStarted = true;
                return true;
            }
            
            if (_depth == 0) 
                return false;

            _remainingLength -= CurrentNode.Value.Length;
            if (_remainingLength <= 0)
                return false;

            PopPath();
            var right = CurrentNode.RightSubNode;
            if (right is not null) 
                MoveToLeftest(right);
            return _depth > 0;
        }


        private void MoveTo(Node<T> root, int start)
        {
            while (true)
            {
                if (start < root.LeftLength)
                {
                    AddPath(root);
                    
                    var leftNode = root.LeftSubNode;
                    var leftStart = start;
                    Debug.Assert(leftNode is not null && leftNode.SubTreeLength > 0);
                    root = leftNode;
                    start = leftStart;
                    
                    continue;
                }

                if (start == root.BeforeRightLength)
                {
                    AddPath(root);
                    return;
                }
                if (start < root.BeforeRightLength)
                {
                    var (leftValue, rightValue) = root.Value.Split(start - root.BeforeRightLength);
                    var node = new Node<T>(rightValue, null, null);
                    AddPath(node);
                    return;
                }
                
                var rightNode = root.RightSubNode;
                var rightStart = start - root.BeforeRightLength;
                Debug.Assert(rightNode is not null && rightNode.SubTreeLength > 0);
                root = rightNode;
                start = rightStart;
            }
        }

        private void MoveToLeftest(Node<T> root) => MoveTo(root, 0);

        private void AddPath(Node<T> node)
        {
            _depth++;
            _paths[_depth - 1] = node;
        }

        private void PopPath()
        {
            _depth--;
        }


        private T GetCurrentValue()
        {
            var value = CurrentNode.Value;
            if (_remainingLength >= value.Length)
                return value;
            
            var (left, right) = value.Split(_remainingLength);
            return left;
        }
    
    
        public void Dispose()
        {
            if (_paths != null) 
                ArrayPool<Node<T>>.Shared.Return(_paths, clearArray: true);
        }
    }
}

