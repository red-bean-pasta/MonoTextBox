using HeadlessTextBox.Utils.WeightedTree;

namespace HeadlessTextBox.Storage;

public class PieceTree: Node<Piece>
{
    public int Length => SubTreeLength;
    
    public PieceTree(
        Piece value, 
        Node<Piece>? leftSubNode, 
        Node<Piece>? rightSubNode
    ) : base(value, leftSubNode, rightSubNode)
    { }
}