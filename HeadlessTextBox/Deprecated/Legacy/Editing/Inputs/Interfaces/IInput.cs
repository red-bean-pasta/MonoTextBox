namespace HeadlessTextBox.Legacy.Editing.Inputs.Interfaces;

public interface IInput
{
    public Caret Undo(Caret caret, List<char> source);

    public Caret Redo(Caret caret, List<char> source);
}