namespace HeadlessTextBox.Legacy.Editing.Inputs.Interfaces;

public interface IAddableInput: IUndoRedoInput
{
    public void Add(char deleted);

    public void Add(IEnumerable<char> c);
}