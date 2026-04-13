using HeadlessTextBox.Storage.WeightedTree;

namespace HeadlessTextBox.Formatting;

public readonly record struct FormatPiece(
    IFormat Format, 
    int Length
): IBranch<FormatPiece>
{
    public (FormatPiece, FormatPiece) Split(int index)
    {
        var left = new FormatPiece(Format, index);
        var right = new FormatPiece(Format, Length - index);
        return (left, right);
    }
}