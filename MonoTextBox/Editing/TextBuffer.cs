using System.Diagnostics;
using MonoTextBox.Editing.BufferHandler;
using MonoTextBox.Editing.Inputs;
using MonoTextBox.Editing.Inputs.Interfaces;

namespace MonoTextBox.Editing;

/// <summary>
/// TextBuffer buffers <see cref="Inputs"/> and und/redo steps
/// </summary>
public class TextBuffer
{
    private readonly List<char> _buffer = new(256);
    private Caret _caret = new(0, 0);
    
    private readonly UndoRedoManager _undoRedoManager;
    
    private record LastInput(
        char LastChar, 
        IAddableInput Buffer
    );
    private LastInput? _lastInput = null;
    
    
    public IReadOnlyList<char> Buffer => _buffer;
    public Caret Caret => _caret;
    public string Content => _buffer.ToString() ?? string.Empty;
    public string SelectedContent => _caret.Slice(_buffer).ToString() ?? string.Empty;
    
    
    public TextBuffer(int maxUndoStep)
    {
        _undoRedoManager = new UndoRedoManager(maxUndoStep);
    }


    public void CaretUpdate(int index)
    {
        PushCurrentInput();
        _caret.StartIndex = index;
        _caret.Selection = 0;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="length">Use negative number for selecting from right to left</param>
    public void CaretSelect(int length)
    {
        _caret.Selection = length;
    }
    
    public void CaretMoveLeft()
    {
        if (_caret.Length > 0)
            _caret = new Caret(_caret.LeftIndex, 0);
        else
            _caret.StartIndex = Math.Max(0, _caret.StartIndex - 1);
    }

    public void CaretMoveRight()
    {
        if (_caret.Length > 0)
            _caret = new Caret(_caret.RightIndex, 0);
        else
            _caret.StartIndex = Math.Min(_buffer.Count - 1, _caret.StartIndex + 1);
    }

    
    public void Undo()
    {
        PushCurrentInput();
        _caret = _undoRedoManager.Undo(_buffer, _caret);
    }

    public void Redo()
    {
        PushCurrentInput();
        _caret = _undoRedoManager.Redo(_buffer, _caret);
    }
    
    
    public string? Copy() 
        => _caret.Selection == 0 
            ? null 
            : SelectedContent;

    public void Paste(string text)
    {
        PushCurrentInput();
        
        var (input, caret) = PasteHelper.Paste(_buffer, _caret, text);
        _undoRedoManager.Add(input);
        _caret = caret;
    } 

    public void Move(int destIndex)
    {
        Debug.Assert(_lastInput is null);
        
        var (input, caret) = MoveHelper.Move(_buffer, _caret, destIndex);
        _undoRedoManager.Add(input);
        _caret = caret;
    }
    
    
    public void ReceiveCharInput(char inputChar)
    {
        switch (inputChar)
        {
            case '\b':
                ReceiveBackspace();
                break;
            case (char)127:
                ReceiveDelete();
                break;
            default:
                ReceiveChar(inputChar);
                break;
        }
    }

    public void ReceiveStringInput(string pasteText)
    {
        if (string.IsNullOrEmpty(pasteText))
            return;
        
        PushCurrentInput();

        var input = new PasteInput(_caret.LeftIndex, pasteText, DisplaceSelectedOnInput());
        
        _buffer.InsertRange(_caret.StartIndex, pasteText);
        
        _caret.StartIndex += pasteText.Length;
        
        _undoRedoManager.Add(input);
    }
    
    public void ReceiveMovementInput()
    {
        if (_caret.OverrideIndex is null
            || _caret.CheckInRange(_caret.OverrideIndex.Value))
            return;
        
        PushCurrentInput();

        var insertIndex = _caret.OverrideIndex.Value;
        
        var input = new MoveInput(TODO, insertIndex);
        
        var content = _buffer.GetRange(_caret.LeftIndex, _caret.Length);
        _buffer.RemoveRange(_caret.LeftIndex, _caret.Length);
        _buffer.InsertRange(insertIndex, content);
        
        _caret = new(insertIndex, _caret.Length);
        
        _undoRedoManager.Add(input);
    }

    private void ReceiveDelete()
    {
        if (_caret.StartIndex == _buffer.Count)
            return;
        
        var replaced = DisplaceSelectedOnInput();
        
        if (_lastInput?.LastChar is (char)127
            && replaced is null)
        {
            _lastInput.Buffer.Add(_buffer[_caret.StartIndex]);
            _buffer.RemoveAt(_caret.StartIndex);
            return;
        }
        
        PushCurrentInput();

        if (replaced is null)
        {
            _lastInput = new(
                (char)127, 
                new DeleteInput(_caret.StartIndex, _buffer[_caret.StartIndex]));
            _buffer.RemoveAt(_caret.StartIndex);
        }
        else
            _lastInput = new(
                (char)127, 
                new DeleteInput(_caret.StartIndex, replaced));
        
        // no change in _caret.StartCaret;
    }

    private void ReceiveBackspace()
    {
        if (_caret.StartIndex < 1)
            return;
        
        var replaced = DisplaceSelectedOnInput();
        
        if (_lastInput?.LastChar is '\b'
            && replaced is null)
        {
            _lastInput.Buffer.Add(_buffer[_caret.StartIndex - 1]);
            _buffer.RemoveAt(_caret.StartIndex - 1);
            _caret.StartIndex--;
            return;
        }
        
        PushCurrentInput();

        if (replaced is null)
        {
            _lastInput = new(
                '\b', 
                new DeleteInput(_caret.StartIndex, _buffer[_caret.StartIndex - 1]));
            _buffer.RemoveAt(_caret.StartIndex - 1);
            _caret.StartIndex--;
        }
        else
            _lastInput = new(
                '\b', 
                new DeleteInput(_caret.StartIndex, replaced));
    }
    
    private void ReceiveChar(char inputChar)
    {
        var replaced = DisplaceSelectedOnInput();

        if (replaced is not null)
        {
            PushCurrentInput();
            _lastInput = new(inputChar, new TypeInput(_caret.LeftIndex, inputChar, replaced));
            PushCurrentInput();
        }
        else if (_lastInput is null)
            _lastInput = new(inputChar, new TypeInput(_caret.LeftIndex, inputChar)); 
        else if (Char.IsControl(inputChar))
            ReceiveControlChar(inputChar);
        else if (Char.IsPunctuation(inputChar))
            ReceivePunctuationChar(inputChar);
        else if (Char.IsWhiteSpace(inputChar))
            ReceiveWhiteSpaceChar(inputChar);
        else
            ReceiveLetterOrDigitChar(inputChar);
        
        _buffer.Insert(_caret.StartIndex, inputChar);
        
        _caret.StartIndex++;
    }
    
    private void ReceiveLetterOrDigitChar(char inputChar)
    {
        // case 1: " .But" = " " + "." + "But"; "\t...However" = "\t" + "..." + "However"
        // case 1: "www.something.com" = "www." + "something." + "com"
        // case 2: "Well... However" = "Well... " + "However" 

        if (!Char.IsLetterOrDigit(_lastInput.LastChar))
            SwitchNewTypingInput(inputChar);
        else
            _lastInput.Buffer.Add(inputChar);
    }

    private void ReceiveWhiteSpaceChar(char inputChar)
    {
        // case 1: "\t\t\t " = "\t\t\t" + " "
        // case 2: "He just said" = "He " + "just " + "said"
        // case 3: "He    " = "He " + "   "
        // case 4: "Well.    That's" = "Well. " + "   "+ "That's"
        
        if (Char.IsControl(_lastInput.LastChar))
            SwitchNewTypingInput(inputChar);
        else
            _lastInput.Buffer.Add(inputChar);

        if (!char.IsWhiteSpace(_lastInput.LastChar))
            PushCurrentInput();
    }

    private void ReceivePunctuationChar(char inputChar)
    {
        // case 1: "Fine." = "Fine."
        // case 2: "Well..." = "Well..."; "How!?" = "How!?"
        // case 3: "He said, :" = "He said, " + ":"
        // case 4: "\t\t\t..." = "\t\t\t" + "..."
        
        if (Char.IsWhiteSpace(_lastInput.LastChar) 
            || Char.IsControl(_lastInput.LastChar))
            SwitchNewTypingInput(inputChar);
        else
            _lastInput.Buffer.Add(inputChar);
    }

    private void ReceiveControlChar(char inputChar)
    {
        // case 1: "something\n" = "something" + "\n"
        // case 2: "something\t\t\t" = "something" + "\t\t\t"
        // case 3: "\t\t\n" = "\t\t" + "\n"

        if (_lastInput.LastChar != inputChar)
            SwitchNewTypingInput(inputChar);
        else
            _lastInput.Buffer.Add(inputChar);
    }

    private void PushCurrentInput()
    {
        if (_lastInput is null) 
            return;
        
        _undoRedoManager.Add(_lastInput.Buffer);
        _lastInput = null;
    }

    private void SwitchNewTypingInput(char inputChar)
    {
        PushCurrentInput();
        _lastInput = new(inputChar, new TypeInput(_caret.LeftIndex, inputChar)); 
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns>If anything is selected.</returns>
    private List<char>? DisplaceSelectedOnInput()
    {
        if (_caret.Selection == 0)
            return null;

        var index = _caret.LeftIndex;
        var count = Math.Abs(_caret.Selection);
        
        var displaced = _buffer.GetRange(index, count); // copy the range
        
        _buffer.RemoveRange(index, count);
        
        _caret.StartIndex = index;
        _caret.Selection = 0;
        
        return displaced;
    }
}