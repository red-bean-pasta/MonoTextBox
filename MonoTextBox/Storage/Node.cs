using System.Diagnostics;

namespace MonoTextBox.Editing.Buffer;

public class Node
{
    private Piece _piece;

    private Node? _leftSubNode;
    private Node? _rightSubNode;

    private int _subTreeLength;
    private int _subTreeHeight;


    private int LeftLength => _leftSubNode?._subTreeLength ?? 0;
    private int BeforeRightLength => LeftLength + _piece.Length;
    private int LeftHeight => _leftSubNode?._subTreeHeight ?? 0;
    private int RightHeight => _rightSubNode?._subTreeHeight ?? 0;
    
    
    public Node(
        Piece piece, 
        Node? leftSubNode, 
        Node? rightSubNode)
    {
        _piece = piece;
        _leftSubNode = leftSubNode;
        _rightSubNode = rightSubNode;

        Recalculate();
    }
    
    
    public Piece FindPiece(int index)
    {
        switch (CheckLeftRight(index))
        {
            case < 0:
                Debug.Assert(_leftSubNode is not null);
                return _leftSubNode.FindPiece(index);
            case 0:
                return _piece;
            case > 0:
                Debug.Assert(_rightSubNode is not null);
                return _rightSubNode.FindPiece(index - BeforeRightLength);
        }
    }
    

    /// <returns>If depth added</returns>
    public Node InsertAndBalance(Piece piece, int index)
    {
        Insert(piece, index);
        return Balance();
    }
    
    private void Insert(Piece piece, int insertIndex)
    {
        if (insertIndex <= LeftLength)
        {
            Debug.Assert(_leftSubNode is not null);
            _leftSubNode = InsertToSubNode(_leftSubNode, piece, insertIndex);
        }
        else if (insertIndex >= BeforeRightLength)
        {
            var rightIndex = insertIndex - BeforeRightLength;
            _rightSubNode = InsertToSubNode(_rightSubNode, piece, rightIndex);
        }
        else
        {
            InsertToCurrentPiece(piece, insertIndex - LeftLength);
        }
        
        Recalculate();
    }

    private void InsertToCurrentPiece(Piece piece, int splitIndex)
    {
        var (leftSplit, rightSplit) = SplitPiece(_piece, splitIndex);
        _leftSubNode = InsertToSubNode(_leftSubNode, leftSplit, LeftLength);
        _rightSubNode = InsertToSubNode(_rightSubNode, rightSplit, 0);
        _piece = piece;
    }

    private static Node InsertToSubNode(Node? subNode, Piece piece, int insertIndex)
    {
        if (subNode is null)
            return new Node(piece, null, null);
        else
            return subNode.InsertAndBalance(piece, insertIndex);
    }
    

    // Rotate the tree if skewed.
    // Example:
    // a -> b -> c
    // =>
    // a <- b -> c
    private Node Balance()
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

    private static Node RotateRight(Node node)
    {
        Debug.Assert(node._leftSubNode is not null);
        
        var newRoot = node._leftSubNode;
        
        var newLeft = newRoot._leftSubNode;
        
        var newRight = node;
        newRight._leftSubNode = newRoot._rightSubNode;
        newRight.Recalculate();
        
        newRoot._leftSubNode = newLeft;
        newRoot._rightSubNode = newRight;
        newRoot.Recalculate();
        
        return newRoot;
    }

    private static Node RotateLeft(Node node)
    {
        Debug.Assert(node._rightSubNode is not null);

        var newRoot = node._rightSubNode;
        
        var newLeft = node;
        newLeft._rightSubNode = newRoot._leftSubNode;
        newLeft.Recalculate();
        
        var newRight = newRoot._rightSubNode;
        
        newRoot._leftSubNode = newLeft;
        newRoot._rightSubNode = newRight;
        newRoot.Recalculate();
        
        return newRoot;
    }

    private void StraightenLeft()
    {
        var left = _leftSubNode;
        Debug.Assert(left is not null);
        
        _leftSubNode = left.LeftHeight < left.RightHeight
            ? RotateLeft(left)
            : left;
    }
    
    private void StraightenRight()
    {
        var right = _rightSubNode;
        Debug.Assert(right is not null);
        
        _rightSubNode = right.RightHeight < right.LeftHeight
            ? RotateRight(right)
            : right;
    }


    private void Recalculate()
    {
        _subTreeHeight = Math.Max(
                _leftSubNode?._subTreeHeight ?? 0,
                _rightSubNode?._subTreeHeight ?? 0
            ) + 1;

        _subTreeLength = 
            _piece.Length
            + (_leftSubNode?._subTreeLength ?? 0)
            + (_rightSubNode?._subTreeLength ?? 0);
    }

    
    // left subtree: [0, leftLength)
    // current piece: [leftLength, leftLength + Piece.Length)
    // right subtree: [starts at leftLength + Piece.Length, ...)
    private int CheckLeftRight(int index)
    {
        if (index < LeftLength)
            return -1;
        
        if (index < BeforeRightLength) 
            return 0;
        
        return 1;
    }

    private static (Piece Left, Piece Right) SplitPiece(Piece piece, int index)
    {
        Debug.Assert(0 < index && index < piece.Length);
        var left = new Piece(piece.StartIndex, index, piece.Source);
        var right = new Piece(piece.StartIndex + index, piece.Length - index, piece.Source);
        return (left, right);
    }
}