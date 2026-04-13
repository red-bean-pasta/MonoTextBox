namespace HeadlessTextBox.Utils;

public static class AssertException
{
    public class AssertFailedException : Exception
    {
        public AssertFailedException() { }

        public AssertFailedException(string? message): base(message) { }

        public AssertFailedException(string? message, Exception inner): base(message, inner) { }
    }
    
    
    public static void ThrowIf(bool condition, string? message = null)
    {
        if (condition)
            throw new AssertFailedException(message);
    }
}