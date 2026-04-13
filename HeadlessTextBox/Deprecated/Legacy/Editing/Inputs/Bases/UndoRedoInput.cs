using HeadlessTextBox.Legacy.Editing.Inputs.Interfaces;
using HeadlessTextBox.Utils;
using HeadlessTextBox.Utils.Extensions;

namespace HeadlessTextBox.Legacy.Editing.Inputs.Bases;

public abstract class UndoRedoInput: IInput
{
    private int Anchor { get; }

    protected List<char> Content { get; }

    protected List<char> Replaced { get; }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="start">
    /// The caret index where deletion and insertion is anchored, 
    /// usually the <see cref="Caret.LeftIndex"/>
    /// </param>
    /// <param name="content"></param>
    /// <param name="replaced"></param>
    protected UndoRedoInput(
        int start,
        IEnumerable<char>? content = null,
        IEnumerable<char>? replaced = null)
    {
        Anchor = start;
        Content = EnumerableExtensions.EnumerateToList(content);
        Replaced = EnumerableExtensions.EnumerateToList(replaced);
    }
    
    
    public Caret Undo(Caret caret, List<char> source)
    {
        source.RemoveRange(Anchor, Content.Count);
        source.InsertRange(Anchor, Replaced);
        
        return new Caret(Anchor, Replaced.Count);
    }

    
    public Caret Redo(Caret caret, List<char> source)
    {
        source.RemoveRange(Anchor, Replaced.Count);
        source.InsertRange(Anchor, Content);
        
        return new Caret(Anchor, Content.Count);
    }
}