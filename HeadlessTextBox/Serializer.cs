using HeadlessTextBox.Compositing.Contracts;
using HeadlessTextBox.Compositing.Serialization;

namespace HeadlessTextBox;

public static class Serializer
{
    public static (string text, string format) Serialize(SourceBuffer source)
    {
        return (
            TextSerializer.Serialize(source.Text),
            FormatSerializer.SerializeV1(source.Format)
        );
    }
}