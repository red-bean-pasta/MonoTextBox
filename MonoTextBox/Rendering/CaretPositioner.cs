using Microsoft.Xna.Framework;
using MonoTextBox.Editing;

namespace MonoTextBox.Rendering;

public static class CaretPositioner
{
    public static int CalculatePressedCaretIndex(
      Vector2 pressed, 
      TextBuffer textBuffer, 
      IReadOnlyList<Vector2> charPositions)
    {
      if (textBuffer.Buffer.Count == 0)
        return 0;
      
      if (pressed.Y >= charPositions[^1].Y + GlypnPositioner.FontHeight
          || (pressed.Y >= charPositions[^1].Y 
              && pressed.X > charPositions[^1].X + GlypnPositioner.CalculateCharWidth(textBuffer.Buffer[^1]) / 2))
        return textBuffer.Buffer.Count;
      
      // Click on anywhere before input text
      if (pressed.Y <= charPositions[0].Y
          || (pressed.Y <= charPositions[0].Y + GlypnPositioner.FontHeight
              && pressed.X <= charPositions[0].X))
        return 0;
      
      int lo = 0;
      int hi = textBuffer.Buffer.Count - 1;
      while (lo <= hi)
      {
        int m = (lo + hi) / 2;

        if (pressed.Y > charPositions[m].Y + GlypnPositioner.FontHeight)
          lo = m + 1;
        else if (pressed.Y < charPositions[m].Y)
          hi = m - 1;
        else
        {
          var c = textBuffer.Buffer[m];
          var w = GlypnPositioner.CalculateCharWidth(c);
          
          if (pressed.X < charPositions[m].X) 
            hi = m - 1;
          else if (pressed.X > charPositions[m].X + w)
            lo = m + 1;
          else
            return pressed.X <= charPositions[m].X + 0.5f * w 
              ? m
              : m + 1;
        }
      }
      
      // There are edge cases where use clicked on blank area,
      // due to float rounding to int
      return Math.Clamp(lo, 0, textBuffer.Buffer.Count);
    }
}