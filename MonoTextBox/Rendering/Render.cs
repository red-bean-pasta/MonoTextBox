using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoTextBox.Editing;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;


namespace MonoTextBox.Rendering;


public static class Render
{
    private static bool IsUsingLatinLanguage => SpriteText.characterMap.Count == 0;

    private static readonly Texture2D HelperPixel = Game1.staminaRect;

    private const int CaretWidth = 3;

    private static readonly Color CaretColor =
        SpriteText.color_Default == Color.White ? new Color(86, 22, 12) : SpriteText.color_Default;

    private static readonly Color SelectionColor = Color.ForestGreen * 0.6f;
    
    public static void Draw(
        SpriteBatch b,
        Caret caret,
        IEnumerable<char> characters,
        IEnumerable<Vector2> positions)
    {
        var i = 0;
        var selection = Rectangle.Empty;
        using var chrEnumerator = characters.GetEnumerator();
        using var posEnumerator = positions.GetEnumerator();
        while (true)
        {
            var currentPosition = posEnumerator.Current;
            var currentChar = chrEnumerator.Current;
            
            if (IsUsingLatinLanguage)
                DrawFromDefaultTexture(b, currentChar, currentPosition);
            else
                DrawFromFontTexture(b, currentChar, currentPosition);

            var hasNext = posEnumerator.MoveNext() && chrEnumerator.MoveNext();
            ExpandIfSelected(currentChar, currentPosition, hasNext, posEnumerator.Current);

            if (i == caret.StartIndex)
                DrawIfAtCaret(currentPosition);
            
            if (!hasNext) break;

            i++;
        }
        if (selection != Rectangle.Empty)
            DrawSelectionBackground(b, selection);
        
        return;
        void ExpandIfSelected(
            char currentChar,
            Vector2 currentPosition,
            bool hasNext,
            Vector2 nextPosition)
        {
            if (caret.Length == 0 || !caret.CheckInRange(i)) 
                return;
            
            var isNextLine = hasNext && Math.Abs(nextPosition.Y - currentPosition.Y) > 0.01f;
            var deltaWidth = hasNext || isNextLine
                ? (nextPosition - currentPosition).X
                : GlypnPositioner.CalculateCharWidth(currentChar);

            if (selection == Rectangle.Empty)
                selection = new Rectangle((int)currentPosition.X, (int)currentPosition.Y, (int)deltaWidth, (int)GlypnPositioner.RenderLineHeight);
            else if (deltaWidth != 0)
                selection.Width += (int)deltaWidth;

            if (!isNextLine) return;
            
            DrawSelectionBackground(b, selection);
            selection = Rectangle.Empty;
        }

        void DrawIfAtCaret(Vector2 currentPosition)
        {
            var x = currentPosition.X - CaretWidth;
            var y = currentPosition.Y - (GlypnPositioner.RenderLineHeight - GlypnPositioner.FontHeight) / 2;
            var rect = new Rectangle((int)x, (int)y, CaretWidth, (int)GlypnPositioner.RenderLineHeight);
            DrawCaret(b, rect);
        }
    }

    private static void DrawFromDefaultTexture(SpriteBatch b, char character, Vector2 position)
    {
        if (char.IsControl(character))
            return;

        var sourceArea = GetSourceAreaForLatinChar(character);

        if (sourceArea.Y > SpriteText.spriteTexture.Height)
            sourceArea = GetSourceAreaForLatinChar('?');

        DrawChar(b, position, sourceArea);
    }

    private static void DrawFromFontTexture(SpriteBatch b, char character, Vector2 position)
    {
        if (char.IsControl(character))
            return;

        SpriteText.characterMap.TryGetValue(character, out var fontChar);

        if (fontChar == null)
            SpriteText.characterMap.TryGetValue('?', out fontChar);

        if (fontChar == null)
        {
            DrawFromDefaultTexture(b, '?', position);
            return;
        }

        Rectangle sourceArea = new(fontChar.X, fontChar.Y, fontChar.Width, fontChar.Height);

        position.X += fontChar.XOffset * SpriteText.FontPixelZoom;
        position.Y += fontChar.YOffset * SpriteText.FontPixelZoom;

        Texture2D fontPage = SpriteText.fontPages[fontChar.Page];

        DrawChar(b, position, sourceArea, fontPage);
    }

