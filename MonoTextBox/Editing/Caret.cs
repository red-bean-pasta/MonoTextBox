namespace MonoTextBox.Editing;

public struct Caret
{
    /// <summary>
    /// Index where click started
    /// </summary>
    public int StartIndex { get; set; } = 0;
    /// <summary>
    /// Positive when select left to right; 
    /// Negative when select from right to left
    /// </summary>
    public int Selection { get; set; } = 0;
    public int FinishIndex => StartIndex + Selection;
    
    
    public int LeftIndex => Selection >= 0 ? StartIndex : StartIndex + Selection;
    public int RightIndex => LeftIndex + Length;
    public int Length => Math.Abs(Selection);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="startIndex"></param>
    /// <param name="selection">
    /// Can be negative to indicate right to left selection
    /// </param>
    public Caret(int startIndex, int selection)
    {
        StartIndex = startIndex;
        Selection = selection;
    }

    
    public bool CheckInRange(int index) 
        => LeftIndex <= index && index <= LeftIndex + Length;
    
    
    public List<char> Slice(List<char> source)
        => source.GetRange(LeftIndex, Length);
}