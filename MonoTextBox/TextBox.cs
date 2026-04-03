using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using MonoTextBox.Editing;
using MonoTextBox.Rendering;


namespace MonoTextBox;


public class TextBox
{
    protected Rectangle Rectangle;

    protected readonly TextBuffer TextBuffer;
    
    protected readonly List<Vector2> CharPositionBuffer;
    
    protected readonly ScrollBuffer ScrollBuffer = new();
    
    public bool Selected { get; set; } = false;
    
    public Rectangle Area => Rectangle;
    public string? Content => TextBuffer.Buffer.ToString();
    
    
    /// <summary>
    /// UpdateSelection and move text.
    /// </summary>
    private bool _isMovingText = false;
    
    /// <summary>
    /// Called before input is buffered.
    /// </summary>
    public event Action? OnInputReceiving;
    public event Action? OnInputReceived;
    
    /// <summary>
    /// Current typed visible text covered area
    /// </summary>
    public Rectangle TextCoveredArea => GetTextCoveredArea();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="rectangle">
    /// NOTE that the box area is used as is.
    /// It shouldn't contain margin.
    /// </param>
    public TextBox(Rectangle rectangle)
    {
        Rectangle = rectangle;
        
        TextBuffer = new();
        
        CharPositionBuffer = new List<Vector2>(300){ new(rectangle.X, rectangle.Y) };
        
        Selected = false;
    }

    
    public override void draw(SpriteBatch b) => Draw(b);
    private void Draw(SpriteBatch b) 
        => Render.Draw(b, TextBuffer.Caret, TextBuffer.Buffer, CharPositionBuffer);

    
    public override void receiveScrollWheelAction(int direction)
    {
        var distance = ScrollBuffer.GetDistance(direction);
        
        if (distance <= 0)
            return;
        
        if (TextBuffer.Buffer.Count == 0)
            return;
        
        var room = CharPositionBuffer[^1].Y + GlypnPositioner.RenderLineHeight - Rectangle.Bottom;
        if (room <= 0)
            return;
        if (distance > room)
            distance = (int)Math.Ceiling(room);
        
        GlypnPositioner.MoveArea(0, distance, CharPositionBuffer);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (!Rectangle.Contains(x, y))
            return;
        
        Select();
        
        var i = CaretPositioner.CalculatePressedCaretIndex(new Vector2(x, y), TextBuffer, CharPositionBuffer);
        
        if (TextBuffer.Caret.Length == 0
            || !TextBuffer.Caret.CheckInRange(i))
        {
            TextBuffer.CaretUpdate(i);
            return;
        }
        
        _isMovingText = true;
    }
    
    public override void leftClickHeld(int x, int y)
    {
        var destIndex = CaretPositioner.CalculatePressedCaretIndex(new Vector2(x, y), TextBuffer, CharPositionBuffer);
        
        if (!_isMovingText)
        {
            var startIndex = TextBuffer.Caret.StartIndex;
            var length = destIndex - startIndex;
            TextBuffer.CaretUpdate(startIndex, length);
        }
        else
            TextBuffer.StartMovingContent(destIndex);
    }

    public override void releaseLeftClick(int x, int y)
    {
        if (!_isMovingText)
            return;
        
        leftClickHeld(x, y);
        
        ReceiveMovementInput();
        
        TextBuffer.FinishMovingContent();
    }
    
    public virtual void RecieveTextInput(char inputChar)
    {
        OnInputReceiving?.Invoke();
        
        int startCursorPositionOfChange = TextBuffer.Caret.StartIndex;
        TextBuffer.ReceiveCharInput(inputChar);
        GlypnPositioner.UpdatePositionsForChars(startCursorPositionOfChange, Rectangle, TextBuffer, CharPositionBuffer);
        
        OnInputReceived?.Invoke();
    }

    public virtual void RecieveTextInput(string text)
    {
        OnInputReceiving?.Invoke();
        
        var startCursorPositionOfChange = TextBuffer.Caret.StartIndex;
        TextBuffer.ReceiveStringInput(text);
        GlypnPositioner.UpdatePositionsForChars(startCursorPositionOfChange, Rectangle, TextBuffer, CharPositionBuffer);
        
        OnInputReceived?.Invoke();
    }

    public virtual void RecieveCommandInput(char command)
    {
        OnInputReceiving?.Invoke();
        
        TextBuffer.ReceiveCharInput(command);
        // Only \t \r and \b is considered as command input in original code.
        // for \t and \r, text buffer's cursor added one, and now minus one, thus it's the right cursor position to update positions 
        // for \b, text buffer's cursor minus 1. However, due to word/speaker wrapping, we need to subtract one once more. 
        int startCursorPositionOfChange = TextBuffer.Caret.StartIndex - 1;
        GlypnPositioner.UpdatePositionsForChars(startCursorPositionOfChange, Rectangle, TextBuffer, CharPositionBuffer);
        
        OnInputReceived?.Invoke();
    }

