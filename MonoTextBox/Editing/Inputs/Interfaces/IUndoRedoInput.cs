namespace MonoTextBox.Editing.Inputs.Interfaces;

public interface IUndoRedoInput
{
    public Caret Undo(List<char> source, Caret caret);

    public Caret Redo(List<char> source, Caret caret);
}