using System.Runtime.InteropServices;

namespace HeadlessTextBox.Editing.Recording;


public record struct FormatRecordStack
{
    private int _count;
    private int _next;
    
    private readonly int[] _formatRecordLength;
    private readonly List<FormatBufferRef> _formatBuffer; // BUG: Grows indefinitely


    private int Size => _formatRecordLength.Length;
    
    private int CurrentIndex => Wrap(_next - 1, Size);
    
        
    public FormatRecordStack(int size = 256)
    {
        _count = 0;
        _next = 0;
        
        _formatRecordLength = new int[size];
        _formatBuffer = new List<FormatBufferRef>(size * 3);
    }

    
    public void Add(IReadOnlyList<FormatBufferRef> records)
    {
        foreach (var record in records)
            _formatBuffer.Add(record);
        
        _formatRecordLength[_next] = records.Count;
        MoveNext();
    }
    
    public void Add(ReadOnlySpan<FormatBufferRef> records)
    {
        foreach (var record in records)
            _formatBuffer.Add(record);
        
        _formatRecordLength[_next] = records.Length;
        MoveNext();
    }


    private bool GetCurrentValue(out ReadOnlySpan<FormatBufferRef> value)
    {
        if (_count <= 0)
        {
            value = default;
            return false;
        }
        
        var length = _formatRecordLength[CurrentIndex];
        var start = _formatBuffer.Count - length;
        value = CollectionsMarshal.AsSpan(_formatBuffer)[start..];
        return true;
    }
    
    
    private void MoveNext()
    {
        if (_count < Size)
            _count++;
        
        _next++;
        _next %= Size;
    }
    
    private static int Wrap(int i, int n)
    {
        return (i % n + n) % n;
    }
}