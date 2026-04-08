using MonoTextBox.Utils;

namespace MonoTextBox.Positioning.SpanEnumerating;

public static class SpanExtensions
{
    public static ReadOnlySpan<T> Slice<T>(this ReadOnlySpan<T> span, Slice slice)
    {
        return span.Slice(slice.Start, slice.End - slice.Start);
    }


    public static int IndexNewLine(this ReadOnlySpan<char> text)
    {
        return text.IndexOfAny('\r', '\n');
    }
    
    
    public static OffsetEnumerator EnumerateNewLines(this ReadOnlySpan<char> span)
    {
        return new OffsetEnumerator(span, EnumerateNewLine);
    }
    
    /// <summary>
    /// Example:
    /// "\nhello\r\nworld\n" => "" "hello" "world" ""
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    private static OffsetResult EnumerateNewLine(OffsetContext context)
    {
        if (context.IsFinished) 
            return OffsetResult.Finish;

        var index = context.Remains.IndexNewLine();
        
        if (index == -1)
        {
            context.Finish();
            return new OffsetResult(
                true, 
                new Slice(
                    context.AbsoluteOffset, 
                    context.AbsoluteOffset + context.Remains.Length
                )
            );
        }

        var offset = new Slice(
            context.AbsoluteOffset,
            context.AbsoluteOffset + index
        );
        var stride = 
            context.Remains[index] == '\r' 
            && index + 1 < context.Remains.Length 
            && context.Remains[index + 1] == '\n'
                ? 2 : 1;
        context.Update(
            false,
            context.AbsoluteOffset + index + stride,
            context.Remains[(index + stride)..]
        );
        return new OffsetResult(true, offset);
    }
}