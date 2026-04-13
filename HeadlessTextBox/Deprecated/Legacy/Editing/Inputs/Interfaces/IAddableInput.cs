namespace HeadlessTextBox.Legacy.Editing.Inputs.Interfaces;

public interface IAddableInput: IInput
{
    public void Add(char deleted);

    public void Add(IEnumerable<char> c);
}