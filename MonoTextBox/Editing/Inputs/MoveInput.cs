using MonoTextBox.Editing.Inputs.Interfaces;

namespace MonoTextBox.Editing.Inputs;

public class MoveInput: IUndoRedoInput
{
    private readonly Caret _startCaret;
    
    private readonly int _destIndex;


    /// <summary>
    /// 
    /// </summary>
    /// <param name="startCaret"></param>
    /// <param name="destIndex"></param>
    public MoveInput(
        Caret startCaret,
        int destIndex)
    {
        _startCaret = startCaret;
        _destIndex = destIndex;
    }
    

    public Caret Undo(List<char> source, Caret caret)
    {
        var content = source.GetRange(_destIndex, _startCaret.Length);
        
        source.RemoveRange(_destIndex, _startCaret.Length);
        source.InsertRange(_startCaret.LeftIndex, content);
        
        return _startCaret;
    }

    public Caret Redo(List<char> source, Caret caret)
    {
        var content = caret.Slice(source);
        
        source.RemoveRange(_startCaret.LeftIndex, _startCaret.Length);
        source.InsertRange(_destIndex, content);
        
        return new Caret(_destIndex, _startCaret.Length);
    }
}