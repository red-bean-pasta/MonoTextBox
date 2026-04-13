using HeadlessTextBox.Compositing.Contracts;
using HeadlessTextBox.Storage;

namespace HeadlessTextBox.Editing;

public static class UndoRedoHelper
{
    public static void Undo(
        in InputRecord input, 
        AddBuffer buffer, 
        TextStorage storage)
    {
        UndoInsert(input.InsertPosition, input.Inserted, storage);
        UndoDelete(input.CaretBefore.Left, input.Removed, buffer, storage);
    }

    public static void Redo(
        in InputRecord input, 
        AddBuffer buffer, 
        TextStorage storage)
    {
        RedoDelete(input.CaretBefore.Left, input.Removed, storage);
        RedoInsert(input.InsertPosition, input.Inserted, buffer, storage);
    }

    
    private static void UndoInsert(
        int position,
        BufferSlice piece,
        TextStorage storage)
    {
        storage.Remove(position, piece.Length);
    }
    
    private static void RedoInsert(
        int position,
        BufferSlice piece,
        AddBuffer buffer,
        TextStorage storage)
    {
        var span = buffer.GetSpan(piece.Start, piece.Length);
        storage.Insert(position, span);
    }


    private static void UndoDelete(
        int position,
        BufferSlice piece,
        AddBuffer buffer,
        TextStorage storage)
    {
        RedoInsert(position, piece, buffer, storage);
    }

    private static void RedoDelete(
        int position,
        BufferSlice piece,
        TextStorage storage)
    {
        UndoInsert(position, piece, storage);
    }
}