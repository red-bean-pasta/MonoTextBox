using HeadlessTextBox.Storage;

namespace HeadlessTextBox.TextStoring;

public class TextAddBuffer : AddBuffer<char>
{
    public TextAddBuffer(int chunkSize = 64 * 1024) : base(chunkSize) 
    { }
}