using HeadlessTextBox.Compositing.Contracts;
using HeadlessTextBox.Editing.Recording;

namespace HeadlessTextBox.Editing;

public static class UndoRedoHelper
{
    public static void Undo(
        Record record, 
        TextRecorder textRecorder, 
        FormatRecorder formatRecorder,
        SourceBuffer storage)
    {
        if (record.InsertedText.Count > 0 || record.RemovedText.Count > 0)
        {
            UndoInsert(record.InsertedText, textRecorder, storage);
            UndoRemove(record.RemovedText, textRecorder, storage);
        }
        else
        {
            UndoFormat(record.RemovedFormat, formatRecorder, storage);
        }
    }
    
    public static void Redo(
        Record record, 
        TextRecorder textRecorder, 
        FormatRecorder formatRecorder,
        SourceBuffer storage)
    {
        if (record.InsertedText.Count > 0 || record.RemovedText.Count > 0)
        {
            RedoRemove(record.RemovedText, textRecorder, storage);
            RedoInsert(record.InsertedText, textRecorder, storage);
        }
        else
        {
            RedoFormat(record.AppliedFormat, formatRecorder, storage);
        }
    }
    
    
    private static void UndoInsert(
        TextUnit insertUnit,
        TextRecorder textRecorder,
        SourceBuffer storage)
    {
        var refs = textRecorder.GetRefs(insertUnit.Start, insertUnit.Count);
        foreach (var r in refs) 
            storage.Remove(r.Position, r.Length);
    }

    private static void UndoRemove(
        TextUnit removeUnit,
        TextRecorder textRecorder,
        SourceBuffer storage)
    {
        RedoInsert(removeUnit, textRecorder, storage);
    }
    
    private static void RedoInsert(
        TextUnit insertUnit,
        TextRecorder textRecorder,
        SourceBuffer storage)
    {
        var refs = textRecorder.GetRefs(insertUnit.Start, insertUnit.Count);
        foreach (var r in refs)
        {
            var chars = textRecorder.GetChars(r.Start, r.Length);
            storage.Insert(r.Position, chars);
        }
    }
    
    private static void RedoRemove(
        TextUnit removeUnit,
        TextRecorder textRecorder,
        SourceBuffer storage)
    {
        UndoInsert(removeUnit, textRecorder, storage);
    }

    private static void UndoFormat(
        FormatUnit removeUnit,
        FormatRecorder formatRecorder,
        SourceBuffer storage)
    {
        ApplyFormat(removeUnit, formatRecorder, storage);
    }
    
    private static void RedoFormat(
        FormatUnit applyUnit,
        FormatRecorder formatRecorder,
        SourceBuffer storage)
    {
        ApplyFormat(applyUnit, formatRecorder, storage);
    }

    
    private static void ApplyFormat(
        FormatUnit unit,
        FormatRecorder formatRecorder,
        SourceBuffer storage)
    {
        var formats = formatRecorder.GetPieces(unit.Start, unit.Count);
        foreach (var f in formats)
            storage.ChangeFormat(f.Position, f.Length, f.Format);
    }
}