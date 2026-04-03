using Microsoft.Xna.Framework;
using MonoTextBox.Editing;

namespace MonoTextBox.Rendering;

public static class GlypnPositioner
{
    private static float Zoom => SpriteText.FontPixelZoom;
    
    /// <summary>
    /// Unicode index where characters following don't use blank space as word boundary, e.g. Chinese, Japanese and Korean
    /// At least that's what I hope.
    /// </summary>
    private const int NoWordWrapUnicodeIndex = 0x2150;
    
    private const float UnscaledLatinFontHeight = 18f;
    public static float LatinFontHeight => UnscaledLatinFontHeight * Zoom;
    
    public const float UnscaledBlankSpaceWidth = 8f;
    public static float BlankSpaceWidth => UnscaledBlankSpaceWidth * Zoom;
    
    public static float FontHeight
        => SpriteText.characterMap.Count == 0
            ? SpriteText.FontFile.Common.LineHeight * SpriteText.FontPixelZoom
            : LatinFontHeight;
    
    public static float RenderLineHeight
        => SpriteText.characterMap.Count == 0
            ? (SpriteText.FontFile.Common.LineHeight + 2) * SpriteText.FontPixelZoom
            : LatinFontHeight;
    
    /// <summary>
    /// To recalculate the positions for all chars if TextBox get RESIZED,
    /// which is computationally consuming.
    /// If only moving TextBox with no resizing, use MoveArea().
    /// </summary>
    /// <param name="newArea"></param>
    /// <param name="textBuffer"></param>
    /// <param name="charPositions"></param>
    public static void UpdateArea(Rectangle newArea, TextBuffer textBuffer, List<Vector2> charPositions) 
        => UpdatePositionsForChars(0, newArea, textBuffer, charPositions);

    public static void MoveArea(int offsetX, int offsetY, List<Vector2> charPositions)
    {
        for (int i = 0; i < charPositions.Count; i++)
            charPositions[i] = new(charPositions[i].X + offsetX, charPositions[i].Y + offsetY);
    }
    
    public static void UpdatePositionsForChars(
        int caretIndexStartChange,
        Rectangle boxArea, 
        TextBuffer textBuffer, 
        List<Vector2> positions)
    {
        // Remove positions that exceed current total character count
        // e.g., after a text deletion
        if (positions.Count - textBuffer.Buffer.Count > 0)
            positions.RemoveRange(textBuffer.Buffer.Count-1, positions.Count - textBuffer.Buffer.Count);
      
        Vector2 currentPosition = positions[caretIndexStartChange];
      
        for (int i = caretIndexStartChange; i < textBuffer.Buffer.Count; i++)
        {
            var c = textBuffer.Buffer[i];

            if (c == '\n')
            {
                positions[i] = currentPosition;
                currentPosition = UpdatePositionAfterNewLine(currentPosition, boxArea);
            }
            else if (c == ' ')
            {
                positions[i] = currentPosition;
                currentPosition = UpdatePositionAfterBlankSpace(currentPosition, boxArea);
            }
            else if (c == '\t')
            {
                positions[i] = currentPosition;
                currentPosition = UpdatePositionAfterTab(currentPosition, boxArea);
            }
            else if (c < NoWordWrapUnicodeIndex)
            {
                currentPosition = UpdatePositionsWithWordWrapping(currentPosition, boxArea, textBuffer, ref i, ref positions);
            }
            else
            {
                positions[i] = currentPosition;
                currentPosition = UpdatePositionsWithoutWordWrapping(currentPosition, boxArea, c);
            }
        }
    }
    
    private static Vector2 UpdatePositionAfterNewLine(Vector2 currentPosition, Rectangle boxArea) 
        => new(boxArea.X, currentPosition.Y + RenderLineHeight);

    private static Vector2 UpdatePositionAfterBlankSpace(Vector2 currentPosition, Rectangle boxArea)
    {
        currentPosition.X += BlankSpaceWidth;

        if (currentPosition.X >= boxArea.Right)
            currentPosition = UpdatePositionAfterNewLine(currentPosition, boxArea);
        
        return currentPosition;
    }

