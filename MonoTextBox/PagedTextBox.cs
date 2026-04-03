using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoTextBox.Rendering;

namespace MonoTextBox;

public class PagedTextBox: TextBox
{
    private readonly Dictionary<int, List<Vector2>> _pageCharPositions = new();

    private Rectangle PageArea => base.Rectangle;
    
    private int PageCount => (int)Math.Ceiling(CharPositionBuffer[^1].Y / PageArea.Height);

    /// <summary>
    /// 0 based.
    /// </summary>
    public int CurrentPage { get; private set; } = 0;
    private int InnerCurrentPageTop => CurrentPage * PageArea.Height;
    private int InnerCurrentPageBottom => (CurrentPage + 1) * PageArea.Height;
    
    public PagedTextBox(Rectangle pageArea, string? initialText = null) 
        : base(pageArea)
    { }
    
    public override void draw(SpriteBatch b)
    {
        if (TextBuffer.Buffer.Count <= 0)
            return;

        if (CharPositionBuffer[^1].Y < PageArea.Bottom)
        {
            base.draw(b); 
            return;
        }
        
        var (start, end) = FindIndexOfPage(CurrentPage);
        var delta = new Vector2(0, PageArea.Height * CurrentPage);
        var chars = TextBuffer.Buffer
            .Skip(start)
            .Take(end - start + 1);
        var pos = CharPositionBuffer
            .Skip(start)
            .Take(end - start + 1)
            .Select(v => v - delta);

        Render.Draw(b, TextBuffer.Caret, chars, pos);
    }

    public override void receiveScrollWheelAction(int direction)
    {
        var distance = ScrollBuffer.GetDistance(direction);
        
        if (distance <= 0)
            return;

        CurrentPage = Math.Min(PageCount, CurrentPage++);
    }
    
    public override void RecieveTextInput(char inputChar)
    {
        base.RecieveTextInput(inputChar);
        FlipPageOnInput();
    }

    public override void RecieveTextInput(string text)
    {
        base.RecieveTextInput(text);
        FlipPageOnInput();
    }

    public override void RecieveCommandInput(char command)
    {
        base.RecieveCommandInput(command);
        FlipPageOnInput();
    }

    public override void RecieveSpecialInput(Keys key)
    {
        base.RecieveSpecialInput(key);
        FlipPageOnInput();
    }
    
    private void FlipPageOnInput()
    {
        var i = TextBuffer.Caret.FinishIndex;
        var y = CharPositionBuffer[i].Y;
        if (y >= InnerCurrentPageBottom)
            CurrentPage++;
        else if (y < InnerCurrentPageTop)
            CurrentPage--;
    }
    
    private (int Start, int End) FindIndexOfPage(int page)
    {
        var top = page * PageArea.Height;
        var bottom = (page + 1) * PageArea.Height;
        var start = FindFirstIndexAfterY(top);
        var end = FindFirstIndexAfterY(bottom) - 1;
        return (start, end);
    }

    private int FindFirstIndexAfterY(int y, int startBound = 0, int endBound = int.MaxValue)
    {
        var i = startBound;
        var j = Math.Min(endBound, CharPositionBuffer.Count - 1);
        
        if (CharPositionBuffer[i].Y > y)
            return i;
        if (CharPositionBuffer[j].Y < y)
            return -1;
        
        while (i <= j)
        {
            var m = (i + j) / 2;
            
            if (CharPositionBuffer[i].Y < y)
                i = m + 1;
            else
                j = m - 1;
        }

        return i;
    }
}