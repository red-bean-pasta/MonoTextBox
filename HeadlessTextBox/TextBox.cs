using HeadlessTextBox.Formatting;
using Icu;

namespace HeadlessTextBox;

// A thin wrapper around TextManager for more convenient consuming
// by handling position and scroll offset
public class TextBox
{
    public float X { get; private set; }
    public float Y { get; private set; }
    public float Height { get; private set; }

    public float Scrolled { get; private set; }
    
    public TextManager Buffer { get; }
    
    
    public float Width => Buffer.Width;
    public Locale Locale => Buffer.Locale;


    private TextBox(
        float x,
        float y,
        float height,
        TextManager textManager)
    {
        X = x;
        Y = y;
        Height = height;
        Buffer = textManager;
    }
    
    public TextBox(
        float x,
        float y,
        float width,
        float height,
        int undoStackSize = 256,
        Locale? locale = null
    ) : this(x, y, height, new TextManager(width, undoStackSize, locale))
    { }

    public TextBox(
        float x,
        float y,
        float width,
        float height,
        string text,
        FormatTree format,
        int undoStackSize = 256,
        Locale? locale = null
    ) : this(x, y, height, new TextManager(text, format, width, undoStackSize, locale))
    { }

    public static TextBox Build<T>(
        float x,
        float y,
        float width,
        float height,
        string text,
        string format,
        int undoStackSize = 256,
        Locale? locale = null
    ) where T : IFormat
    {
        var manager = TextManager.Build<T>(text, format, width, undoStackSize, locale);
        return new TextBox(x, y, height, manager);
    }
    
    
    public TextBoxContentEnumerator EnumerateInScope() 
        => new(X, Y, Height, Scrolled, Buffer);


    public void ChangeSize(float newWidth, float newHeight)
    {
        Buffer.Resize(newWidth);
        Height = newHeight;
    }

    public void ChangePosition(float x, float y)
    {
        X = x;
        Y = y;
    }

    public void Scroll(float offset)
    {
        offset = Math.Clamp(offset, 0 - Scrolled, Buffer.Height - Scrolled - Height);
        Scrolled += offset;
    }
}


public ref struct TextBoxContentEnumerator
{
    private readonly float _x;
    private readonly float _y;
    private readonly float _scrolled;

    private TextElementEnumerator _elementEnumerator;


    public VisualChar Current => GetCurrentValue();
    

    public TextBoxContentEnumerator(
        float x,
        float y,
        float height,
        float scrolled,
        TextManager textManager)
    {
        _x = x;
        _y = y;
        _scrolled = scrolled;
        _elementEnumerator = textManager.EnumerateInScopeElements(scrolled, height);
    }


    public TextBoxContentEnumerator GetEnumerator() => this;
    
        
    public bool MoveNext() => _elementEnumerator.MoveNext();


    private VisualChar GetCurrentValue()
    {
        var raw = _elementEnumerator.Current;
        return raw with
        {
            X = raw.X + _x,
            Y = raw.Y + _y + _scrolled
        };
    }
}
