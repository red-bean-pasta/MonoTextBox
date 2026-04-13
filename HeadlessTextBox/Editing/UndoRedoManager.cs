using System.Diagnostics;
using HeadlessTextBox.Compositing.Contracts;
using HeadlessTextBox.Storage;

namespace HeadlessTextBox.Editing;

public class UndoRedoManager
{
    private readonly InputRecordStack _undoStack;
    private readonly InputRecordStack _redoStack;
    private readonly AddBuffer _buffer = new();
    
    
    public UndoRedoManager(int size)
    {
        _undoStack = new InputRecordStack(size);
        _redoStack = new InputRecordStack(size);
    }


    public bool GetLastRecord(out InputRecord record) => _undoStack.GetCurrent(out record);


    public void AddUndo(
        Caret caretBefore,
        ReadOnlySpan<char> removed = default,
        ReadOnlySpan<char> inserted = default)
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
            
            var (start, length) = _buffer.Append(span);
            return new BufferSlice(start, length);
        }
    }

    public void ExtendCurrentRemoved(ReadOnlySpan<char> removed)
    {
        _buffer.Append(removed);
        _undoStack.GetCurrent(out var record);
        Debug.Assert(record.Inserted.Length <= 0);
        record.ExtendRemoved(removed.Length);
    }
    
    public void ExtendCurrentInserted(ReadOnlySpan<char> inserted)
    {
        _buffer.Append(inserted);
        _undoStack.GetCurrent(out var record);
        record.ExtendInserted(inserted.Length);
    }
    
    
    public void Undo(TextStorage storage)
    {
        if (!_undoStack.GetCurrent(out var record))
            return;
        
        UndoRedoHelper.Undo(record, _buffer, storage);
        _redoStack.Add(record);
    }
    
    public void Redo(TextStorage storage)
    {
        if (!_redoStack.GetCurrent(out var record))
            return;
        
        UndoRedoHelper.Redo(record, _buffer, storage);
        _undoStack.Add(record);
    }


    private void ClearRedo()
    {
        _redoStack.Clear();
    }
}


public class InputRecordStack
{
    private int _count;
    private int _next;
    private readonly InputRecord[] _buffer;
    
    
    public InputRecordStack(int size = 256)
    {
        _buffer = new InputRecord[size];
        _count = 0;
        _next = 0;
    }


    public void Add(InputRecord record)
    {
        _buffer[_next] = record;
        _count++;
        _next++;
        _next %= _buffer.Length;
    }


    public bool GetCurrent(out InputRecord record)
    {
        if (_count <= 0)
        {
            record = default;
            return false;
        }

        var current = Wrap(_next - 1, _buffer.Length);
        record = _buffer[current];
        return true;
    }
    
    private static int Wrap(int i, int n)
    {
        return (i % n + n) % n;
    }


    public void Clear()
    {
        _count = 0;
        _next = 0;
    }
}