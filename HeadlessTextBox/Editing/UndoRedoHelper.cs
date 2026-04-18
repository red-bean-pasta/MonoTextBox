using HeadlessTextBox.Compositing.Contracts;
using HeadlessTextBox.Editing.Recording;

namespace HeadlessTextBox.Editing;

public static class UndoRedoHelper
{
    public static void Undo(
        Record record, 
        TextRefBuffer refBuffer,
        TextBuffer charBuffer, 
        FormatBuffer formatBuffer,
        SourceBuffer storage)
    {
        if (record.InsertedText.Count > 0 || record.RemovedText.Count > 0)
        {
            UndoInsert(record.InsertedText, refBuffer, storage);
            UndoRemove(record.RemovedText, refBuffer, charBuffer, storage);
        }
        else
        {
            UndoFormat(record.RemovedFormat, formatBuffer, storage);
        }
    }
    
    public static void Redo(
        Record record, 
        TextRefBuffer refBuffer,
        TextBuffer charBuffer, 
        FormatBuffer formatBuffer,
        SourceBuffer storage)
    {
        if (record.InsertedText.Count > 0 || record.RemovedText.Count > 0)
        {
            RedoRemove(record.RemovedText, refBuffer, storage);
            RedoInsert(record.InsertedText, refBuffer, charBuffer, storage);
        }
        else
        {
            RedoFormat(record.AppliedFormat, formatBuffer, storage);
        }
    }
    
    
    private static void UndoInsert(
        TextUnit insertUnit,
        TextRefBuffer refBuffer,
        SourceBuffer storage)
    {
        var refs = refBuffer.GetSpan(insertUnit.Start, insertUnit.Count);
        foreach (var r in refs) 
            storage.Remove(r.Position, r.Length);
    }

    private static void UndoRemove(
        TextUnit removeUnit,
        TextRefBuffer refBuffer,
        TextBuffer charBuffer,
        SourceBuffer storage)
    {
        RedoInsert(removeUnit, refBuffer, charBuffer, storage);
    }
    
    private static void RedoInsert(
        TextUnit insertUnit,
        TextRefBuffer refBuffer,
        TextBuffer charBuffer,
        SourceBuffer storage)
    {
        var refs = refBuffer.GetSpan(insertUnit.Start, insertUnit.Count);
        foreach (var r in refs)
        {
            var chars = charBuffer.GetSpan(r.Start, r.Length);
            storage.Insert(r.Position, chars);
        }
    }
    
    private static void RedoRemove(
        TextUnit removeUnit,
        TextRefBuffer refBuffer,
        SourceBuffer storage)
    {
        UndoInsert(removeUnit, refBuffer, storage);
    }

    private static void UndoFormat(
        FormatUnit removeUnit,
        FormatBuffer formatBuffer,
        SourceBuffer storage)
    {
        ApplyFormat(removeUnit, formatBuffer, storage);
    }
    
    private static void RedoFormat(
        FormatUnit applyUnit,
        FormatBuffer formatBuffer,
        SourceBuffer storage)
    {
        ApplyFormat(applyUnit, formatBuffer, storage);
    }

    
    private static void ApplyFormat(
        FormatUnit unit,
        FormatBuffer formatBuffer,
        SourceBuffer storage)
    {
        var formats = formatBuffer.GetSpan(unit.Start, unit.Count);
        foreach (var f in formats)
            storage.ChangeFormat(f.Position, f.Length, f.Format);
    }
}