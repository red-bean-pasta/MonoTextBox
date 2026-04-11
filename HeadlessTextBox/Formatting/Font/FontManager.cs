namespace HeadlessTextBox.Formatting.Font;

public static class FontManager
{
    public static Dictionary<int, IFontMeasurable> Fonts { get; } = new();
    
    public static IFontMeasurable GetFont(int id) => Fonts[id];
    
    public static void RegisterFont(int id, IFontMeasurable font) => Fonts[id] = font;
}