    private static void DrawCaret(SpriteBatch b, Rectangle caretRectangle)
    {
        var ifDraw = Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 1000 >= 500;
        if (!ifDraw)
            return;

        b.Draw(HelperPixel, caretRectangle, CaretColor);
    }

    private static void DrawSelectionBackground(SpriteBatch b, Rectangle selection)
        => b.Draw(HelperPixel, selection, SelectionColor);

    private static void DrawChar(
        SpriteBatch b,
        Vector2 position,
        Rectangle sourceArea,
        Texture2D? sourceTexture = null,
        float? fontPixelSize = null,
        Color? color = null,
        float? layerDepth = null)
    {
        sourceTexture ??= color is not null
            ? SpriteText.coloredTexture
            : SpriteText.spriteTexture;

        fontPixelSize ??= SpriteText.FontPixelZoom;
        color ??= SpriteText.color_Default;
        layerDepth ??= 0.88f;

        b.Draw(sourceTexture, position, sourceArea, color.Value, 0.0f, Vector2.Zero, fontPixelSize.Value,
            SpriteEffects.None, layerDepth.Value);
    }

    /// <summary>
    /// Copy of private method SpriteText.getSourceRectForChar(). 
    /// Apply to Latin language and none-alternative Russian. 
    /// </summary>
    /// <param name="character"></param>
    /// <returns></returns>
    public static Rectangle GetSourceAreaForLatinChar(char character)
    {
        int num = character - 32;
        switch (character)
        {
            case 'Ą':
                num = 576;
                break;
            case 'ą':
                num = 578;
                break;
            case 'Ć':
                num = 579;
                break;
            case 'ć':
                num = 580;
                break;
            case 'Ę':
                num = 581;
                break;
            case 'ę':
                num = 582;
                break;
            case 'Ğ':
                num = 102;
                break;
            case 'ğ':
                num = 103;
                break;
            case 'İ':
                num = 98;
                break;
            case 'ı':
                num = 99;
                break;
            case 'Ł':
                num = 583;
                break;
            case 'ł':
                num = 584;
                break;
            case 'Ń':
                num = 585;
                break;
            case 'ń':
                num = 586;
                break;
            case 'Ő':
                num = 105;
                break;
            case 'ő':
                num = 106;
                break;
            case 'Œ':
                num = 96;
                break;
            case 'œ':
                num = 97;
                break;
            case 'Ś':
                num = 574;
                break;
            case 'ś':
                num = 575;
                break;
            case 'Ş':
                num = 100;
                break;
            case 'ş':
                num = 101;
                break;
            case 'Ű':
                num = 107;
                break;
            case 'ű':
                num = 108;
                break;
            case 'Ź':
                num = 587;
                break;
            case 'ź':
                num = 588;
                break;
            case 'Ż':
                num = 589;
                break;
            case 'ż':
                num = 590;
                break;
            case 'Ё':
                num = 512;
                break;
            case 'Є':
                num = 514;
                break;
            case 'І':
                num = 515;
                break;
            case 'Ї':
                num = 516;
                break;
            case 'Ў':
                num = 517;
                break;
            case 'ё':
                num = 560;
                break;
            case 'є':
                num = 562;
                break;
            case 'і':
                num = 563;
                break;
            case 'ї':
                num = 564;
                break;
            case 'ў':
                num = 565;
                break;
            case 'Ґ':
                num = 513;
                break;
            case 'ґ':
                num = 561;
                break;
            case '–':
                num = 464;
                break;
            case '—':
                num = 465;
                break;
            case '’':
                num = 104;
                break;
            case '№':
                num = 466;
                break;
            default:
                if (num is >= 1008 and < 1040)
                {
                    num -= 528;
                    break;
                }

                if (num is >= 1040 and < 1072)
                {
                    num -= 512;
                    break;
                }

                break;
        }

        return new Rectangle(num * 8 % SpriteText.spriteTexture.Width, num * 8 / SpriteText.spriteTexture.Width * 16, 8,
            16);
    }
}