using System.Diagnostics;
using System.Net.Mime;
using HeadlessTextBox.Compositing.Storage;
using HeadlessTextBox.Editing.Recording;

namespace HeadlessTextBox.Editing;

public class UndoRedoManager
{
    private readonly InputRecordStack _undoStack;
    private readonly InputRecordStack _redoStack;
    
    
    private readonly MediaTypeNames.Text
    
    private readonly FormatBuffer _formatBuffer = new();
    
    
    public UndoRedoManager(int size)
    {
        _undoStack = new InputRecordStack(size);
        _redoStack = new InputRecordStack(size);
    }


    public bool GetLastRecord(out InputRecord record) => _undoStack.GetCurrentValue(out record);


    public void AddUndo(
        Caret caretBefore,
        ReadOnlySpan<char> removed,
        ReadOnlySpan<char> inserted)
    {
        var removeSlice = BufferAndGetInput(removed);
        var insertSlice = BufferAndGetInput(inserted);
        var record = new InputRecord(caretBefore)
        {
            Removed = removeSlice,
            Inserted = insertSlice
        };
        _undoStack.Add(record);
        ClearRedo();
        
        return;
        BufferSlice BufferAndGetInput(ReadOnlySpan<char> span)
        {
            if (span.Length <= 0) 
                return default;
            
            var (start, length) = _formatBuffer.Append(span);
            return new BufferSlice(start, length);
        }
    }

    public void ExtendCurrentRemoved(ReadOnlySpan<char> removed)
    {
        _formatBuffer.Append(removed);
        _undoStack.GetCurrent(out var record);
        Debug.Assert(record.Inserted.Length <= 0);
        record.ExtendRemoved(removed.Length);
    }
    
    public void ExtendCurrentInserted(ReadOnlySpan<char> inserted)
    {
        _formatBuffer.Append(inserted);
        _undoStack.GetCurrent(out var record);
        record.ExtendInserted(inserted.Length);
    }
    
    
    public void Undo(TextStorage storage)
    {
        if (!_undoStack.GetCurrent(out var record))
            return;
        
        UndoRedoHelper.Undo(record, _formatBuffer, storage);
        _redoStack.Add(record);
    }
    
    public void Redo(TextStorage storage)
    {
        if (!_redoStack.GetCurrent(out var record))
            return;
        
        UndoRedoHelper.Redo(record, _formatBuffer, storage);
        _undoStack.Add(record);
    }


    private void ClearRedo()
    {
        _redoStack.Clear();
    }


    private void Compact()
    {
        throw new NotImplementedException();
    }
}


