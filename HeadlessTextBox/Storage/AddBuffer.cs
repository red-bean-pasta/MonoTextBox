using HeadlessTextBox.Utils.Extensions;

namespace HeadlessTextBox.Storage;

public class AddBuffer<T>
{
    protected readonly int ChunkSize;
    
    protected readonly List<T[]> Chunks = new();
    protected readonly List<int> ChunkSums = new();
    
    protected int InChunkNextPosition = 0;


    protected T[] LastChunk => Chunks[^1];
    protected int LastChunkSum
    {
        get => ChunkSums[^1];
        set => ChunkSums[^1] = value;
    }
    protected T LastItem
    {
        get => Chunks[^1][InChunkNextPosition - 1];
        set => Chunks[^1][InChunkNextPosition - 1] = value;
    }
    
    
    public AddBuffer(int chunkSize) 
    {
        ChunkSize = chunkSize;
        AddNewChunk();
    }


    public ReadOnlySpan<T> GetSpan(int start, int length) => GetMemory(start, length).Span;

    public ReadOnlyMemory<T> GetMemory(int start, int length)
    {
        var chunkIndex = FindChunk(start);
        var relativeStart = start - ChunkSums[chunkIndex];
        var memory = Chunks[chunkIndex].AsMemory(relativeStart, length);
        return memory;
    }
    
    
    public (int Start, int Count) Append(T value) 
    {
        if (InChunkNextPosition + 1 > ChunkSize) 
            AddNewChunk();

        var start = InChunkNextPosition;
        var length = 1;
        LastChunk[InChunkNextPosition] = value;
        InChunkNextPosition += length;
        LastChunkSum += length;
        return (start, length);
    }
    
    public (int Start, int Length) Append(ReadOnlySpan<T> values) {
        if (InChunkNextPosition + values.Length > ChunkSize) 
            AddNewChunk();

        var start = InChunkNextPosition;
        var length = values.Length;
        
        values.CopyTo(LastChunk.AsSpan(start));
        InChunkNextPosition += values.Length;
        LastChunkSum += values.Length;
        return (start, length);
    }
    
    protected void AddNewChunk() {
        Chunks.Add(new T[ChunkSize]);
        ChunkSums.Add(0);
        InChunkNextPosition = 0;
    }
    
    
    public void Prune(int length)
    {
        var start = 0 + length;
        var chunkIndex = FindChunk(start);
        var chunkSum = ChunkSums[chunkIndex];
        
        Chunks.RemoveRange(0, chunkIndex);
        ChunkSums.RemoveRange(0, chunkSum);
        for (var i = 0; i < ChunkSums.Count; i++) 
            ChunkSums[i] -= chunkSum;
    }


    protected int FindChunk(int start) => ChunkSums.FindFirstGreater(start);
}