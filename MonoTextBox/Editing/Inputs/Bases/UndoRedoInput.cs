using MonoTextBox.Editing.Inputs.Interfaces;

namespace MonoTextBox.Editing.Inputs.Bases;

public abstract class UndoRedoInput: IUndoRedoInput
{
    private int Anchor { get; }

    protected List<char> Content { get; }

    protected List<char> Replaced { get; }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="anchorIndex">
    /// The caret index where deletion and insertion is anchored, 
    /// usually the <see cref="Caret.LeftIndex"/>
    /// </param>
    /// <param name="content"></param>
    /// <param name="replaced"></param>
    protected UndoRedoInput(
        int anchorIndex,
        IEnumerable<char>? content = null,
        IEnumerable<char>? replaced = null)
    {
        Anchor = anchorIndex;
        Content = Helper.EnumerateToList(content);
        Replaced = Helper.EnumerateToList(replaced);
    }
    
    
    public Caret Undo(
        List<char> source, 
        Caret caret)
    {
        source.RemoveRange(Anchor, Content.Count);
        source.InsertRange(Anchor, Replaced);
        
        return new Caret(Anchor, Replaced.Count);
    }

    
    public Caret Redo(
        List<char> source, 
        Caret caret)
    {
        source.RemoveRange(Anchor, Replaced.Count);
        source.InsertRange(Anchor, Content);
        
        return new Caret(Anchor, Content.Count);
    }
}