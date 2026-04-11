namespace HeadlessTextBox.Formatting;

public interface IFormat: IEquatable<IFormat>
{
    public int Font { get; set; }
}