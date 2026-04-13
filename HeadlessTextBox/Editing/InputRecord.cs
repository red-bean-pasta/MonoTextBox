using HeadlessTextBox.Utils;

namespace HeadlessTextBox.Editing;


public record struct BufferSlice(int Start, int Length)
{
    public int Start { get; private set; } = Start;
    public int Length { get; private set; } = Length;

    public static BufferSlice Empty => default;

    public void Extend(int length)
    {
        Length += length;
    }
}


// 24-bits struct. Better pass it with `in`
public readonly record struct InputRecord(Caret CaretBefore)
{
    public BufferSlice Removed { get; init; } = BufferSlice.Empty;

    public int InsertPosition { get; init; } = CaretBefore.Left; // Useful when remove and insert doesn't anchor at the same position, like in moving 
    public BufferSlice Inserted { get; init; } = BufferSlice.Empty;
    

    public void ExtendInserted(int length = 1)
    {
        // AssertException.ThrowIf(Removed.Length > 0); // Possible when replacing "abc" with "d" then continue typing "ef"
        AssertException.ThrowIf(CaretBefore.Left == InsertPosition);
        Inserted.Extend(length);
    }

    public void ExtendRemoved(int length = 1)
    {
        AssertException.ThrowIf(Inserted.Length > 0);
        // AssertException.ThrowIf(CaretBefore.Length > 0); // Possible when deleting "abc" then continue deleting "ef"
        Removed.Extend(length);
    }
}