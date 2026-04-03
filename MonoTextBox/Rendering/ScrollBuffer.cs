namespace MonoTextBox.Rendering;

public class ScrollBuffer
{
    private const int FLUSH_INTERVAL = 300; // in millisecond
    /// <summary>
    /// An arbitrary unit-less number of mouse input.
    /// Standardized to 120 on Windows.
    /// On trackpad or gaming mouse, it may be smaller, e.g. 30.
    /// </summary>
    private const int SCROLL_TICK_STEP = 120;
    /// <summary>
    /// How many pixels to scroll per tick.
    /// </summary>
    private const int DISTANCE_PER_TICK = 100;

    private int _scrollRemainder = 0;
    private int _lastScrollTime = 0;
    
    public int GetDistance(int scrollDelta)
    {
        int currentTime = Game1.currentGameTime.TotalGameTime.Milliseconds;
        if (currentTime - _lastScrollTime > FLUSH_INTERVAL)
            _scrollRemainder = 0;
        _lastScrollTime = currentTime;
        
        var scrollTotal = _scrollRemainder + scrollDelta;
        if (Math.Abs(scrollTotal) < SCROLL_TICK_STEP)
        {
            _scrollRemainder = scrollTotal;
            return 0;
        }
        
        var ticks = scrollDelta / (float)SCROLL_TICK_STEP;
        var distance = (int)ticks * DISTANCE_PER_TICK;
        return distance;
    }
    
}