    private static Vector2 UpdatePositionAfterTab(Vector2 currentPosition, Rectangle boxArea)
    {
      currentPosition.X += BlankSpaceWidth * 4;
      
      if (currentPosition.X >= boxArea.X + boxArea.Width)
          currentPosition = UpdatePositionAfterNewLine(currentPosition, boxArea);
      
      return currentPosition;
    }

    /// <summary>
    /// For Latin and Cyrillic language, which requires word wrapping,
    /// e.g., when the left line space is not fit enough for the whole word
    /// </summary>
    /// <param name="currentPosition"></param>
    /// <param name="boxArea"></param>
    /// <param name="textBuffer"></param>
    /// <param name="i"></param>
    /// <param name="positions"></param>
    /// <returns></returns>
    private static Vector2 UpdatePositionsWithWordWrapping(
      Vector2 currentPosition, 
      Rectangle boxArea, 
      TextBuffer textBuffer, 
      ref int i, 
      ref List<Vector2> positions)
    {
      int startPosition = i;
      float wordWidth = 0;
      while (true)
      {
        wordWidth += CalculateCharWidth(textBuffer.Buffer[i]);
        i++;

        char nextChar = textBuffer.Buffer[i];
        // Break if met whitespace or hyphen or if met the speaker like Chinese or Japanese
        if (nextChar is '\n' or '\t' or ' ' or '-'
            || nextChar >= NoWordWrapUnicodeIndex)
            break;
        
        // Break if this word is longer than the whole box width(edge case)
        if (wordWidth >= boxArea.Width)
        {
          i--;
          break;
        }
      }

      if (currentPosition.X + wordWidth > boxArea.X + boxArea.Width)
      {
        currentPosition = UpdatePositionAfterNewLine(currentPosition, boxArea);
      }

      for (int j = startPosition; j < i; j++)
      {
        positions[j] = currentPosition;
        currentPosition.X += CalculateCharWidth(textBuffer.Buffer[j]);
      }

      return currentPosition;
    }

    /// <summary>
    /// For language like Chinese, Japanese, Korean and such, that doesn't require word wrapping.
    /// </summary>
    /// <param name="currentPosition"></param>
    /// <param name="boxArea"></param>
    /// <param name="character"></param>
    /// <returns> Returns both positions to actually render the current speaker,
    /// as it may have offset;
    /// and position for next speaker </returns>
    private static Vector2 UpdatePositionsWithoutWordWrapping(Vector2 currentPosition, Rectangle boxArea, char character)
    {
      float charWidth = CalculateCharWidth(character);
      
      if (currentPosition.X + charWidth > boxArea.X + boxArea.Width)
        currentPosition = UpdatePositionAfterNewLine(currentPosition, boxArea);
      
      Vector2 nextPosition = new(
        currentPosition.X + charWidth,
        currentPosition.Y
      );
      
      return nextPosition;
    }

    public static float GetTextWidth(string text) 
        => text.Sum(CalculateCharWidth);

    public static float CalculateCharWidth(char c)
    {
        if (SpriteText.characterMap.Count == 0)
            return BlankSpaceWidth + SpriteText.getWidthOffsetForChar(c) * Zoom * 2;
      
        SpriteText.characterMap.TryGetValue(c, out var fontChar);

        switch (c)
        {
            case ' ':
                return BlankSpaceWidth;
            case '\n':
                return 0;
            case '\t':
                return BlankSpaceWidth * 4;
        }
        
        if (char.IsControl(c) || char.IsWhiteSpace(c)) // like '\r', '\0' or '\f'
            return 0;
        
        if (fontChar == null)
            SpriteText.characterMap.TryGetValue('?', out fontChar);
        
        if (fontChar != null)
            return fontChar.XAdvance * SpriteText.FontPixelZoom;
      
        return BlankSpaceWidth + SpriteText.getWidthOffsetForChar('?') * SpriteText.FontPixelZoom * 2;
    }
}