    public virtual void RecieveSpecialInput(Keys key)
    {
        switch (key)
        {
            case Keys.Left:
                TextBuffer.CaretMoveLeft();
                break;
            case Keys.Right:
                TextBuffer.CaretMoveRight();
                break;
            case Keys.Up:
                MoveUpOrDown(true);
                break;
            case Keys.Down:
                MoveUpOrDown(false);
                break;
        }

        return;
        void MoveUpOrDown(bool isUp)
        {
            var flag = isUp ? -1 : 1;
            var i = TextBuffer.Caret.FinishIndex;
            var pos = CharPositionBuffer[i];
            var h = GlypnPositioner.RenderLineHeight;
            pos += new Vector2(0, h) * flag;
            if (pos.Y <= Rectangle.Y)
                GlypnPositioner.MoveArea(0, (int)h * flag, CharPositionBuffer);
            receiveLeftClick((int)pos.X, (int)pos.Y);
        }
    }
    
    public virtual void ReceiveMovementInput()
    {
        if (TextBuffer.Caret.OverrideIndex is null)
            return;
        
        OnInputReceiving?.Invoke();
        
        int startCursorPositionOfChange = TextBuffer.Caret.OverrideIndex.Value;
        TextBuffer.ReceiveMovementInput();
        GlypnPositioner.UpdatePositionsForChars(startCursorPositionOfChange, Rectangle, TextBuffer, CharPositionBuffer);
        
        OnInputReceived?.Invoke();
    }
    

    public void Select()
    {
        Selected = true;

        if (Game1.keyboardDispatcher.Subscriber != this)
            Game1.keyboardDispatcher.Subscriber = this;
    }

    public void Deselect()
    {
        Selected = false;

        if (Game1.keyboardDispatcher.Subscriber == this)
            Game1.keyboardDispatcher.Subscriber = null;
    }
    
    
    /// <summary>
    /// Move the whole TextBox.
    /// For scrolling only the text content, see <see cref="TextBox.receiveScrollWheelAction"/>.
    /// </summary>
    /// <param name="offsetX"></param>
    /// <param name="offsetY"></param>
    public void Offset(int offsetX, int offsetY)
    {
        if (offsetX == 0 && offsetY == 0)
            return;
        
        // Move the text box area's position
        Rectangle.X += offsetX;
        Rectangle.Y += offsetY;
        
        // Move each char's positions
        GlypnPositioner.MoveArea(offsetX, offsetY, CharPositionBuffer);
    }

    /// <summary>
    /// DISCOURAGED as this recalculates all characters' positions.
    /// </summary>
    /// <param name="rect"></param>
    public void Reshape(Rectangle rect)
    {
        Rectangle = rect;
        GlypnPositioner.UpdateArea(rect, TextBuffer, CharPositionBuffer);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="widthDelta">Only if it's blank or a one-liner, else error will be thrown</param>
    /// <param name="heightDelta">Expand height is always safe.</param>
    public void Resize(int widthDelta = 0, int heightDelta = 0)
    {
        var isOneLiner = Math.Abs(CharPositionBuffer[0].Y - CharPositionBuffer[^1].Y) < 0.01f;
        if (widthDelta != 0 && !isOneLiner)
            throw new InvalidOperationException("Not allowed to expand text box area when it's not a one-liner.");
        
        Rectangle.Width += widthDelta;
        Rectangle.Height += heightDelta;
    }

    
    private Rectangle GetTextCoveredArea()
    {
        if (CharPositionBuffer.Count == 0)
            return Rectangle.Empty;
        
        if (Math.Abs(CharPositionBuffer[^1].Y - CharPositionBuffer[0].Y) < 0.1f)
            return new Rectangle(
                (int)CharPositionBuffer[0].X, 
                (int)CharPositionBuffer[0].Y, 
                (int)(CharPositionBuffer[^1].X + GlypnPositioner.CalculateCharWidth(TextBuffer.Buffer[^1]) - CharPositionBuffer[0].X), 
                (int)GlypnPositioner.FontHeight);

        return new Rectangle(
            (int)CharPositionBuffer[0].X, 
            (int)CharPositionBuffer[0].Y,
            (int)(Rectangle.Right - CharPositionBuffer[0].X),
            (int)Math.Min(Rectangle.Height, CharPositionBuffer[^1].Y + GlypnPositioner.FontHeight));
    }
}