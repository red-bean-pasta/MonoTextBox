using HeadlessTextBox.Legacy.Editing.Inputs.Interfaces;

namespace HeadlessTextBox.Legacy.Editing;

public class UndoRedoManager
{
    private readonly int _max;
    
    private readonly LinkedList<IInput> _undoStack = new();
    private readonly Stack<IInput> _redoStack = new();

    
    public UndoRedoManager(int max) 
        => this._max = max;

    
    public void Add(IInput step)
    {
        Push(_undoStack, step, _max);
        _redoStack.Clear();
    }
    
    
    public Caret Undo(List<char> source, Caret caret)
    {
        if (_undoStack.Count == 0)
            return caret;
        
        var input = Pop(_undoStack);
        _redoStack.Push(input);
        return input.Undo(caret, source);
    }
    
    public Caret Redo(List<char> source, Caret caret)
    {
        if (_redoStack.Count == 0)
            return caret;
        
        var input = _redoStack.Pop();
        _undoStack.AddLast(input);
        return input.Redo(caret, source);
    }


    private static T Pop<T>(LinkedList<T> list)
    {
        var item = list.Last();
        list.RemoveLast();
        return item;
    }

    private static void Push<T>(LinkedList<T> list, T item, int max = -1)
    {
        list.AddLast(item);
        
        if (max >= 0 && list.Count > max) 
            list.RemoveFirst();
    }
}