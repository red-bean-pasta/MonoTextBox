using HeadlessTextBox.Utils;
using HeadlessTextBox.Utils.Extensions;

namespace HeadlessTextBox.Storage;

public class AddBuffer
{
    private readonly int _chunkSize;
    
    private readonly List<char[]> _chunks = new();
    private readonly List<int> _sums = new();
    
    private int _currentChunkIndex = -1;
    private int _currentChunkPosition = 0;
    
    
    public AddBuffer(int chunkSize = 1024 * 64) 
    {
        _chunkSize = chunkSize;
        AddNewChunk();
    }


    public ReadOnlySpan<char> GetSpan(int start, int length) => GetMemory(start, length).Span;

    public ReadOnlyMemory<char> GetMemory(int start, int length)
    {
        var chunkIndex = FindChunk(start);
        var relativeStart = start - _sums[chunkIndex];
        var memory = _chunks[chunkIndex].AsMemory(relativeStart, length);
        return memory;
    }
    
    
    public (int Start, int Length) Append(ReadOnlySpan<char> text) {
        if (_currentChunkPosition + text.Length > _chunkSize) 
            AddNewChunk();

        var start = _currentChunkPosition;
        var length = text.Length;
        
        text.CopyTo(_chunks[_currentChunkIndex].AsSpan(start));
        _currentChunkPosition += text.Length;

        return (start, length);
    }
    
    private void AddNewChunk() {
        _chunks.Add(new char[_chunkSize]);
        _currentChunkIndex++;
        _currentChunkPosition = 0;
    }


    private int FindChunk(int start) => _sums.FindFirstGreater(start);
}