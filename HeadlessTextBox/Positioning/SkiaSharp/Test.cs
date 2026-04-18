using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace HeadlessTextBox.Positioning.SkiaSharp;

public class Test
{

// 1. Initialize the Font Manager and Paragraph Style
using var fontCollection = new TypefaceFontProvider();
// Register a font (e.g., Arial or a specific TTF file)
fontCollection.RegisterTypeface(SKTypeface.FromFamilyName("Arial"));

var fontManager = new SKFontManager(); // Can use system fonts
using var fontCollectionManaged = new FontCollection();
fontCollectionManaged.SetDefaultFontManager(fontManager);

// 2. Define the Paragraph Style (Alignment, Max Lines, etc.)
var paragraphStyle = new ParagraphStyle
{
    TextStyle = new TextStyle
    {
        Color = SKColors.Black,
        FontSize = 24,
        Placeholder = false
    },
    TextAlign = TextAlign.Left
};

// 3. The Paragraph Builder (This is where the "Rich Text" magic happens)
using var builder = new ParagraphBuilder(paragraphStyle, fontCollectionManaged);

// Add some plain text
builder.AddText("This is standard text. ");

// Add some Styled/Rich text
builder.PushStyle(new TextStyle { Color = SKColors.Crimson, FontWeight = SKFontStyleWeight.Bold });
builder.AddText("This is bold crimson text. ");
builder.Pop();

// Add an Emoji (HarfBuzz + SkParagraph handles the surrogate pairs!)
builder.AddText("Check out this emoji: 🚀");

// 4. Layout (The "Pango-like" part)
using var paragraph = builder.Build();
// Set the width boundary for word wrapping (e.g., 400 pixels)
paragraph.Layout(400);

// 5. Paint it to a SkiaSharp Canvas
using var bitmap = new SKBitmap(500, 500);
using var canvas = new SKCanvas(bitmap);
canvas.Clear(SKColors.White);

// Draw the paragraph at coordinates (20, 20)
paragraph.Paint(canvas, 20, 20);

}