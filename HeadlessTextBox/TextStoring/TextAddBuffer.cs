using HeadlessTextBox.Storage;

namespace HeadlessTextBox.TextStoring;

public class TextAddBuffer : AddBuffer<char>
{
    public TextAddBuffer(int chunkSize = DefaultSize) : base(chunkSize) 
    { }

    public (int Start, int Length) AppendText(ReadOnlySpan<char> text)
    {
        return Append(text);
    }
}